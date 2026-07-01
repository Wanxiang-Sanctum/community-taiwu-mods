using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Wanxiang.Guanxiangtai.McpServerRuntime;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Frontend;

internal static class McpServerLauncher
{
    private const string ProcessesDirectoryName = "Processes";

    private const string McpServerBundleName = "Wanxiang.Guanxiangtai.McpServer";

    private const string McpServerExecutableName = "Wanxiang.Guanxiangtai.McpServer.exe";

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "MCP server startup is optional at the frontend plugin boundary; failures are logged instead of aborting game initialization.")]
    public static void EnsureStarted(
        string modDirectory,
        TaiwuLogger log)
    {
        try
        {
            string runtimeDirectory = GuanxiangtaiMcpPaths.GetRuntimeDirectory(modDirectory);
            _ = Directory.CreateDirectory(runtimeDirectory);
            McpServerEndpointRegistry.ConfigureRuntimeDirectory(runtimeDirectory);

            if (McpServerEndpointRegistry.TryGetLiveEndpoint() is { } endpoint)
            {
                log.Info(
                    "MCP server 已在运行",
                    new
                    {
                        endpoint.Host,
                        endpoint.Port,
                        endpoint.Path,
                        endpoint.ProcessId,
                    });
                return;
            }

            string bundleDirectory = GetMcpServerBundleDirectory(modDirectory);
            string executablePath = Path.Combine(bundleDirectory, McpServerExecutableName);

            if (!File.Exists(executablePath))
            {
                log.Warning(
                    "未找到 MCP server 可执行文件",
                    new
                    {
                        executablePath,
                    });
                return;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                WorkingDirectory = bundleDirectory,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            using Process? process = Process.Start(startInfo);
            log.Info(
                "MCP server 进程已启动",
                new
                {
                    executablePath,
                    processId = process?.Id,
                });
        }
        catch (Exception ex)
        {
            log.Error(ex, "MCP server 进程启动失败");
        }
    }

    private static string GetMcpServerBundleDirectory(string modDirectory)
    {
        return Path.Combine(
            Path.GetFullPath(modDirectory),
            ProcessesDirectoryName,
            McpServerBundleName);
    }
}
