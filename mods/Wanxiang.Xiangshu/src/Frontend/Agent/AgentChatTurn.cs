namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentChatTurn(
    string? externalSessionId,
    string playerName,
    string assistantName,
    IReadOnlyList<string> playerMessages)
{
    public string? ExternalSessionId { get; } = externalSessionId;

    public string PlayerName { get; } = playerName;

    public string AssistantName { get; } = assistantName;

    public IReadOnlyList<string> PlayerMessages { get; } = playerMessages;
}
