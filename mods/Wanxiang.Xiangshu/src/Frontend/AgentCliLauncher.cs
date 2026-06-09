using System.Diagnostics;
using System.Globalization;
using System.Text;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend;

internal sealed class AgentCliLauncher : IDisposable
{
    private const string DiagnosticPrompt =
        "Use the xiangshu MCP server now. Call the tool xiangshu_check_toolchain, then summarize whether the toolchain is ready and report the frontend, backend, and mcp-server status.";

    private static readonly TimeSpan McpEndpointWaitTimeout = TimeSpan.FromSeconds(10);

    private readonly object _syncRoot = new();

    private Process? _process;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public (bool Started, string Message) TryStartDiagnostic(AgentSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        ThrowIfDisposed();

        lock (_syncRoot)
        {
            if (_process?.HasExited == false)
            {
                return (
                    Started: false,
                    "Wanxiang.Xiangshu agent diagnostic is already running.");
            }

            _process?.Dispose();
            _process = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _ = RunDiagnosticAsync(settings, _cancellationTokenSource.Token);

            return (
                Started: true,
                $"Wanxiang.Xiangshu agent diagnostic started. Logs: {GetDiagnosticDirectory(settings)}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        lock (_syncRoot)
        {
            if (_process is not null)
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }

                _process.Dispose();
                _process = null;
            }
        }
    }

    private async Task RunDiagnosticAsync(
        AgentSettings settings,
        CancellationToken cancellationToken)
    {
        string diagnosticDirectory = GetDiagnosticDirectory(settings);
        _ = Directory.CreateDirectory(diagnosticDirectory);
        string stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string stdoutPath = Path.Combine(diagnosticDirectory, $"{stamp}.stdout.log");
        string stderrPath = Path.Combine(diagnosticDirectory, $"{stamp}.stderr.log");
        string exitPath = Path.Combine(diagnosticDirectory, $"{stamp}.exit.txt");

        try
        {
            IpcEndpoint mcpEndpoint = await WaitForMcpEndpointAsync(cancellationToken)
                .ConfigureAwait(false);
            string mcpUrl = BuildMcpUrl(mcpEndpoint);
            ProcessStartInfo startInfo = BuildStartInfo(
                settings,
                mcpUrl,
                diagnosticDirectory,
                stamp);

            Process process = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false,
            };

            lock (_syncRoot)
            {
                _process = process;
            }

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start the configured agent CLI.");
            }

            if (settings.Adapter == AgentAdapter.Codex)
            {
                await process.StandardInput
                    .WriteAsync(DiagnosticPrompt)
                    .ConfigureAwait(false);
                process.StandardInput.Close();
            }

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            await Task.Run(process.WaitForExit, cancellationToken).ConfigureAwait(false);

            string stdout = await stdoutTask.ConfigureAwait(false);
            string stderr = await stderrTask.ConfigureAwait(false);

            await File.WriteAllTextAsync(stdoutPath, stdout, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(stderrPath, stderr, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                exitPath,
                $"ExitCode: {process.ExitCode.ToString(CultureInfo.InvariantCulture)}{Environment.NewLine}McpUrl: {mcpUrl}{Environment.NewLine}",
                Encoding.UTF8,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await File.WriteAllTextAsync(
                exitPath,
                $"{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}",
                Encoding.UTF8,
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            lock (_syncRoot)
            {
                if (_process?.HasExited != false)
                {
                    _process?.Dispose();
                    _process = null;
                }
            }
        }
    }

    private static ProcessStartInfo BuildStartInfo(
        AgentSettings settings,
        string mcpUrl,
        string diagnosticDirectory,
        string stamp)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = settings.CommandPath,
            WorkingDirectory = settings.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = settings.Adapter == AgentAdapter.Codex,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = !settings.DebugModeEnabled,
        };

        if (settings.Adapter == AgentAdapter.Claude)
        {
            ConfigureClaude(startInfo, mcpUrl, diagnosticDirectory, stamp);
        }
        else
        {
            ConfigureCodex(startInfo, mcpUrl, settings.WorkingDirectory, diagnosticDirectory, stamp);
        }

        return startInfo;
    }

    private static void ConfigureCodex(
        ProcessStartInfo startInfo,
        string mcpUrl,
        string workingDirectory,
        string diagnosticDirectory,
        string stamp)
    {
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--dangerously-bypass-approvals-and-sandbox");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("--output-last-message");
        startInfo.ArgumentList.Add(Path.Combine(diagnosticDirectory, $"{stamp}.last-message.txt"));
        startInfo.ArgumentList.Add("--cd");
        startInfo.ArgumentList.Add(workingDirectory);
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add($"mcp_servers.xiangshu.url=\"{mcpUrl}\"");
        startInfo.ArgumentList.Add("-");
    }

    private static void ConfigureClaude(
        ProcessStartInfo startInfo,
        string mcpUrl,
        string diagnosticDirectory,
        string stamp)
    {
        string mcpConfigPath = Path.Combine(diagnosticDirectory, $"{stamp}.mcp.json");
        File.WriteAllText(
            mcpConfigPath,
            $$"""
            {
              "mcpServers": {
                "xiangshu": {
                  "type": "http",
                  "url": "{{mcpUrl}}"
                }
              }
            }
            """,
            Encoding.UTF8);

        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--dangerously-skip-permissions");
        startInfo.ArgumentList.Add("--mcp-config");
        startInfo.ArgumentList.Add(mcpConfigPath);
        startInfo.ArgumentList.Add(DiagnosticPrompt);
    }

    private static async Task<IpcEndpoint> WaitForMcpEndpointAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + McpEndpointWaitTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            IpcEndpoint? endpoint = IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.McpServerSide);

            if (endpoint is not null)
            {
                return endpoint;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("No live Wanxiang.Xiangshu MCP endpoint was found.");
    }

    private static string BuildMcpUrl(IpcEndpoint endpoint)
    {
        return string.Concat(
            "http://",
            endpoint.Host,
            ":",
            endpoint.Port.ToString(CultureInfo.InvariantCulture),
            endpoint.Path);
    }

    private static string GetDiagnosticDirectory(AgentSettings settings)
    {
        return Path.Combine(settings.WorkingDirectory, "Diagnostics");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentCliLauncher));
        }
    }
}
