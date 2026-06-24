using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using Wanxiang.Xiangshu.Frontend.Agent;
using Wanxiang.Xiangshu.Frontend.Agent.Turn;
using Wanxiang.Xiangshu.Frontend.Mcp;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Settings;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal sealed class AgentCliLauncher(
    McpBearerToken bearerToken) : IDisposable
{
    private const string ProtocolFallbackMessage = "方才回声散乱，未能凝成答复。你可再问一次。";

    private static readonly TimeSpan McpEndpointDiscoveryWindow = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan McpEndpointPollInterval = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);

    private const int MaxLogExcerptLength = 400;

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly McpBearerToken _mcpBearerToken = bearerToken
        ?? throw new ArgumentNullException(nameof(bearerToken));
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

            string turnInputJson = AgentChatTurnInputBuilder.Build(turn);
            using AgentCliTempFiles tempFiles = AgentCliTempFiles.Create(settings.WorkingDirectory);
            IAgentCliAdapter adapter = AgentCliAdapters.Get(settings.Adapter);
            AgentProcessResult result = await RunInvocationAsync(
                    settings,
                    adapter,
                    turnInputJson,
                    turn.AgentSessionId,
                    tempFiles,
                    requireChatReplySchema: true,
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

            if (adapter.HasExplicitErrorResult(result))
            {
                throw new AgentCliFailureException(
                    reason: "agent-error-result",
                    message: "The configured agent CLI returned an error result.",
                    exitCode: null,
                    stderrExcerpt: CreateStderrExcerpt(result.Stderr));
            }

            string? agentSessionId = adapter.ExtractAgentSessionId(result);
            if (string.IsNullOrWhiteSpace(turn.AgentSessionId)
                && string.IsNullOrWhiteSpace(agentSessionId))
            {
                throw new AgentCliFailureException(
                    reason: "missing-agent-session-id",
                    message: "The configured agent CLI did not return a resumable session id.",
                    exitCode: null,
                    stderrExcerpt: CreateStderrExcerpt(result.Stderr));
            }

            bool extractedReply = adapter.TryExtractAssistantMessage(result, out string? assistantMessage);
            return new AgentCliChatResult(
                (assistantMessage ?? ProtocolFallbackMessage).Trim(),
                agentSessionId,
                isProtocolFallback: !extractedReply);
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
        IAgentCliAdapter adapter,
        string turnInputJson,
        string? agentSessionId,
        AgentCliTempFiles tempFiles,
        bool requireChatReplySchema,
        CancellationToken cancellationToken)
    {
        Process? process = null;

        try
        {
            IpcEndpoint mcpEndpoint = await WaitForMcpEndpointAsync(cancellationToken);
            string mcpServerUrl = BuildMcpServerUrl(mcpEndpoint);
            ProcessStartInfo startInfo = BuildStartInfo(
                settings,
                adapter,
                mcpServerUrl,
                tempFiles,
                turnInputJson,
                agentSessionId,
                requireChatReplySchema,
                _mcpBearerToken);

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

            if (adapter.RedirectStandardInput)
            {
                await process.StandardInput.WriteAsync(turnInputJson);
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
        IAgentCliAdapter adapter,
        string mcpServerUrl,
        AgentCliTempFiles tempFiles,
        string turnInputJson,
        string? agentSessionId,
        bool requireChatReplySchema,
        McpBearerToken bearerToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = settings.CommandPath,
            WorkingDirectory = settings.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = adapter.RedirectStandardInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        ApplyEnvironmentVariables(startInfo, settings.EnvironmentVariables);
        startInfo.Environment[IpcRuntime.McpBearerTokenEnvironmentVariable] = bearerToken.Value;

        adapter.ConfigureStartInfo(
            startInfo,
            new AgentCliInvocation(
                settings,
                mcpServerUrl,
                tempFiles,
                turnInputJson,
                agentSessionId,
                requireChatReplySchema,
                bearerToken));

        return startInfo;
    }

    private static void ApplyEnvironmentVariables(
        ProcessStartInfo startInfo,
        IReadOnlyList<AgentEnvironmentVariable> environmentVariables)
    {
        foreach (AgentEnvironmentVariable variable in environmentVariables)
        {
            startInfo.Environment[variable.Name] = variable.Value;
        }
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

    private static string BuildMcpServerUrl(IpcEndpoint endpoint)
    {
        return string.Concat(
            "http://",
            endpoint.Host,
            ":",
            endpoint.Port.ToString(CultureInfo.InvariantCulture),
            endpoint.Path);
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
