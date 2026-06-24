using Newtonsoft.Json;
using Wanxiang.Xiangshu.Frontend.Chat;

namespace Wanxiang.Xiangshu.Frontend.Agent.Turn;

internal sealed class AgentChatTurn(
    string? agentSessionId,
    string playerName,
    IReadOnlyList<AgentChatContextMessage> contextMessages)
{
    public string? AgentSessionId { get; } = agentSessionId;

    public string PlayerName { get; } = playerName;

    public IReadOnlyList<AgentChatContextMessage> ContextMessages { get; } = contextMessages;
}

internal sealed class AgentChatContextMessage(
    string id,
    DateTimeOffset sentAt,
    AgentChatRole role,
    AgentChatMessageOrigin origin,
    string content)
{
    [JsonProperty("id")]
    public string Id { get; } = id;

    [JsonProperty("sentAt")]
    public DateTimeOffset SentAt { get; } = sentAt;

    [JsonProperty("role")]
    public AgentChatRole Role { get; } = role;

    [JsonProperty("origin")]
    public AgentChatMessageOrigin Origin { get; } = origin;

    [JsonProperty("content")]
    public string Content { get; } = content;
}
