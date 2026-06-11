using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Agent;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "AgentCliLauncher is owned and disposed by FrontendPlugin; the chat session only borrows it.")]
internal sealed class AgentChatSession(
    AgentCliLauncher agentCliLauncher,
    Func<AgentSettings?> settingsProvider) : IDisposable
{
    private const string FailureMessage = "此刻诸机不应，稍后再问。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly AgentCliLauncher _agentCliLauncher = agentCliLauncher;
    private readonly Func<AgentSettings?> _settingsProvider = settingsProvider;
    private readonly ConcurrentQueue<AgentChatSessionEvent> _events = new();
    private readonly object _syncRoot = new();
    private readonly List<AgentChatMessage> _messages = [];
    private readonly Queue<AgentChatMessage> _pendingMessages = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    private int _nextMessageId;
    private int _nextBatchId;
    private bool _working;
    private bool _disposed;
    private string? _externalSessionId;

    public void SubmitUserMessage(string content)
    {
        ThrowIfDisposed();

        string trimmedContent = content.Trim();

        if (string.IsNullOrEmpty(trimmedContent))
        {
            return;
        }

        AgentChatMessage message;

        lock (_syncRoot)
        {
            message = new AgentChatMessage(
                CreateMessageId(),
                AgentChatRole.User,
                trimmedContent,
                "user",
                batchId: null);
            _messages.Add(message);
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private void StartProcessingIfNeeded()
    {
        lock (_syncRoot)
        {
            if (_working || _pendingMessages.Count == 0)
            {
                return;
            }

            _working = true;
        }

        _events.Enqueue(AgentChatSessionEvent.WorkingChanged(isWorking: true));
#pragma warning disable CA2025
        _ = Task.Run(ProcessPendingMessagesAsync);
#pragma warning restore CA2025
    }

    private async Task ProcessPendingMessagesAsync()
    {
        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                AgentSettings? settings = _settingsProvider();
                AgentChatTurn? turn = CreateNextTurn();

                if (turn is null)
                {
                    FinishProcessing();
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
                            _cancellation.Token);
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
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
    }

    private AgentChatTurn? CreateNextTurn()
    {
        lock (_syncRoot)
        {
            if (_pendingMessages.Count == 0)
            {
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
                [.. _messages.Select(ToAgentChatTurnMessage)],
                [.. batchMessages.Select(ToAgentChatTurnMessage)]);
        }
    }

    private void FinishProcessing()
    {
        lock (_syncRoot)
        {
            _working = false;
        }

        _events.Enqueue(AgentChatSessionEvent.WorkingChanged(isWorking: false));
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
                content,
                origin,
                batchId);
            _messages.Add(message);
        }

        _events.Enqueue(AgentChatSessionEvent.MessageAdded(message));
    }

    private void AddAssistantSessionMessage(string content)
    {
        AddAssistantMessage(content, "session", batchId: null);
    }

    private static AgentChatTurnMessage ToAgentChatTurnMessage(AgentChatMessage message)
    {
        AgentChatTurnRole role = message.Role == AgentChatRole.User
            ? AgentChatTurnRole.User
            : AgentChatTurnRole.Assistant;
        return new AgentChatTurnMessage(role, message.Content);
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentChatSession));
        }
    }
}
