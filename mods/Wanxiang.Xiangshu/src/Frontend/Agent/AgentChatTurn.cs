using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentChatTurn(
    string? agentSessionId,
    string playerName,
    IReadOnlyList<AgentChatTurnMessage> playerMessages)
{
    public string? AgentSessionId { get; } = agentSessionId;

    public string PlayerName { get; } = playerName;

    public IReadOnlyList<AgentChatTurnMessage> PlayerMessages { get; } = playerMessages;
}

internal sealed class AgentChatTurnMessage(
    string id,
    DateTimeOffset sentAt,
    string content)
{
    [JsonProperty("id")]
    public string Id { get; } = id;

    [JsonProperty("sentAt")]
    public DateTimeOffset SentAt { get; } = sentAt;

    [JsonProperty("content")]
    public string Content { get; } = content;
}
