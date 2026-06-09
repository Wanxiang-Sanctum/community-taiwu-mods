using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Wanxiang.Xiangshu.Frontend;

internal sealed class McpSidecarProcess : IDisposable
{
    private const string ProcessDirectoryName = "Wanxiang.Xiangshu.McpServer";

    private const string ProcessExecutableName = "Wanxiang.Xiangshu.McpServer.exe";

    private Process? _process;
    private bool _disposed;

    public void Start()
    {
        ThrowIfDisposed();

        string executablePath = GetExecutablePath();
        Process process = new()
        {
            StartInfo =
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("--parent-pid");
        process.StartInfo.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

        _ = process.Start();
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

    private static string GetExecutablePath()
    {
        string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Environment.CurrentDirectory;
        string modDirectory = Directory.GetParent(pluginDirectory)?.FullName
            ?? pluginDirectory;

        return Path.Combine(
            modDirectory,
            "Processes",
            ProcessDirectoryName,
            ProcessExecutableName);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(McpSidecarProcess));
        }
    }
}
