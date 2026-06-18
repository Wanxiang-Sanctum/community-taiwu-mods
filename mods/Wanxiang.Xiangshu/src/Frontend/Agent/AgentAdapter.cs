namespace Wanxiang.Xiangshu.Frontend.Agent;

internal enum AgentAdapter
{
    Codex = 0,
    Claude = 1,
    CodeBuddy = 2,
}

internal static class AgentAdapterNames
{
    public static AgentAdapter FromSettingValueOrDefault(int value)
    {
        return value switch
        {
            (int)AgentAdapter.Codex => AgentAdapter.Codex,
            (int)AgentAdapter.Claude => AgentAdapter.Claude,
            (int)AgentAdapter.CodeBuddy => AgentAdapter.CodeBuddy,
            _ => AgentAdapter.Codex,
        };
    }

    public static string GetDefaultCommandName(AgentAdapter adapter)
    {
        return adapter switch
        {
            AgentAdapter.Codex => "codex",
            AgentAdapter.Claude => "claude",
            AgentAdapter.CodeBuddy => "codebuddy",
            _ => throw new ArgumentOutOfRangeException(nameof(adapter), adapter, null),
        };
    }

    public static string GetKey(AgentAdapter adapter)
    {
        return adapter switch
        {
            AgentAdapter.Codex => "codex",
            AgentAdapter.Claude => "claude",
            AgentAdapter.CodeBuddy => "codebuddy",
            _ => throw new ArgumentOutOfRangeException(nameof(adapter), adapter, null),
        };
    }
}
