namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentChatTurn(
    string sessionId,
    string batchId,
    string? externalSessionId,
    IReadOnlyList<string> playerMessages)
{
    public string SessionId { get; } = sessionId;

    public string BatchId { get; } = batchId;

    public string? ExternalSessionId { get; } = externalSessionId;

    public IReadOnlyList<string> PlayerMessages { get; } = playerMessages;
}
