using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Wanxiang.Xiangshu.Frontend;

internal sealed class McpSidecarProcess(
    string modDirectory,
    string workingDirectory,
    string manifestPath) : IDisposable
{
    private const string ProcessDirectoryName = "Wanxiang.Xiangshu.McpServer";

    private const string ProcessExecutableName = "Wanxiang.Xiangshu.McpServer.exe";

    private readonly object _logSyncRoot = new();

    private Process? _process;
    private StreamWriter? _stdoutWriter;
    private StreamWriter? _stderrWriter;
    private bool _disposed;

    public McpSidecarStartResult Start()
    {
        ThrowIfDisposed();

        string processDirectory = GetProcessDirectory();
        string executablePath = Path.Combine(processDirectory, ProcessExecutableName);
        string logDirectory = Path.Combine(workingDirectory, "Diagnostics", "McpServer");
        _ = Directory.CreateDirectory(logDirectory);

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "Wanxiang.Xiangshu MCP server executable was not found.",
                executablePath);
        }

        string stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string stdoutPath = Path.Combine(logDirectory, $"{stamp}.stdout.log");
        string stderrPath = Path.Combine(logDirectory, $"{stamp}.stderr.log");
        string eventLogPath = Path.Combine(logDirectory, $"{stamp}.events.clef");
        StreamWriter stdoutWriter = CreateLogWriter(stdoutPath);
        StreamWriter stderrWriter = CreateLogWriter(stderrPath);
        Process process = new()
        {
            StartInfo =
            {
                FileName = executablePath,
                WorkingDirectory = processDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
            process.OutputDataReceived += (_, args) => WriteStdoutLine(args.Data);
            process.ErrorDataReceived += (_, args) => WriteStderrLine(args.Data);

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Wanxiang.Xiangshu MCP server process.");
            }

            _stdoutWriter = stdoutWriter;
            _stderrWriter = stderrWriter;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _process = process;

            return new McpSidecarStartResult(
                process.Id,
                logDirectory,
                stdoutPath,
                stderrPath,
                eventLogPath);
        }
        catch
        {
            process.Dispose();
            stdoutWriter.Dispose();
            stderrWriter.Dispose();
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

        lock (_logSyncRoot)
        {
            _stdoutWriter?.Dispose();
            _stdoutWriter = null;
            _stderrWriter?.Dispose();
            _stderrWriter = null;
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

    private static StreamWriter CreateLogWriter(string path)
    {
        return new StreamWriter(path, append: false, Encoding.UTF8)
        {
            AutoFlush = true,
        };
    }

    private void WriteStdoutLine(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_logSyncRoot)
        {
            _stdoutWriter?.WriteLine(line);
        }
    }

    private void WriteStderrLine(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_logSyncRoot)
        {
            _stderrWriter?.WriteLine(line);
        }
    }
}

internal sealed class McpSidecarStartResult(
    int processId,
    string logDirectory,
    string stdoutPath,
    string stderrPath,
    string eventLogPath)
{
    public int ProcessId { get; } = processId;

    public string LogDirectory { get; } = logDirectory;

    public string StdoutPath { get; } = stdoutPath;

    public string StderrPath { get; } = stderrPath;

    public string EventLogPath { get; } = eventLogPath;
}
