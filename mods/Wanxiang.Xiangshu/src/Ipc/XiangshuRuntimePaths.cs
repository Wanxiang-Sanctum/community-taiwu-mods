using System;
using System.IO;

namespace Wanxiang.Xiangshu.Ipc;

public static class XiangshuRuntimePaths
{
    public const string DefaultAgentWorkingDirectoryName = "AgentWorkspace";

    public const string RuntimeDirectoryName = "XiangshuRuntime";

    public const string IpcManifestFileName = "ipc-endpoints.json";

    public const string DiagnosticsDirectoryName = "Diagnostics";

    public const string McpServerDiagnosticsDirectoryName = "McpServer";

    public static string ResolveAgentWorkingDirectory(
        string modDirectory,
        string? configuredValue)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(modDirectory);
#else
        if (modDirectory is null)
        {
            throw new ArgumentNullException(nameof(modDirectory));
        }
#endif

        string value = string.IsNullOrWhiteSpace(configuredValue)
            ? DefaultAgentWorkingDirectoryName
            : configuredValue.Trim();
        string path = Environment.ExpandEnvironmentVariables(value);

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(modDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    public static string GetRuntimeDirectory(string agentWorkingDirectory)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(agentWorkingDirectory);
#else
        if (agentWorkingDirectory is null)
        {
            throw new ArgumentNullException(nameof(agentWorkingDirectory));
        }
#endif

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
}
