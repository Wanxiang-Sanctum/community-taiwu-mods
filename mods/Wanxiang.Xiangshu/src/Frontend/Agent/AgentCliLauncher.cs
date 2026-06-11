using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentCliLauncher : IDisposable
{
    private const string DiagnosticPrompt =
        "Use the xiangshu MCP server now. Call the tool xiangshu_check_toolchain, then summarize whether the toolchain is ready and report the frontend, backend, and mcp-server status.";

    private static readonly TimeSpan McpEndpointWaitTimeout = TimeSpan.FromSeconds(10);

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly object _syncRoot = new();

    private Process? _process;
    private CancellationTokenSource? _activeInvocationCancellation;
    private bool _disposed;

    public (bool Started, string Message) TryStartDiagnostic(AgentSettings settings)
    {
        ThrowIfDisposed();

        CancellationTokenSource? cancellation = new();

        try
        {
            if (!TryBeginInvocation(cancellation, out string? busyMessage))
            {
                cancellation.Dispose();
                cancellation = null;
                return (
                    Started: false,
                    busyMessage ?? "Wanxiang.Xiangshu agent diagnostic is already running.");
            }

            CancellationTokenSource invocationCancellation = cancellation;
            cancellation = null;

#pragma warning disable CA2025
            _ = Task.Run(() => RunDiagnosticAsync(settings, invocationCancellation));
#pragma warning restore CA2025

            return (
                Started: true,
                "Wanxiang.Xiangshu agent diagnostic started.");
        }
        finally
        {
            cancellation?.Dispose();
        }
    }

    public async Task<AgentCliInvocationResult> InvokeChatAsync(
        AgentSettings settings,
        AgentChatTurn turn,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!TryBeginInvocation(cancellation, out string? busyMessage))
        {
            cancellation.Dispose();
            throw new InvalidOperationException(
                busyMessage ?? "Wanxiang.Xiangshu agent invocation is already running.");
        }

        try
        {
            string prompt = AgentChatPromptBuilder.BuildPrompt(turn);
            using AgentCliTempFiles tempFiles = AgentCliTempFiles.Create();
            AgentProcessResult result = await RunInvocationAsync(
                    settings,
                    prompt,
                    tempFiles,
                    cancellation.Token);

            if (result.ExitCode != 0)
            {
                string stderrSummary = string.IsNullOrWhiteSpace(result.Stderr)
                    ? string.Empty
                    : " Stderr: " + TrimForException(result.Stderr);
                throw new InvalidOperationException(
                    "The configured agent CLI exited with code "
                    + result.ExitCode.ToString(CultureInfo.InvariantCulture)
                    + "."
                    + stderrSummary);
            }

            string assistantMessage = ExtractAssistantMessage(settings.Adapter, result);

            if (string.IsNullOrWhiteSpace(assistantMessage))
            {
                throw new InvalidOperationException("The configured agent CLI did not return a chat message.");
            }

            return new AgentCliInvocationResult(
                assistantMessage.Trim(),
                ExtractExternalSessionId(result.Stdout));
        }
        finally
        {
            CompleteInvocation(cancellation);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        CancellationTokenSource? cancellation;
        Process? process;

        lock (_syncRoot)
        {
            cancellation = _activeInvocationCancellation;
            _activeInvocationCancellation = null;
            process = _process;
            _process = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();

        if (process is not null)
        {
            TryKill(process);
#pragma warning disable CA1508
            process?.Dispose();
#pragma warning restore CA1508
        }
    }

    private async Task RunDiagnosticAsync(
        AgentSettings settings,
        CancellationTokenSource cancellation)
    {
        using AgentCliTempFiles tempFiles = AgentCliTempFiles.Create();

        try
        {
            _ = await RunInvocationAsync(
                    settings,
                    DiagnosticPrompt,
                    tempFiles,
                    cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "agent diagnostic failed");
        }
        finally
        {
            CompleteInvocation(cancellation);
        }
    }

    private async Task<AgentProcessResult> RunInvocationAsync(
        AgentSettings settings,
        string prompt,
        AgentCliTempFiles tempFiles,
        CancellationToken cancellationToken)
    {
        Process? process = null;

        try
        {
            IpcEndpoint mcpEndpoint = await WaitForMcpEndpointAsync(cancellationToken);
            string mcpUrl = BuildMcpUrl(mcpEndpoint);
            ProcessStartInfo startInfo = BuildStartInfo(
                settings,
                mcpUrl,
                tempFiles,
                prompt);

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false,
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start the configured agent CLI.");
            }

            lock (_syncRoot)
            {
                _process = process;
            }

            if (settings.Adapter == AgentAdapter.Codex)
            {
                await process.StandardInput.WriteAsync(prompt);
                process.StandardInput.Close();
            }

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            await Task.Run(process.WaitForExit, cancellationToken);
            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            LogInvocationResult(settings.Adapter, process.ExitCode, stdout, stderr);

            return new AgentProcessResult(
                stdout,
                stderr,
                process.ExitCode,
                tempFiles.ReadLastMessage());
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            throw;
        }
        finally
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                }
            }

#pragma warning disable CA1508
            process?.Dispose();
