namespace Wanxiang.Xiangshu.Frontend.Agent;

internal enum AgentAdapter
{
    Codex = 0,
    Claude = 1,
    CodeBuddy = 2,
}

internal static class AgentAdapterNames
{
    public static string GetDefaultCommandName(AgentAdapter adapter)
    {
        return (int)adapter switch
        {
            (int)AgentAdapter.Claude => "claude",
            (int)AgentAdapter.CodeBuddy => "codebuddy",
            _ => "codex",
        };
    }

    public static string GetKey(AgentAdapter adapter)
    {
        return (int)adapter switch
        {
            (int)AgentAdapter.Claude => "claude",
            (int)AgentAdapter.CodeBuddy => "codebuddy",
            _ => "codex",
        };
    }
}
