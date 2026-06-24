namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class CurrentChatSessionBinding
{
    private readonly object _syncRoot = new();
    private AgentChatSession? _current;

    public void Bind(AgentChatSession session)
    {
        lock (_syncRoot)
        {
            _current = session;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _current = null;
        }
    }

    public void AddIntermediateReply(string content)
    {
        AgentChatSession session;

        lock (_syncRoot)
        {
            session = _current
                ?? throw new InvalidOperationException(
                    "No Wanxiang.Xiangshu chat session is bound to the active world.");
        }

        session.AddIntermediateReply(content);
    }
}
