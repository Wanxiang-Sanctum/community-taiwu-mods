using System.Diagnostics;
using System.Globalization;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Sidecar;

internal sealed class McpSidecarProcess(
    string modDirectory,
    string workingDirectory,
    string manifestPath) : IDisposable
{
    private const string ProcessDirectoryName = "Wanxiang.Xiangshu.McpServer";

    private const string ProcessExecutableName = "Wanxiang.Xiangshu.McpServer.exe";

    private Process? _process;
    private bool _disposed;

    public McpSidecarStartResult Start()
    {
        ThrowIfDisposed();

        string processDirectory = GetProcessDirectory();
        string executablePath = Path.Combine(processDirectory, ProcessExecutableName);
        string logDirectory = XiangshuRuntimePaths.GetMcpServerDiagnosticsDirectory(workingDirectory);

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "Wanxiang.Xiangshu MCP server executable was not found.",
                executablePath);
        }

        string stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string eventLogPath = Path.Combine(logDirectory, $"{stamp}.events.clef");
        Process process = new()
        {
            StartInfo =
            {
                FileName = executablePath,
                WorkingDirectory = processDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("--parent-pid");
        process.StartInfo.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add("--log-file");
        process.StartInfo.ArgumentList.Add(eventLogPath);
        process.StartInfo.ArgumentList.Add("--manifest-file");
        process.StartInfo.ArgumentList.Add(manifestPath);

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Wanxiang.Xiangshu MCP server process.");
            }

            _process = process;

            return new McpSidecarStartResult(
                process.Id,
                logDirectory,
                eventLogPath);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Process? process = _process;
        _process = null;

        if (process is not null)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }

            process.Dispose();
        }
    }

    private string GetProcessDirectory()
    {
        return Path.Combine(
            modDirectory,
            "Processes",
            ProcessDirectoryName);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(McpSidecarProcess));
        }
    }

}

internal sealed class McpSidecarStartResult(
    int processId,
    string logDirectory,
    string eventLogPath)
{
    public int ProcessId { get; } = processId;

    public string LogDirectory { get; } = logDirectory;

    public string EventLogPath { get; } = eventLogPath;
}
