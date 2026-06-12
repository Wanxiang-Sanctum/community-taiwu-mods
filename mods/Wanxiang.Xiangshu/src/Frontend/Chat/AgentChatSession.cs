using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Agent;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "AgentCliLauncher is owned and disposed by FrontendPlugin; the chat session only borrows it.")]
internal sealed class AgentChatSession : IDisposable
{
    private const string FailureMessage = "此刻诸机不应，稍后再问。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly AgentCliLauncher _agentCliLauncher;
    private readonly Func<AgentSettings?> _settingsProvider;
    private readonly AgentChatSessionStore? _sessionStore;
    private readonly string _adapterName;
    private readonly string _assistantName;
    private readonly ConcurrentQueue<AgentChatSessionEvent> _events = new();
    private readonly object _syncRoot = new();
    private readonly List<AgentChatMessage> _visibleMessages = [];
    private readonly Queue<AgentChatMessage> _pendingMessages = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _sessionId;

    private int _nextMessageId;
    private bool _working;
    private bool _disposed;
    private bool _cancellationDisposed;
    private string? _externalSessionId;

    public AgentChatSession(
        AgentCliLauncher agentCliLauncher,
        Func<AgentSettings?> settingsProvider,
        AgentChatSessionStore? sessionStore,
        AgentAdapter adapter,
        string assistantName)
    {
        _agentCliLauncher = agentCliLauncher
            ?? throw new ArgumentNullException(nameof(agentCliLauncher));
        _settingsProvider = settingsProvider
            ?? throw new ArgumentNullException(nameof(settingsProvider));
        _sessionStore = sessionStore;
        _adapterName = GetAdapterName(adapter);
        _assistantName = string.IsNullOrWhiteSpace(assistantName)
            ? ChatParticipantIdentity.AssistantName
            : assistantName.Trim();

        AgentChatSessionState? restoredState = _sessionStore?.LoadCurrent();

        if (restoredState is not null
            && !string.Equals(restoredState.Adapter, _adapterName, StringComparison.OrdinalIgnoreCase))
        {
            Log.Info(
                "chat session reset because agent adapter changed",
                new
                {
                    restoredState.SessionId,
                    restoredAdapter = restoredState.Adapter,
                    currentAdapter = _adapterName,
                });
            restoredState = null;
        }

        _sessionId = restoredState?.SessionId ?? Guid.NewGuid().ToString("N");

        if (restoredState is not null)
        {
            _visibleMessages.AddRange(restoredState.VisibleMessages);
            _nextMessageId = restoredState.LastMessageNumber;
            _externalSessionId = restoredState.ExternalSessionId;
        }

        PersistSnapshot();
    }

    public void SubmitUserMessage(
        string content,
        string speakerName)
    {
        string trimmedContent = content.Trim();
        string trimmedSpeakerName = speakerName.Trim();

        if (string.IsNullOrEmpty(trimmedContent))
        {
            return;
        }

        AgentChatMessage message;

        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();

            message = new AgentChatMessage(
                CreateMessageId(),
                AgentChatRole.User,
                trimmedSpeakerName,
                trimmedContent,
                "user");
            _visibleMessages.Add(message);
            _pendingMessages.Enqueue(message);
        }

