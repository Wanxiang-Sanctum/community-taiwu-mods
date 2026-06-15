using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentCliLauncher : IDisposable
{
    private static readonly TimeSpan McpEndpointDiscoveryWindow = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan McpEndpointPollInterval = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);

    private const int MaxLogExcerptLength = 400;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly object _syncRoot = new();

    private Process? _process;
    private CancellationTokenSource? _activeInvocationCancellation;
    private bool _disposed;

    public async UniTask<AgentCliChatResult> InvokeChatAsync(
        AgentSettings settings,
        AgentChatTurn turn,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken invocationToken = cancellation.Token;

        if (!TryBeginInvocation(cancellation, out string? busyMessage))
        {
            cancellation.Dispose();
            throw new InvalidOperationException(
                busyMessage ?? "Wanxiang.Xiangshu agent invocation is already running.");
        }

        try
        {
            await UniTask.SwitchToThreadPool();
            invocationToken.ThrowIfCancellationRequested();

            string agentInput = AgentChatTurnInputBuilder.Build(turn);
            using AgentCliTempFiles tempFiles = AgentCliTempFiles.Create(settings.WorkingDirectory);
            AgentProcessResult result = await RunInvocationAsync(
                    settings,
                    agentInput,
                    turn.AgentSessionId,
                    tempFiles,
                    useChatReplySchema: true,
                    invocationToken);

            if (result.ExitCode != 0)
            {
                throw new AgentCliFailureException(
                    reason: "nonzero-exit-code",
                    message: "The configured agent CLI exited with code "
                        + result.ExitCode.ToString(CultureInfo.InvariantCulture)
                        + ".",
                    exitCode: result.ExitCode,
                    stderrExcerpt: CreateStderrExcerpt(result.Stderr));
            }

            if (!TryExtractAssistantMessage(settings.Adapter, result, out string? assistantMessage))
            {
                throw new AgentCliFailureException(
                    reason: "invalid-chat-reply",
                    message: "The configured agent CLI did not return a valid chat reply.",
                    exitCode: null,
                    stderrExcerpt: CreateStderrExcerpt(result.Stderr));
            }

            return new AgentCliChatResult(
                assistantMessage.Trim(),
                ExtractAgentSessionId(result.Stdout));
        }
        finally
        {
            CompleteInvocation(cancellation);
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellation;
        Process? process;
        bool invocationOwnsProcess;

        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            cancellation = _activeInvocationCancellation;
            _activeInvocationCancellation = null;
            process = _process;
            _process = null;
            invocationOwnsProcess = cancellation is not null;
        }

        cancellation?.Cancel();

        if (process is not null)
        {
            TryKillProcess(process, reason: "launcher disposed");

            if (!invocationOwnsProcess)
            {
                process.Dispose();
            }
        }
    }

    private async UniTask<AgentProcessResult> RunInvocationAsync(
        AgentSettings settings,
        string agentInput,
        string? agentSessionId,
        AgentCliTempFiles tempFiles,
        bool useChatReplySchema,
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
                agentInput,
                agentSessionId,
                useChatReplySchema);

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
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
                await process.StandardInput.WriteAsync(agentInput);
                process.StandardInput.Close();
            }

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            await WaitForExitAsync(process, cancellationToken);
            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            return new AgentProcessResult(
                stdout,
                stderr,
                process.ExitCode,
                tempFiles.ReadLastMessage());
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process, reason: "invocation cancelled");

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
        string agentInput,
        string? agentSessionId,
        bool useChatReplySchema)
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
            ConfigureClaude(startInfo, mcpUrl, tempFiles, agentInput, agentSessionId, useChatReplySchema);
        }
        else
        {
            string? outputSchemaPath = useChatReplySchema
                ? tempFiles.WriteChatReplySchema()
                : null;
            ConfigureCodex(
                startInfo,
                mcpUrl,
                settings.WorkingDirectory,
                tempFiles.LastMessagePath,
                agentSessionId,
                outputSchemaPath);
        }

        return startInfo;
    }

    private static void ConfigureCodex(
        ProcessStartInfo startInfo,
        string mcpUrl,
        string workingDirectory,
        string lastMessagePath,
        string? agentSessionId,
        string? outputSchemaPath)
    {
        startInfo.ArgumentList.Add("exec");
        if (!string.IsNullOrWhiteSpace(agentSessionId))
        {
            startInfo.ArgumentList.Add("resume");
        }

        startInfo.ArgumentList.Add("--dangerously-bypass-approvals-and-sandbox");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("--output-last-message");
        startInfo.ArgumentList.Add(lastMessagePath);
        if (outputSchemaPath is not null)
        {
            startInfo.ArgumentList.Add("--output-schema");
            startInfo.ArgumentList.Add(outputSchemaPath);
        }

        if (string.IsNullOrWhiteSpace(agentSessionId))
        {
            startInfo.ArgumentList.Add("--cd");
            startInfo.ArgumentList.Add(workingDirectory);
        }

        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add($"mcp_servers.xiangshu.url=\"{mcpUrl}\"");
        if (!string.IsNullOrWhiteSpace(agentSessionId))
        {
            startInfo.ArgumentList.Add(agentSessionId);
        }

        startInfo.ArgumentList.Add("-");
    }

    private static void ConfigureClaude(
        ProcessStartInfo startInfo,
        string mcpUrl,
        AgentCliTempFiles tempFiles,
        string agentInput,
        string? agentSessionId,
        bool useChatReplySchema)
    {
        string mcpConfigPath = tempFiles.WriteClaudeMcpConfig(mcpUrl);

        startInfo.ArgumentList.Add("--print");
        if (!string.IsNullOrWhiteSpace(agentSessionId))
        {
            startInfo.ArgumentList.Add("--resume");
            startInfo.ArgumentList.Add(agentSessionId);
        }

        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--dangerously-skip-permissions");
        startInfo.ArgumentList.Add("--mcp-config");
        startInfo.ArgumentList.Add(mcpConfigPath);
        if (useChatReplySchema)
        {
            startInfo.ArgumentList.Add("--json-schema");
            startInfo.ArgumentList.Add(AgentCliTempFiles.ChatReplySchemaJson);
        }

        startInfo.ArgumentList.Add(agentInput);
    }

    private bool TryBeginInvocation(
        CancellationTokenSource cancellation,
        out string? busyMessage)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();

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

    private static async UniTask<IpcEndpoint> WaitForMcpEndpointAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + McpEndpointDiscoveryWindow;

        while (DateTimeOffset.UtcNow < deadline)
        {
            IpcEndpoint? endpoint = IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.McpServerEndpointRole);

            if (endpoint is not null)
            {
                return endpoint;
            }

            await UniTask.Delay(McpEndpointPollInterval, cancellationToken: cancellationToken);
        }

        throw new InvalidOperationException("No live Wanxiang.Xiangshu MCP endpoint was found.");
    }

    private static async UniTask WaitForExitAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        if (process.HasExited)
        {
            return;
        }

        UniTaskCompletionSource completionSource = new();

        void OnExited(object? sender, EventArgs args)
        {
            _ = sender;
            _ = args;
            _ = completionSource.TrySetResult();
        }

        process.Exited += OnExited;

        try
        {
            if (process.HasExited)
            {
                return;
            }

            await using CancellationTokenRegistration registration = cancellationToken.Register(
                static state => ((UniTaskCompletionSource)state).TrySetCanceled(),
                completionSource);
            await completionSource.Task;
        }
        finally
        {
            process.Exited -= OnExited;
        }
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

    private static bool TryExtractAssistantMessage(
        AgentAdapter adapter,
        AgentProcessResult result,
        [NotNullWhen(true)]
        out string? assistantMessage)
    {
        assistantMessage = null;

        if (adapter == AgentAdapter.Codex
            && !string.IsNullOrWhiteSpace(result.LastMessage))
        {
            return TryExtractChatReply(result.LastMessage, out assistantMessage);
        }

        if (adapter == AgentAdapter.Claude
            && TryExtractClaudeResult(result.Stdout, out string? claudeResult))
        {
            return TryExtractChatReply(claudeResult ?? string.Empty, out assistantMessage);
        }

        return TryExtractChatReply(result.Stdout, out assistantMessage);
    }

    private static bool TryExtractChatReply(
        string value,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return TryExtractChatReply(JToken.Parse(value), out reply);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryExtractChatReply(
        JToken token,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = token is JObject jsonObject
            ? jsonObject["reply"]?.Value<string>()?.Trim()
            : null;
        return !string.IsNullOrWhiteSpace(reply);
    }

    private static string? CreateStderrExcerpt(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length <= MaxLogExcerptLength ? trimmed : trimmed[..MaxLogExcerptLength];
    }

    private static bool TryExtractClaudeResult(
        string stdout,
        out string? result)
    {
        result = null;

        foreach (string line in SplitLines(stdout))
        {
            if (!TryParseJsonLine(line, out JObject? jsonObject))
            {
                continue;
            }

            if (TryReadClaudeStructuredOutput(jsonObject, out string? structuredOutput))
            {
                result = structuredOutput;
                continue;
            }

            if (jsonObject.TryGetValue("result", out JToken? value))
            {
                string? candidate = ConvertClaudeResultToken(value);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    result = candidate;
                }
            }
        }

        return !string.IsNullOrWhiteSpace(result);
    }

    private static bool TryReadClaudeStructuredOutput(
        JObject jsonObject,
        [NotNullWhen(true)]
        out string? result)
    {
        result = null;

        if (!jsonObject.TryGetValue("structured_output", out JToken? value)
            && !jsonObject.TryGetValue("structuredOutput", out value))
        {
            return false;
        }

        string? candidate = ConvertClaudeResultToken(value);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        result = candidate;
        return true;
    }

    private static string? ConvertClaudeResultToken(JToken value)
    {
        return value.Type == JTokenType.String
            ? value.Value<string>()
            : value.ToString(Formatting.None, []);
    }

    private static string? ExtractAgentSessionId(string stdout)
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

    private static void TryKillProcess(
        Process? process,
        string reason)
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
                if (!process.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
                {
                    Log.Warning(
                        "agent CLI process stayed alive after kill",
                        new
                        {
                            reason,
                            process.Id,
                        });
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
        }
    }

    private void ThrowIfDisposed()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposedLocked();
        }
    }

    private void ThrowIfDisposedLocked()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentCliLauncher));
        }
    }
}
