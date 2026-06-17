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
    private const string InterruptMessage = "且慢";

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

    private int _lastMessageOrdinal;
    private bool _dispatching;
    private bool _dispatchPaused;
    private bool _disposed;
    private bool _cancellationDisposed;
    private int _sessionGeneration;
    private TurnDispatch? _activeDispatch;
    private string _sessionId;
    private string? _agentSessionId;

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
                    restoredAdapter = restoredState.Adapter,
                    currentAdapter = _adapterName,
                });
            restoredState = null;
        }

        _sessionId = restoredState?.SessionId ?? Guid.NewGuid().ToString("N");

        if (restoredState is not null)
        {
            _visibleMessages.AddRange(restoredState.VisibleMessages);
            _lastMessageOrdinal = restoredState.LastMessageOrdinal;
            _agentSessionId = restoredState.AgentSessionId;
        }

        PersistSnapshot();
    }

    public bool IsReplying
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposedLocked();
                return IsReplyingLocked();
            }
        }
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
                DateTimeOffset.UtcNow,
                AgentChatRole.User,
                trimmedSpeakerName,
                trimmedContent,
                "user");
            _visibleMessages.Add(message);
            _pendingMessages.Enqueue(message);
            _dispatchPaused = false;
            _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        }

        PersistSnapshot();
        StartProcessingIfNeeded();
    }

    public bool RequestInterrupt(string speakerName)
    {
        string trimmedSpeakerName = speakerName.Trim();

        if (string.IsNullOrWhiteSpace(trimmedSpeakerName))
        {
            return false;
        }

        AgentChatMessage message;
        CancellationTokenSource turnCancellation;

        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();

            if (!_dispatching
                || _activeDispatch is not { InterruptRequested: false, ResetRequested: false } activeDispatch)
            {
                return false;
            }

            message = new AgentChatMessage(
                CreateMessageId(),
                DateTimeOffset.UtcNow,
                AgentChatRole.User,
                trimmedSpeakerName,
                InterruptMessage,
                "user");
            _visibleMessages.Add(message);
            _pendingMessages.Enqueue(message);
            _dispatchPaused = true;
            activeDispatch.InterruptRequested = true;
            turnCancellation = activeDispatch.Cancellation;
            _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        }

        PersistSnapshot();
        _events.Enqueue(AgentChatSessionEvent.StateChanged());
        turnCancellation.Cancel();
        return true;
    }

    public void Reset()
    {
        CancellationTokenSource? turnCancellation = null;
        string oldSessionId;
        string newSessionId;

        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();

            oldSessionId = _sessionId;

            if (_activeDispatch is { } activeDispatch)
            {
                activeDispatch.ResetRequested = true;
                turnCancellation = activeDispatch.Cancellation;
            }

            ResetCurrentSessionLocked();
            newSessionId = _sessionId;
            _events.Enqueue(AgentChatSessionEvent.MessagesReset());
            _events.Enqueue(AgentChatSessionEvent.StateChanged());
        }

        PersistSnapshot();
        TryDeleteReplacedSessionSnapshot(oldSessionId, newSessionId);

        turnCancellation?.Cancel();
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

            if (_activeDispatch is not { InterruptRequested: false, ResetRequested: false })
            {
                return;
            }

            message = new AgentChatMessage(
                CreateMessageId(),
                DateTimeOffset.UtcNow,
                AgentChatRole.Assistant,
                _assistantName,
                normalizedContent,
                "agent-intermediate");
            _visibleMessages.Add(message);
            _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        }

        PersistSnapshot();
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
            disposeCancellation = !_dispatching;
        }

        _cancellation.Cancel();

        if (disposeCancellation)
        {
            DisposeCancellation();
        }
    }

    private void StartProcessingIfNeeded()
    {
        bool stateChanged = false;

        lock (_syncRoot)
        {
            if (_disposed || _dispatching || !CanDispatchPendingLocked())
            {
                return;
            }

            _dispatching = true;
            stateChanged = true;
        }

        if (stateChanged)
        {
            _events.Enqueue(AgentChatSessionEvent.StateChanged());
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
                TurnDispatch? dispatch = TakeNextDispatch();

                if (dispatch is null)
                {
                    return;
                }

                AgentSettings? settings = _settingsProvider();

                if (settings is null)
                {
                    try
                    {
                        bool turnCompleted = false;
                        CancellationToken turnToken = dispatch.Cancellation.Token;

                        try
                        {
                            turnToken.ThrowIfCancellationRequested();
                            turnCompleted = true;
                            if (!CompleteDispatch(dispatch))
                            {
                                throw new OperationCanceledException(turnToken);
                            }

                            if (!TryAddAssistantMessage(dispatch, FailureMessage, "session"))
                            {
                                throw new OperationCanceledException(turnToken);
                            }
                        }
                        finally
                        {
                            if (!turnCompleted)
                            {
                                _ = CompleteDispatch(dispatch);
                            }
                        }

                        Log.Warning(
                            "chat agent invocation skipped because settings are unavailable",
                            CreateLogContext());
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                    }
                    continue;
                }

                try
                {
                    bool turnCompleted = false;
                    CancellationToken turnToken = dispatch.Cancellation.Token;

                    try
                    {
                        turnToken.ThrowIfCancellationRequested();
                        AgentCliChatResult result = await _agentCliLauncher.InvokeChatAsync(
                            settings,
                            dispatch.Turn,
                            turnToken);
                        turnToken.ThrowIfCancellationRequested();
                        turnCompleted = true;
                        if (!CompleteDispatch(dispatch))
                        {
                            throw new OperationCanceledException(turnToken);
                        }

                        if (!TryApplyAgentResult(dispatch, result))
                        {
                            throw new OperationCanceledException(turnToken);
                        }
                    }
                    finally
                    {
                        if (!turnCompleted)
                        {
                            _ = CompleteDispatch(dispatch);
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Error(
                        ex,
                        "chat agent invocation failed",
                        CreateFailureLogContext(dispatch, ex));
                    _ = TryAddAssistantMessage(dispatch, FailureMessage, "session");
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
            StartProcessingIfNeeded();
        }
    }

    private object CreateLogContext()
    {
        return new
        {
            adapter = _adapterName,
        };
    }

    private object CreateFailureLogContext(
        TurnDispatch dispatch,
        Exception exception)
    {
        if (exception is AgentCliFailureException cliFailure)
        {
            return new
            {
                adapter = _adapterName,
                cliSessionMode = GetCliSessionMode(dispatch),
                cliFailureReason = cliFailure.Reason,
                cliExitCode = cliFailure.ExitCode,
                cliStderrExcerpt = cliFailure.StderrExcerpt,
            };
        }

        return new
        {
            adapter = _adapterName,
        };
    }

    private static string GetCliSessionMode(TurnDispatch dispatch)
    {
        return string.IsNullOrWhiteSpace(dispatch.Turn.AgentSessionId)
            ? "new"
            : "resumed";
    }

    private TurnDispatch? TakeNextDispatch()
    {
        TurnDispatch? dispatch;

        lock (_syncRoot)
        {
            if (!CanDispatchPendingLocked())
            {
                dispatch = null;
            }
            else
            {
                List<AgentChatMessage> turnMessages = [];

                while (_pendingMessages.Count > 0)
                {
                    AgentChatMessage message = _pendingMessages.Dequeue();
                    turnMessages.Add(message);
                }

                dispatch = CreateDispatch(turnMessages);
                _activeDispatch = dispatch;
            }
        }

        PersistSnapshot();
        if (dispatch is not null)
        {
            _events.Enqueue(AgentChatSessionEvent.StateChanged());
        }

        return dispatch;
    }

    private TurnDispatch CreateDispatch(List<AgentChatMessage> turnMessages)
    {
        CancellationTokenSource? turnCancellation = null;

        try
        {
            AgentChatTurn turn = new(
                _agentSessionId,
                turnMessages[^1].SpeakerName,
                [
                    .. turnMessages.Select(
                        static message => new AgentChatTurnMessage(
                            message.Id,
                            message.CreatedAt.ToUniversalTime(),
                            message.Content)),
                ]);
            turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
            TurnDispatch dispatch = new(
                turn,
                turnCancellation,
                _sessionGeneration);
            turnCancellation = null;
            return dispatch;
        }
        finally
        {
            turnCancellation?.Dispose();
        }
    }

    private bool CompleteDispatch(TurnDispatch dispatch)
    {
        bool stateChanged = false;
        bool cancelled = dispatch.Cancellation.IsCancellationRequested || dispatch.ResetRequested;

        lock (_syncRoot)
        {
            if (ReferenceEquals(_activeDispatch, dispatch))
            {
                cancelled = cancelled || dispatch.InterruptRequested || dispatch.ResetRequested;
                _activeDispatch = null;
                stateChanged = true;
            }
        }

        dispatch.Cancellation.Dispose();

        if (stateChanged)
        {
            _events.Enqueue(AgentChatSessionEvent.StateChanged());
        }

        return !cancelled;
    }

    private void StopProcessing()
    {
        bool disposeCancellation;
        bool stateChanged;

        lock (_syncRoot)
        {
            (disposeCancellation, stateChanged) = StopProcessingLocked();
        }

        if (stateChanged)
        {
            _events.Enqueue(AgentChatSessionEvent.StateChanged());
        }

        if (disposeCancellation)
        {
            DisposeCancellation();
        }
    }

    private (bool DisposeCancellation, bool StateChanged) StopProcessingLocked()
    {
        if (!_dispatching)
        {
            return (_disposed && !_cancellationDisposed, StateChanged: false);
        }

        _dispatching = false;

        return (_disposed && !_cancellationDisposed, StateChanged: true);
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

    private bool TryApplyAgentResult(
        TurnDispatch dispatch,
        AgentCliChatResult result)
    {
        AgentChatMessage message;

        lock (_syncRoot)
        {
            if (!MatchesCurrentSessionLocked(dispatch))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(result.AgentSessionId))
            {
                _agentSessionId = result.AgentSessionId.Trim();
            }

            message = new AgentChatMessage(
                CreateMessageId(),
                DateTimeOffset.UtcNow,
                AgentChatRole.Assistant,
                _assistantName,
                result.AssistantMessage,
                "agent");
            _visibleMessages.Add(message);
            _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        }

        PersistSnapshot();
        return true;
    }

    private bool TryAddAssistantMessage(
        TurnDispatch dispatch,
        string content,
        string origin)
    {
        AgentChatMessage message;

        lock (_syncRoot)
        {
            if (!MatchesCurrentSessionLocked(dispatch))
            {
                return false;
            }

            message = new AgentChatMessage(
                CreateMessageId(),
                DateTimeOffset.UtcNow,
                AgentChatRole.Assistant,
                _assistantName,
                content,
                origin);
            _visibleMessages.Add(message);
            _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
        }

        PersistSnapshot();
        return true;
    }

    private void ResetCurrentSessionLocked()
    {
        _sessionGeneration++;
        _sessionId = Guid.NewGuid().ToString("N");
        _agentSessionId = null;
        _lastMessageOrdinal = 0;
        _visibleMessages.Clear();
        _pendingMessages.Clear();
        _dispatchPaused = false;
    }

    private bool CanDispatchPendingLocked()
    {
        return _pendingMessages.Count > 0 && !_dispatchPaused;
    }

    private bool IsReplyingLocked()
    {
        return _dispatching
            && _activeDispatch is { InterruptRequested: false, ResetRequested: false };
    }

    private bool MatchesCurrentSessionLocked(TurnDispatch dispatch)
    {
        return dispatch.SessionGeneration == _sessionGeneration;
    }

    private string CreateMessageId()
    {
        _lastMessageOrdinal++;
        return "message-" + _lastMessageOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

            state = CreateStateSnapshotLocked();
        }

        _sessionStore.Save(state);
    }

    private void TryDeleteReplacedSessionSnapshot(
        string oldSessionId,
        string newSessionId)
    {
        if (_sessionStore is null
            || string.Equals(oldSessionId, newSessionId, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            _sessionStore.DeleteSnapshot(oldSessionId);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            Log.Error(ex, "chat session snapshot delete failed after player reset");
        }
    }

    private AgentChatSessionState CreateStateSnapshotLocked()
    {
        return new AgentChatSessionState(
            _sessionId,
            _adapterName,
            _agentSessionId,
            _lastMessageOrdinal,
            [.. _visibleMessages.Select(CloneMessage)]);
    }

    private static AgentChatMessage CloneMessage(AgentChatMessage message)
    {
        return new AgentChatMessage(
            message.Id,
            message.CreatedAt,
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

    private sealed class TurnDispatch(
        AgentChatTurn turn,
        CancellationTokenSource cancellation,
        int sessionGeneration)
    {
        public AgentChatTurn Turn { get; } = turn;

        public CancellationTokenSource Cancellation { get; } = cancellation;

        public int SessionGeneration { get; } = sessionGeneration;

        public bool InterruptRequested { get; set; }

        public bool ResetRequested { get; set; }
    }

}
