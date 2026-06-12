namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatMessage(
    string id,
    AgentChatRole role,
    string speakerName,
    string content,
    string origin)
{
    public string Id { get; } = id;

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
