namespace Wanxiang.Xiangshu.Frontend.Agent;

internal enum AgentAdapter
{
    Codex = 0,
    CodeBuddy = 1,
}

internal sealed class AgentAdapterDefinition(
    AgentAdapter adapter,
    int settingValue,
    string key,
    string defaultCommandName)
{
    public AgentAdapter Adapter { get; } = adapter;

    public int SettingValue { get; } = settingValue;

    public string Key { get; } = key;

    public string DefaultCommandName { get; } = defaultCommandName;
}

internal static class AgentAdapterCatalog
{
    private static readonly AgentAdapterDefinition[] Definitions =
    [
        new(
            AgentAdapter.Codex,
            settingValue: 0,
            key: "codex",
            defaultCommandName: "codex"),
        new(
            AgentAdapter.CodeBuddy,
            settingValue: 1,
            key: "codebuddy",
            defaultCommandName: "codebuddy"),
    ];

    public static IReadOnlyList<AgentAdapterDefinition> All { get; } = Definitions;

    public static AgentAdapter FromSettingValueOrDefault(int value)
    {
        foreach (AgentAdapterDefinition definition in Definitions)
        {
            if (definition.SettingValue == value)
            {
                return definition.Adapter;
            }
        }

        return AgentAdapter.Codex;
    }

    public static string GetDefaultCommandName(AgentAdapter adapter)
    {
        return GetRequired(adapter).DefaultCommandName;
    }

    public static string GetKey(AgentAdapter adapter)
    {
        return GetRequired(adapter).Key;
    }

    private static AgentAdapterDefinition GetRequired(AgentAdapter adapter)
    {
        foreach (AgentAdapterDefinition definition in Definitions)
        {
            if (definition.Adapter == adapter)
            {
                return definition;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(adapter), adapter, null);
    }
}
