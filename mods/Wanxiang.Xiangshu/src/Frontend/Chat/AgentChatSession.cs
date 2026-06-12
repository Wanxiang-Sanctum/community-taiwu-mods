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
internal sealed class AgentChatSession(
    AgentCliLauncher agentCliLauncher,
    Func<AgentSettings?> settingsProvider,
    string assistantName) : IDisposable
{
    private const string FailureMessage = "此刻诸机不应，稍后再问。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly AgentCliLauncher _agentCliLauncher = agentCliLauncher;
    private readonly Func<AgentSettings?> _settingsProvider = settingsProvider;
    private readonly string _assistantName = string.IsNullOrWhiteSpace(assistantName)
        ? ChatParticipantIdentity.AssistantName
        : assistantName.Trim();
    private readonly ConcurrentQueue<AgentChatSessionEvent> _events = new();
    private readonly object _syncRoot = new();
    private readonly List<AgentChatMessage> _visibleMessages = [];
    private readonly Queue<AgentChatMessage> _pendingMessages = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    private int _nextMessageId;
    private int _nextBatchId;
    private bool _working;
    private bool _disposed;
    private bool _cancellationDisposed;
    private string? _externalSessionId;

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
                "user",
                batchId: null);
            _visibleMessages.Add(message);
            _pendingMessages.Enqueue(message);
        }

        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        StartProcessingIfNeeded();
    }

    public bool TryDequeueEvent(out AgentChatSessionEvent sessionEvent)
    {
        return _events.TryDequeue(out sessionEvent);
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
                    _externalSessionId = result.ExternalSessionId ?? _externalSessionId;
                    AddAssistantMessage(result.AssistantMessage, "agent", turn.BatchId);
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
        }
    }

    private AgentChatTurn? CreateNextTurnOrStop()
    {
        lock (_syncRoot)
        {
            if (_pendingMessages.Count == 0)
            {
                _ = StopProcessingLocked();
                return null;
            }

            string batchId = CreateBatchId();
            List<AgentChatMessage> batchMessages = [];

            while (_pendingMessages.Count > 0)
            {
                AgentChatMessage message = _pendingMessages.Dequeue();
                message.BatchId = batchId;
                batchMessages.Add(message);
            }

            return new AgentChatTurn(
                _sessionId,
                batchId,
                _externalSessionId,
                batchMessages[^1].SpeakerName,
                _assistantName,
                [.. batchMessages.Select(static message => message.Content)]);
        }
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
        string origin,
        string? batchId)
    {
        AgentChatMessage message;

        lock (_syncRoot)
        {
            message = new AgentChatMessage(
                CreateMessageId(),
                AgentChatRole.Assistant,
                _assistantName,
                content,
                origin,
                batchId);
            _visibleMessages.Add(message);
        }

        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
    }

    private void AddAssistantSessionMessage(string content)
    {
        AddAssistantMessage(content, "session", batchId: null);
    }

    private string CreateMessageId()
    {
        _nextMessageId++;
        return "message-" + _nextMessageId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private string CreateBatchId()
    {
        _nextBatchId++;
        return "batch-" + _nextBatchId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void ThrowIfDisposedLocked()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentChatSession));
        }
    }
}
