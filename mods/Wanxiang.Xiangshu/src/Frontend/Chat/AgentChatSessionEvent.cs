namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatSessionEvent
{
    private AgentChatSessionEvent(
        AgentChatSessionEventKind kind,
        AgentChatMessage? message)
    {
        Kind = kind;
        Message = message;
    }

    public AgentChatSessionEventKind Kind { get; }

    public AgentChatMessage? Message { get; }

    public static AgentChatSessionEvent MessageAdded(AgentChatMessage message)
    {
        return new AgentChatSessionEvent(AgentChatSessionEventKind.MessageAdded, message);
    }

    public static AgentChatSessionEvent StateChanged()
    {
        return new AgentChatSessionEvent(AgentChatSessionEventKind.StateChanged, message: null);
    }

    public static AgentChatSessionEvent MessagesReset()
    {
        return new AgentChatSessionEvent(AgentChatSessionEventKind.MessagesReset, message: null);
    }
}

internal enum AgentChatSessionEventKind
{
    MessageAdded = 0,
    StateChanged = 1,
    MessagesReset = 2,
}
