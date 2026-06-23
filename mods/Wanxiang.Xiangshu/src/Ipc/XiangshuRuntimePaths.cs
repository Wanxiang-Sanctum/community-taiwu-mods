using System;
using System.Globalization;
using System.IO;

namespace Wanxiang.Xiangshu.Ipc;

public static class XiangshuRuntimePaths
{
    public const string DefaultAgentWorkingDirectoryName = "DefaultAgentWorkspace";

    public const string PluginsDirectoryName = "Plugins";

    public const string RuntimeDirectoryName = ".xiangshu-runtime";

    public const string IpcManifestFileName = "ipc-endpoints.json";

    public const string DiagnosticsDirectoryName = "Diagnostics";

    public const string McpServerDiagnosticsDirectoryName = "McpServer";

    public const string ChatSessionsDirectoryName = "ChatSessions";

    public const string ChatSessionWorldsDirectoryName = "Worlds";

    public const string CurrentChatSessionFileName = "current.json";

    public const string ChatSessionSnapshotsDirectoryName = "sessions";

    public static string ResolveAgentWorkingDirectory(
        string modDirectory,
        string? configuredValue)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(modDirectory);
#else
        if (modDirectory is null)
        {
            throw new ArgumentNullException(nameof(modDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(modDirectory))
        {
            throw new ArgumentException("Mod directory is required.", nameof(modDirectory));
        }

        string value = string.IsNullOrWhiteSpace(configuredValue)
            ? DefaultAgentWorkingDirectoryName
            : configuredValue.Trim();
        string path = Environment.ExpandEnvironmentVariables(value);

        if (string.IsNullOrWhiteSpace(path))
        {
            path = DefaultAgentWorkingDirectoryName;
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(modDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    public static string GetPluginDirectory(
        string modDirectory,
        string pluginDirectoryName)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(modDirectory);
        ArgumentNullException.ThrowIfNull(pluginDirectoryName);
#else
        if (modDirectory is null)
        {
            throw new ArgumentNullException(nameof(modDirectory));
        }

        if (pluginDirectoryName is null)
        {
            throw new ArgumentNullException(nameof(pluginDirectoryName));
        }
#endif

        if (string.IsNullOrWhiteSpace(modDirectory))
        {
            throw new ArgumentException("Mod directory is required.", nameof(modDirectory));
        }

        if (string.IsNullOrWhiteSpace(pluginDirectoryName))
        {
            throw new ArgumentException("Plugin directory name is required.", nameof(pluginDirectoryName));
        }

        return Path.Combine(
            Path.GetFullPath(modDirectory),
            PluginsDirectoryName,
            pluginDirectoryName);
    }

    public static string GetRuntimeDirectory(string agentWorkingDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(agentWorkingDirectory);
#else
        if (agentWorkingDirectory is null)
        {
            throw new ArgumentNullException(nameof(agentWorkingDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(agentWorkingDirectory))
        {
            throw new ArgumentException("Agent working directory is required.", nameof(agentWorkingDirectory));
        }

        return Path.Combine(
            Path.GetFullPath(agentWorkingDirectory),
            RuntimeDirectoryName);
    }

    public static string GetIpcManifestPath(string agentWorkingDirectory)
    {
        return Path.Combine(
            GetRuntimeDirectory(agentWorkingDirectory),
            IpcManifestFileName);
    }

    public static string GetMcpServerDiagnosticsDirectory(string agentWorkingDirectory)
    {
        return Path.Combine(
            GetRuntimeDirectory(agentWorkingDirectory),
            DiagnosticsDirectoryName,
            McpServerDiagnosticsDirectoryName);
    }

    public static string GetChatSessionsDirectory(string agentWorkingDirectory)
    {
        return Path.Combine(
            GetRuntimeDirectory(agentWorkingDirectory),
            ChatSessionsDirectoryName);
    }

    public static string GetWorldChatSessionsDirectory(
        string agentWorkingDirectory,
        uint worldId)
    {
        return Path.Combine(
            GetChatSessionsDirectory(agentWorkingDirectory),
            ChatSessionWorldsDirectoryName,
            FormatWorldChatSessionKey(worldId));
    }

    public static string GetCurrentChatSessionPath(
        string agentWorkingDirectory,
        uint worldId)
    {
        return Path.Combine(
            GetWorldChatSessionsDirectory(agentWorkingDirectory, worldId),
            CurrentChatSessionFileName);
    }

    public static string GetChatSessionSnapshotsDirectory(
        string agentWorkingDirectory,
        uint worldId)
    {
        return Path.Combine(
            GetWorldChatSessionsDirectory(agentWorkingDirectory, worldId),
            ChatSessionSnapshotsDirectoryName);
    }

    private static string FormatWorldChatSessionKey(uint worldId)
    {
        return "world-" + worldId.ToString("x8", CultureInfo.InvariantCulture);
    }
}
