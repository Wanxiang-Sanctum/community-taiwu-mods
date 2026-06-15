namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentChatTurn(
    string? agentSessionId,
    string playerName,
    string assistantName,
    IReadOnlyList<string> playerMessages)
{
    public string? AgentSessionId { get; } = agentSessionId;

    public string PlayerName { get; } = playerName;

    public string AssistantName { get; } = assistantName;

    public IReadOnlyList<string> PlayerMessages { get; } = playerMessages;
}
