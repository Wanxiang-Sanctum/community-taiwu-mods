namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatSessionEvent(AgentChatMessage message)
{
    public AgentChatMessage Message { get; } = message;

    public static AgentChatSessionEvent MessageAdded(AgentChatMessage message)
    {
        return new AgentChatSessionEvent(message);
    }
}
