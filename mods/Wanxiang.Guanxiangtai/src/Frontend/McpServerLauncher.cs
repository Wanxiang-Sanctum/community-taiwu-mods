using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wanxiang.Guanxiangtai.McpServerRuntime;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Frontend;

internal static class McpServerLauncher
{
    private const int DetachedLaunchRequestExitTimeoutMilliseconds = 5000;

    private const int MaxCapturedProcessOutputLength = 4096;

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
                        runtimeDirectory,
                        endpointFilePath = McpServerEndpointRegistry.EndpointFilePath,
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
                        runtimeDirectory,
                    });
                return;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                WorkingDirectory = bundleDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            startInfo.ArgumentList.Add(GuanxiangtaiMcp.DetachedLaunchArgument);

            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                log.Warning(
                    "MCP server 脱离启动请求未创建进程",
                    new
                    {
                        executablePath,
                        runtimeDirectory,
                        endpointFilePath = McpServerEndpointRegistry.EndpointFilePath,
                    });
                return;
            }

            if (!process.WaitForExit(DetachedLaunchRequestExitTimeoutMilliseconds))
            {
                string requestProcessTermination = TryTerminateRequestProcess(process);
                log.Warning(
                    "MCP server 脱离启动请求进程未按预期退出",
                    new
                    {
                        executablePath,
                        processId = process.Id,
                        requestProcessTermination,
                        runtimeDirectory,
                        endpointFilePath = McpServerEndpointRegistry.EndpointFilePath,
                    });
                return;
            }

            string? standardOutput = NormalizeProcessOutput(process.StandardOutput.ReadToEnd());
            string? standardError = NormalizeProcessOutput(process.StandardError.ReadToEnd());

            if (process.ExitCode != 0)
            {
                log.Error(
                    "MCP server 脱离启动请求失败",
                    new
                    {
                        executablePath,
                        processId = process.Id,
                        process.ExitCode,
                        standardOutput,
                        standardError,
                        runtimeDirectory,
                        endpointFilePath = McpServerEndpointRegistry.EndpointFilePath,
                    });
                return;
            }

            log.Info(
                "MCP server 脱离启动请求已提交",
                new
                {
                    executablePath,
                    processId = process.Id,
                    standardOutput,
                    standardError,
                    runtimeDirectory,
                    endpointFilePath = McpServerEndpointRegistry.EndpointFilePath,
                });
        }
        catch (Exception ex)
        {
            log.Error(ex, "MCP server 脱离启动请求失败");
        }
    }

    private static string GetMcpServerBundleDirectory(string modDirectory)
    {
        return Path.Combine(
            Path.GetFullPath(modDirectory),
            ProcessesDirectoryName,
            McpServerBundleName);
    }

    private static string? NormalizeProcessOutput(string output)
    {
        string trimmed = output.Trim();

        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length <= MaxCapturedProcessOutputLength
            ? trimmed
            : trimmed[..MaxCapturedProcessOutputLength] + "\n[truncated]";
    }

    private static string TryTerminateRequestProcess(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return "alreadyExited";
            }

            process.Kill();
            return "killRequested";
        }
        catch (Exception ex) when (ex is InvalidOperationException
            or NotSupportedException
            or Win32Exception)
        {
            return "killFailed:" + ex.GetType().Name;
        }
    }
}
