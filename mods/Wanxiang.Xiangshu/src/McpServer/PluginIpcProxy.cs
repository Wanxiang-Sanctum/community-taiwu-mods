using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MessagePipe;
using ModelContextProtocol;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.McpServer;

internal static class PluginIpcProxy
{
    private const string ToolchainCheckMessage = "xiangshu toolchain check";

    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ListEndpoints()
    {
        IpcEndpoint[] endpoints =
        [
            .. IpcEndpointRegistry.GetLiveEndpoints()
                .OrderBy(endpoint => endpoint.Side, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(endpoint => endpoint.StartedAtUtc),
        ];

        return JsonSerializer.Serialize(
            new
            {
                manifestPath = IpcEndpointRegistry.GetManifestPath(),
                endpoints,
            },
            JsonOptions);
    }

    public static async Task<string> CheckToolchainAsync(CancellationToken cancellationToken)
    {
        IpcEndpoint[] endpoints =
        [
            .. IpcEndpointRegistry.GetLiveEndpoints()
                .OrderBy(endpoint => endpoint.Side, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(endpoint => endpoint.StartedAtUtc),
        ];

        IpcEndpoint? mcpEndpoint = FindLatestEndpoint(endpoints, IpcRuntime.McpServerSide);
        SideCheckResult frontend = await CheckSideAsync(
            endpoints,
            IpcRuntime.FrontendSide,
            cancellationToken).ConfigureAwait(false);
        SideCheckResult backend = await CheckSideAsync(
            endpoints,
            IpcRuntime.BackendSide,
            cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Serialize(
            new
            {
                checkedAtUtc = DateTimeOffset.UtcNow,
                manifestPath = IpcEndpointRegistry.GetManifestPath(),
                ready = mcpEndpoint is not null
                    && frontend.PingSucceeded
                    && backend.PingSucceeded,
                mcpServer = new
                {
                    registered = mcpEndpoint is not null,
                    endpoint = mcpEndpoint,
                },
                endpoints,
                frontend,
                backend,
            },
            JsonOptions);
    }

    public static async Task<string> PingAsync(
        string side,
        string message,
        CancellationToken cancellationToken)
    {
        string normalizedSide = NormalizeSide(side);
        IpcEndpoint endpoint =
            IpcEndpointRegistry.TryGetLiveEndpoint(normalizedSide)
            ?? throw new McpException(
                $"No live Wanxiang.Xiangshu {normalizedSide} IPC endpoint was found. Start the game mod first.");

        IpcPingResponse response = await InvokePingAsync(
            endpoint,
            message,
            cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Serialize(
            new
            {
                endpoint,
                response,
            },
            JsonOptions);
    }

    [SuppressMessage(
        "Usage",
        "ASP0000:Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'",
        Justification = "Each tool call needs an isolated MessagePipe client configured for the selected manifest endpoint.")]
    private static async Task<IpcPingResponse> InvokePingAsync(
        IpcEndpoint endpoint,
        string message,
        CancellationToken cancellationToken)
    {
        ServiceCollection services = new();
        _ = services
            .AddMessagePipe(
                options =>
                {
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.RequestHandlerLifetime = InstanceLifetime.Singleton;
                })
            .AddTcpInterprocess(
                endpoint.Host,
                endpoint.Port,
                options => options.InstanceLifetime = InstanceLifetime.Singleton);

        ServiceProvider? provider = null;

        try
        {
            provider = services.BuildServiceProvider();
            IRemoteRequestHandler<IpcPingRequest, IpcPingResponse> handler =
                provider.GetRequiredService<IRemoteRequestHandler<IpcPingRequest, IpcPingResponse>>();

            return await handler
                .InvokeAsync(
                    new IpcPingRequest
                    {
                        Message = message ?? string.Empty,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (provider is not null)
            {
                await provider.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The diagnostic tool reports per-side IPC failures as structured data instead of failing the whole MCP call.")]
    private static async Task<SideCheckResult> CheckSideAsync(
        IReadOnlyList<IpcEndpoint> endpoints,
        string side,
        CancellationToken cancellationToken)
    {
        IpcEndpoint? endpoint = FindLatestEndpoint(endpoints, side);

        if (endpoint is null)
        {
            return new SideCheckResult(
                side,
                EndpointFound: false,
                endpoint,
                PingSucceeded: false,
                Response: null,
                Error: $"No live Wanxiang.Xiangshu {side} IPC endpoint was found.");
        }

        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PingTimeout);

        try
        {
            IpcPingResponse response = await InvokePingAsync(
                endpoint,
                ToolchainCheckMessage,
                timeout.Token).ConfigureAwait(false);

            return new SideCheckResult(
                side,
                EndpointFound: true,
                endpoint,
                PingSucceeded: true,
                response,
                Error: null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SideCheckResult(
                side,
                EndpointFound: true,
                endpoint,
                PingSucceeded: false,
                Response: null,
                Error: $"Timed out after {PingTimeout.TotalSeconds} seconds while pinging {side}.");
        }
        catch (Exception ex)
        {
            return new SideCheckResult(
                side,
                EndpointFound: true,
                endpoint,
                PingSucceeded: false,
                Response: null,
                Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string NormalizeSide(string side)
    {
        if (string.IsNullOrWhiteSpace(side))
        {
            throw new McpException("Side must be either 'frontend' or 'backend'.");
        }

        string trimmedSide = side.Trim();

        if (string.Equals(trimmedSide, IpcRuntime.FrontendSide, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.FrontendSide;
        }

        if (string.Equals(trimmedSide, IpcRuntime.BackendSide, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.BackendSide;
        }

        throw new McpException("Side must be either 'frontend' or 'backend'.");
    }

    private static IpcEndpoint? FindLatestEndpoint(
        IEnumerable<IpcEndpoint> endpoints,
        string side)
    {
        return endpoints
            .Where(endpoint => string.Equals(endpoint.Side, side, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(endpoint => endpoint.StartedAtUtc)
            .FirstOrDefault();
    }

    private sealed record SideCheckResult(
        string Side,
        bool EndpointFound,
        IpcEndpoint? Endpoint,
        bool PingSucceeded,
        IpcPingResponse? Response,
        string? Error);
}
