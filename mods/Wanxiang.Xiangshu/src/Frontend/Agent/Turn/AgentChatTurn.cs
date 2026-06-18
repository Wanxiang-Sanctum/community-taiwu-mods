using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Agent.Turn;

internal sealed class AgentChatTurn(
    string? agentSessionId,
    string playerName,
    IReadOnlyList<AgentChatTurnMessage> messages)
{
    public string? AgentSessionId { get; } = agentSessionId;

    public string PlayerName { get; } = playerName;

    public IReadOnlyList<AgentChatTurnMessage> Messages { get; } = messages;
}

internal sealed class AgentChatTurnMessage(
    string id,
    DateTimeOffset sentAt,
    string role,
    string origin,
    string content)
{
    [JsonProperty("id")]
    public string Id { get; } = id;

    [JsonProperty("sentAt")]
    public DateTimeOffset SentAt { get; } = sentAt;

    [JsonProperty("role")]
    public string Role { get; } = role;

    [JsonProperty("origin")]
    public string Origin { get; } = origin;

    [JsonProperty("content")]
    public string Content { get; } = content;
}
