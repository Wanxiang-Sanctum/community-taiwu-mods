using System.Diagnostics;
using System.Globalization;

namespace Wanxiang.Xiangshu.Frontend;

internal sealed class McpSidecarProcess(string modDirectory) : IDisposable
{
    private const string ProcessDirectoryName = "Wanxiang.Xiangshu.McpServer";

    private const string ProcessExecutableName = "Wanxiang.Xiangshu.McpServer.exe";

    private Process? _process;
    private bool _disposed;

    public void Start(bool debugModeEnabled)
    {
        ThrowIfDisposed();

        string processDirectory = GetProcessDirectory();
        string executablePath = Path.Combine(processDirectory, ProcessExecutableName);
        Process process = new()
        {
            StartInfo =
            {
                FileName = executablePath,
                WorkingDirectory = processDirectory,
                CreateNoWindow = !debugModeEnabled,
                UseShellExecute = debugModeEnabled,
            },
        };
        process.StartInfo.ArgumentList.Add("--parent-pid");
        process.StartInfo.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start Wanxiang.Xiangshu MCP server process.");
        }

        _process = process;
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

        if (process is null)
        {
            return;
        }

        if (!process.HasExited)
        {
            process.Kill();
        }

        process.Dispose();
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
