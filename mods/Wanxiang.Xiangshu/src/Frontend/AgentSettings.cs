namespace Wanxiang.Xiangshu.Frontend;

internal enum AgentAdapter
{
    Codex = 0,
    Claude = 1,
}

internal sealed class AgentSettings
{
    public const string DefaultWorkingDirectoryName = "AgentWorkspace";

    private const string AgentAdapterKey = "AgentAdapter";

    private const string AgentCliPathKey = "AgentCliPath";

    private const string AgentWorkingDirectoryKey = "AgentWorkingDirectory";

    private const string DebugModeKey = "DebugMode";

    private AgentSettings(
        AgentAdapter adapter,
        string commandPath,
        string workingDirectory,
        bool debugModeEnabled)
    {
        Adapter = adapter;
        CommandPath = commandPath;
        WorkingDirectory = workingDirectory;
        DebugModeEnabled = debugModeEnabled;
    }

    public AgentAdapter Adapter { get; }

    public string CommandPath { get; }

    public string WorkingDirectory { get; }

    public bool DebugModeEnabled { get; }

    public static AgentSettings Load(string modIdStr)
    {
        string modDirectory = GetModDirectory();
        AgentAdapter adapter = ReadAgentAdapter(modIdStr);
        string commandPath = ReadCommandPath(modIdStr, adapter);
        string workingDirectory = ReadWorkingDirectory(modIdStr, modDirectory);
        bool debugModeEnabled = ReadDebugMode(modIdStr);

        _ = Directory.CreateDirectory(workingDirectory);

        return new AgentSettings(adapter, commandPath, workingDirectory, debugModeEnabled);
    }

    private static AgentAdapter ReadAgentAdapter(string modIdStr)
    {
        int value = 0;
        _ = TryGetSetting(modIdStr, AgentAdapterKey, ref value);

        if (value == (int)AgentAdapter.Claude)
        {
            return AgentAdapter.Claude;
        }

        return AgentAdapter.Codex;
    }

    private static string ReadCommandPath(
        string modIdStr,
        AgentAdapter adapter)
    {
        string value = string.Empty;
        _ = TryGetSetting(modIdStr, AgentCliPathKey, ref value);

        return string.IsNullOrWhiteSpace(value)
            ? GetDefaultCommandPath(adapter)
            : value.Trim();
    }

    private static string ReadWorkingDirectory(
        string modIdStr,
        string modDirectory)
    {
        string value = DefaultWorkingDirectoryName;
        _ = TryGetSetting(modIdStr, AgentWorkingDirectoryKey, ref value);

        if (string.IsNullOrWhiteSpace(value))
        {
            value = DefaultWorkingDirectoryName;
        }

        string path = Environment.ExpandEnvironmentVariables(value.Trim());

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(modDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    private static bool ReadDebugMode(string modIdStr)
    {
        bool value = false;
        _ = TryGetSetting(modIdStr, DebugModeKey, ref value);

        return value;
    }

    private static string GetDefaultCommandPath(AgentAdapter adapter)
    {
        if (adapter == AgentAdapter.Claude)
        {
            return "claude";
        }

        return "codex";
    }

    private static string GetModDirectory()
    {
        string pluginDirectory = Path.GetDirectoryName(typeof(AgentSettings).Assembly.Location)
            ?? Environment.CurrentDirectory;

        return Directory.GetParent(pluginDirectory)?.FullName
            ?? pluginDirectory;
    }

    private static bool TryGetSetting(
        string modIdStr,
        string key,
        ref int value)
    {
        return global::ModManager.GetSetting(modIdStr, key, ref value);
    }

    private static bool TryGetSetting(
        string modIdStr,
        string key,
        ref bool value)
    {
        return global::ModManager.GetSetting(modIdStr, key, ref value);
    }

    private static bool TryGetSetting(
        string modIdStr,
        string key,
        ref string value)
    {
        return global::ModManager.GetSetting(modIdStr, key, ref value);
    }
}
