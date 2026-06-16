using Wanxiang.Xiangshu.Frontend.Settings;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal enum AgentAdapter
{
    Codex = 0,
    Claude = 1,
}

internal sealed class AgentSettings
{
    public const string DefaultWorkingDirectoryName = XiangshuRuntimePaths.DefaultAgentWorkingDirectoryName;

    private const string AgentAdapterKey = "AgentAdapter";

    private const string AgentCliPathKey = "AgentCliPath";

    private const string AgentWorkingDirectoryKey = "AgentWorkingDirectory";

    private AgentSettings(
        AgentAdapter adapter,
        string commandPath,
        string modDirectory,
        string workingDirectory,
        IReadOnlyList<AgentEnvironmentVariable> environmentVariables)
    {
        Adapter = adapter;
        CommandPath = commandPath;
        ModDirectory = modDirectory;
        WorkingDirectory = workingDirectory;
        EnvironmentVariables = environmentVariables;
    }

    public AgentAdapter Adapter { get; }

    public string CommandPath { get; }

    public string ModDirectory { get; }

    public string WorkingDirectory { get; }

    public IReadOnlyList<AgentEnvironmentVariable> EnvironmentVariables { get; }

    public static AgentSettings Load(string modIdStr)
    {
        string modDirectory = GetModDirectory(modIdStr);
        AgentAdapter adapter = ReadAgentAdapter(modIdStr);
        string commandPath = ReadCommandPath(modIdStr, adapter);
        string workingDirectory = ReadWorkingDirectory(modIdStr, modDirectory);

        _ = Directory.CreateDirectory(workingDirectory);
        _ = Directory.CreateDirectory(XiangshuRuntimePaths.GetRuntimeDirectory(workingDirectory));

        return new AgentSettings(
            adapter,
            commandPath,
            modDirectory,
            workingDirectory,
            LocalSettingsFile.LoadAgentEnvironmentVariables(modDirectory));
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

        return XiangshuRuntimePaths.ResolveAgentWorkingDirectory(modDirectory, value);
    }

    private static string GetDefaultCommandPath(AgentAdapter adapter)
    {
        if (adapter == AgentAdapter.Claude)
        {
            return "claude";
        }

        return "codex";
    }

    private static string GetModDirectory(string modIdStr)
    {
        return Path.GetFullPath(global::ModManager.GetModInfo(modIdStr).DirectoryName);
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
        ref string value)
    {
        return global::ModManager.GetSetting(modIdStr, key, ref value);
    }
}