        PersistSnapshot();
        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        StartProcessingIfNeeded();
    }

    public bool TryDequeueEvent(out AgentChatSessionEvent sessionEvent)
    {
        return _events.TryDequeue(out sessionEvent);
    }

    public IReadOnlyList<AgentChatMessage> CreateVisibleMessagesSnapshot()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();
            return [.. _visibleMessages];
        }
    }

    public void AddIntermediateReply(string? content)
    {
        string normalizedContent = NormalizeMessageContent(content);
        AgentChatMessage message;

        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();

            if (normalizedContent.Length == 0)
            {
                throw new InvalidOperationException("Intermediate reply content is required.");
            }

            message = new AgentChatMessage(
                CreateMessageId(),
                AgentChatRole.Assistant,
                _assistantName,
                normalizedContent,
                "agent-intermediate");
            _visibleMessages.Add(message);
        }

        PersistSnapshot();
        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
    }

    public void Dispose()
    {
        bool disposeCancellation;

        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            disposeCancellation = !_working;
        }

        _cancellation.Cancel();

        if (disposeCancellation)
        {
            DisposeCancellation();
        }
    }

    private void StartProcessingIfNeeded()
    {
        lock (_syncRoot)
        {
            if (_disposed || _working || _pendingMessages.Count == 0)
            {
                return;
            }

            _working = true;
        }

        ProcessPendingMessagesAsync(_cancellation.Token).Forget(
            static ex => Log.Error(ex, "chat session processing failed"));
    }

    private async UniTask ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.SwitchToThreadPool();

            while (!cancellationToken.IsCancellationRequested)
            {
                AgentSettings? settings = _settingsProvider();
                AgentChatTurn? turn = CreateNextTurnOrStop();

                if (turn is null)
                {
                    return;
                }

                if (settings is null)
                {
                    AddAssistantSessionMessage(FailureMessage);
                    continue;
                }

                try
                {
                    AgentCliInvocationResult result = await _agentCliLauncher.InvokeChatAsync(
                            settings,
                            turn,
                            cancellationToken);
                    UpdateExternalSessionId(result.ExternalSessionId);
                    AddAssistantMessage(result.AssistantMessage, "agent");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Error(ex, "chat agent invocation failed");
                    AddAssistantSessionMessage(FailureMessage);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            StopProcessing();
            PersistSnapshot();
        }
    }

    private AgentChatTurn? CreateNextTurnOrStop()
    {
        AgentChatTurn? turn;

        lock (_syncRoot)
        {
            if (_pendingMessages.Count == 0)
            {
                _ = StopProcessingLocked();
                turn = null;
            }
            else
            {
                List<AgentChatMessage> turnMessages = [];

                while (_pendingMessages.Count > 0)
                {
                    AgentChatMessage message = _pendingMessages.Dequeue();
                    turnMessages.Add(message);
                }

                turn = new AgentChatTurn(
                    _externalSessionId,
                    turnMessages[^1].SpeakerName,
                    _assistantName,
                    [.. turnMessages.Select(static message => message.Content)]);
            }
        }

        PersistSnapshot();
        return turn;
    }

    private void StopProcessing()
    {
        bool disposeCancellation;

        lock (_syncRoot)
        {
            disposeCancellation = StopProcessingLocked();
        }

        if (disposeCancellation)
        {
            DisposeCancellation();
        }
    }

    private bool StopProcessingLocked()
    {
        if (!_working)
        {
            return _disposed && !_cancellationDisposed;
        }

        _working = false;

        return _disposed && !_cancellationDisposed;
    }

    private void DisposeCancellation()
    {
        lock (_syncRoot)
        {
            if (_cancellationDisposed)
            {
                return;
            }

            _cancellationDisposed = true;
        }

        _cancellation.Dispose();
    }

    private void AddAssistantMessage(
        string content,
        string origin)
    {
        AgentChatMessage message;

        lock (_syncRoot)
        {
            message = new AgentChatMessage(
                CreateMessageId(),
                AgentChatRole.Assistant,
                _assistantName,
                content,
                origin);
            _visibleMessages.Add(message);
        }

        PersistSnapshot();
        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
    }

    private void AddAssistantSessionMessage(string content)
    {
        AddAssistantMessage(content, "session");
    }

    private string CreateMessageId()
    {
        _nextMessageId++;
        return "message-" + _nextMessageId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void UpdateExternalSessionId(string? externalSessionId)
    {
        if (string.IsNullOrWhiteSpace(externalSessionId))
        {
            return;
        }

        lock (_syncRoot)
        {
            _externalSessionId = externalSessionId.Trim();
        }
    }

    private void PersistSnapshot()
    {
        AgentChatSessionState? state;

        lock (_syncRoot)
        {
            if (_sessionStore is null)
            {
                return;
            }

            state = new AgentChatSessionState(
                _sessionId,
                _adapterName,
                _externalSessionId,
                _nextMessageId,
                [.. _visibleMessages.Select(CloneMessage)]);
        }

        _sessionStore.Save(state);
    }

    private static AgentChatMessage CloneMessage(AgentChatMessage message)
    {
        return new AgentChatMessage(
            message.Id,
            message.Role,
            message.SpeakerName,
            message.Content,
            message.Origin);
    }

    private static string GetAdapterName(AgentAdapter adapter)
    {
        return adapter == AgentAdapter.Claude ? "claude" : "codex";
    }

    private static string NormalizeMessageContent(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private void ThrowIfDisposedLocked()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentChatSession));
        }
    }
}
