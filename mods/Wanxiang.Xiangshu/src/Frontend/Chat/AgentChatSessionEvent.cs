namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatSessionEvent
{
    private AgentChatSessionEvent(
        AgentChatSessionEventKind kind,
        AgentChatMessage? message,
        bool isWorking)
    {
        Kind = kind;
        Message = message;
        IsWorking = isWorking;
    }

    public AgentChatSessionEventKind Kind { get; }

    public AgentChatMessage? Message { get; }

    public bool IsWorking { get; }

    public static AgentChatSessionEvent MessageAdded(AgentChatMessage message)
    {
        return new AgentChatSessionEvent(
            AgentChatSessionEventKind.MessageAdded,
            message,
            isWorking: false);
    }

    public static AgentChatSessionEvent WorkingChanged(bool isWorking)
    {
        return new AgentChatSessionEvent(
            AgentChatSessionEventKind.WorkingChanged,
            message: null,
            isWorking);
    }
}

internal enum AgentChatSessionEventKind
{
    MessageAdded = 0,
    WorkingChanged = 1,
}
