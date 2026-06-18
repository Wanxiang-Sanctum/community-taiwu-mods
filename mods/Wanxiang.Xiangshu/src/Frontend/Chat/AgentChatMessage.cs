namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatMessage(
    string id,
    DateTimeOffset createdAt,
    AgentChatRole role,
    string speakerName,
    string content,
    string origin)
{
    public string Id { get; } = id;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public AgentChatRole Role { get; } = role;

    public string SpeakerName { get; } = speakerName;

    public string Content { get; } = content;

    public string Origin { get; } = origin;
}

internal enum AgentChatRole
{
    User = 0,
    Assistant = 1,
}

internal static class AgentChatRoleNames
{
    public const string User = "user";

    public const string Assistant = "assistant";
}

internal static class AgentChatMessageOrigins
{
    public const string User = "user";

    public const string Agent = "agent";

    public const string AgentIntermediate = "agent-intermediate";

    public const string Runtime = "runtime";
}
