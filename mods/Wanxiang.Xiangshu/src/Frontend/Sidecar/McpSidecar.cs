using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Sidecar;

internal sealed class McpSidecar(
    string modDirectory,
    string workingDirectory,
    string manifestPath) : IDisposable
{
    private const string McpServerBundleName = "Wanxiang.Xiangshu.McpServer";

    private const string McpServerExecutableName = "Wanxiang.Xiangshu.McpServer.exe";

    private Process? _process;
    private bool _disposed;

    public void Start()
    {
        ThrowIfDisposed();

        string bundleDirectory = GetMcpServerBundleDirectory();
        string executablePath = Path.Combine(bundleDirectory, McpServerExecutableName);
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
                WorkingDirectory = bundleDirectory,
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
            TryKill(process);
            process.Dispose();
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    private string GetMcpServerBundleDirectory()
    {
        return Path.Combine(
            modDirectory,
            "Processes",
            McpServerBundleName);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(McpSidecar));
        }
    }
}
