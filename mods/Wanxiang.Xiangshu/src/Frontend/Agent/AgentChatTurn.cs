namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentChatTurn(
    string sessionId,
    string batchId,
    string? externalSessionId,
    IReadOnlyList<AgentChatTurnMessage> visibleMessages,
    IReadOnlyList<AgentChatTurnMessage> batchMessages)
{
    public string SessionId { get; } = sessionId;

    public string BatchId { get; } = batchId;

    public string? ExternalSessionId { get; } = externalSessionId;

    public IReadOnlyList<AgentChatTurnMessage> VisibleMessages { get; } = visibleMessages;

    public IReadOnlyList<AgentChatTurnMessage> BatchMessages { get; } = batchMessages;
}

internal sealed class AgentChatTurnMessage(
    AgentChatTurnRole role,
    string content)
{
    public AgentChatTurnRole Role { get; } = role;

    public string Content { get; } = content;
}

internal enum AgentChatTurnRole
{
    User = 0,
    Assistant = 1,
}