#pragma warning restore CA1508
        }
    }

    private static ProcessStartInfo BuildStartInfo(
        AgentSettings settings,
        string mcpUrl,
        AgentCliTempFiles tempFiles,
        string prompt)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = settings.CommandPath,
            WorkingDirectory = settings.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = settings.Adapter == AgentAdapter.Codex,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (settings.Adapter == AgentAdapter.Claude)
        {
            ConfigureClaude(startInfo, mcpUrl, tempFiles, prompt);
        }
        else
        {
            ConfigureCodex(startInfo, mcpUrl, settings.WorkingDirectory, tempFiles.LastMessagePath);
        }

        return startInfo;
    }

    private static void ConfigureCodex(
        ProcessStartInfo startInfo,
        string mcpUrl,
        string workingDirectory,
        string lastMessagePath)
    {
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--dangerously-bypass-approvals-and-sandbox");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("--output-last-message");
        startInfo.ArgumentList.Add(lastMessagePath);
        startInfo.ArgumentList.Add("--cd");
        startInfo.ArgumentList.Add(workingDirectory);
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add($"mcp_servers.xiangshu.url=\"{mcpUrl}\"");
        startInfo.ArgumentList.Add("-");
    }

    private static void ConfigureClaude(
        ProcessStartInfo startInfo,
        string mcpUrl,
        AgentCliTempFiles tempFiles,
        string prompt)
    {
        string mcpConfigPath = tempFiles.WriteClaudeMcpConfig(mcpUrl);

        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--dangerously-skip-permissions");
        startInfo.ArgumentList.Add("--mcp-config");
        startInfo.ArgumentList.Add(mcpConfigPath);
        startInfo.ArgumentList.Add(prompt);
    }

    private bool TryBeginInvocation(
        CancellationTokenSource cancellation,
        out string? busyMessage)
    {
        lock (_syncRoot)
        {
            if (_activeInvocationCancellation is not null || _process?.HasExited == false)
            {
                busyMessage = "Wanxiang.Xiangshu agent invocation is already running.";
                return false;
            }

            _process?.Dispose();
            _process = null;
            _activeInvocationCancellation = cancellation;
            busyMessage = null;
            return true;
        }
    }

    private void CompleteInvocation(CancellationTokenSource cancellation)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_activeInvocationCancellation, cancellation))
            {
                _activeInvocationCancellation = null;
            }
        }

        cancellation.Dispose();
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

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
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

    private static string ExtractAssistantMessage(
        AgentAdapter adapter,
        AgentProcessResult result)
    {
        if (adapter == AgentAdapter.Codex
            && !string.IsNullOrWhiteSpace(result.LastMessage))
        {
            return result.LastMessage;
        }

        if (adapter == AgentAdapter.Claude
            && TryExtractClaudeResult(result.Stdout, out string? claudeResult))
        {
            return claudeResult ?? string.Empty;
        }

        return result.Stdout.Trim();
    }

    private static string TrimForException(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= 400 ? trimmed : trimmed[..400];
    }

    private static void LogInvocationResult(
        AgentAdapter adapter,
        int exitCode,
        string stdout,
        string stderr)
    {
        if (exitCode == 0)
        {
            Log.Info(
                "agent invocation completed",
                new
                {
                    adapter,
                    stdoutLength = stdout.Length,
                    stderrLength = stderr.Length,
                });
            return;
        }

        Log.Error(
            "agent invocation failed",
            new
            {
                adapter,
                exitCode,
                stderr = TrimForException(stderr),
            });
    }

    private static bool TryExtractClaudeResult(
        string stdout,
        out string? result)
    {
        result = null;

        foreach (string line in SplitLines(stdout))
        {
            if (TryParseJsonLine(line, out JObject? jsonObject)
                && TryReadString(jsonObject, "result", out string? value))
            {
                result = value;
            }
        }

        return result is not null;
    }

    private static string? ExtractExternalSessionId(string stdout)
    {
        string? sessionId = null;

        foreach (string line in SplitLines(stdout))
        {
            if (!TryParseJsonLine(line, out JObject? jsonObject))
            {
                continue;
            }

            if (TryReadString(jsonObject, "session_id", out string? value)
                || TryReadString(jsonObject, "sessionId", out value)
                || TryReadFirstDescendantString(jsonObject, "thread_id", out value)
                || TryReadFirstDescendantString(jsonObject, "threadId", out value))
            {
                sessionId = value;
            }
        }

        return sessionId;
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        using StringReader reader = new(value);

        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    private static bool TryParseJsonLine(
        string line,
        [NotNullWhen(true)]
        out JObject? jsonObject)
    {
        try
        {
            jsonObject = JObject.Parse(line);
            return true;
        }
        catch (JsonException)
        {
            jsonObject = null;
            return false;
        }
    }

    private static bool TryReadString(
        JObject jsonObject,
        string propertyName,
        out string? value)
    {
        value = jsonObject[propertyName]?.Value<string>();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadFirstDescendantString(
        JObject jsonObject,
        string propertyName,
        out string? value)
    {
        foreach (JProperty property in jsonObject.Descendants().OfType<JProperty>())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.Ordinal))
            {
                continue;
            }

            value = property.Value.Value<string>();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static void TryKill(Process? process)
    {
        if (process is null)
        {
            return;
        }

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
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentCliLauncher));
        }
    }
}
