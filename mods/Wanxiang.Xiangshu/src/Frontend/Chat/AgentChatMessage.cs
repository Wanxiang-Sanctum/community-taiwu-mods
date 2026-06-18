using System.Runtime.Serialization;

namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatMessage(
    string id,
    DateTimeOffset createdAt,
    AgentChatRole role,
    string speakerName,
    string content,
    AgentChatMessageOrigin origin)
{
    public string Id { get; } = id;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public AgentChatRole Role { get; } = role;

    public string SpeakerName { get; } = speakerName;

    public string Content { get; } = content;

    public AgentChatMessageOrigin Origin { get; } = origin;
}

internal enum AgentChatRole
{
    [EnumMember(Value = "user")]
    User = 0,

    [EnumMember(Value = "assistant")]
    Assistant = 1,
}

internal enum AgentChatMessageOrigin
{
    [EnumMember(Value = "user")]
    User = 0,

    [EnumMember(Value = "agent")]
    Agent = 1,

    [EnumMember(Value = "agent-intermediate")]
    AgentIntermediate = 2,

    [EnumMember(Value = "runtime")]
    Runtime = 3,
}
