namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatMessage(
    string id,
    AgentChatRole role,
    string content,
    string origin,
    string? batchId)
{
    public string Id { get; } = id;

    public AgentChatRole Role { get; } = role;

    public string Content { get; } = content;

    public string Origin { get; } = origin;

    public string? BatchId { get; set; } = batchId;
}

internal enum AgentChatRole
{
    User = 0,
    Assistant = 1,
}
