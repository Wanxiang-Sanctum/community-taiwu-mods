using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePipe;
using ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.McpServer;

internal static class PluginIpcProxy
{
    private const string ToolchainCheckMessage = "xiangshu toolchain check";

    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);

    public static string ListEndpoints()
    {
        IpcEndpoint[] endpoints =
        [
            .. IpcEndpointRegistry.GetLiveEndpoints()
                .OrderBy(endpoint => endpoint.Side, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(endpoint => endpoint.StartedAtUtc),
        ];
        EndpointListResult result = new(
            IpcEndpointRegistry.GetManifestPath(),
            [.. endpoints.Select(DescribeEndpoint)]);

        return JsonSerializer.Serialize(
            result,
            XiangshuMcpJsonContext.Default.EndpointListResult);
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
        EndpointDescription[] endpointDescriptions = [.. endpoints.Select(DescribeEndpoint)];
        SideCheckResult frontend = await CheckSideAsync(
            endpoints,
            IpcRuntime.FrontendSide,
            cancellationToken);
        SideCheckResult backend = await CheckSideAsync(
            endpoints,
            IpcRuntime.BackendSide,
            cancellationToken);
        ToolchainCheckResult result = new(
            DateTimeOffset.UtcNow,
            IpcEndpointRegistry.GetManifestPath(),
            mcpEndpoint is not null
                && frontend.PingSucceeded
                && backend.PingSucceeded,
            new McpServerCheckResult(
                mcpEndpoint is not null,
                mcpEndpoint is null ? null : DescribeEndpoint(mcpEndpoint)),
            endpointDescriptions,
            frontend,
            backend);

        return JsonSerializer.Serialize(
            result,
            XiangshuMcpJsonContext.Default.ToolchainCheckResult);
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
            cancellationToken);
        PingResult result = new(DescribeEndpoint(endpoint), response);

        return JsonSerializer.Serialize(
            result,
            XiangshuMcpJsonContext.Default.PingResult);
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
                    cancellationToken);
        }
        finally
        {
            if (provider is not null)
            {
                await provider.DisposeAsync();
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
                Endpoint: null,
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
                timeout.Token);

            return new SideCheckResult(
                side,
                EndpointFound: true,
                DescribeEndpoint(endpoint),
                PingSucceeded: true,
                response,
                Error: null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SideCheckResult(
                side,
                EndpointFound: true,
                DescribeEndpoint(endpoint),
                PingSucceeded: false,
                Response: null,
                Error: $"Timed out after {PingTimeout.TotalSeconds} seconds while pinging {side}.");
        }
        catch (Exception ex)
        {
            return new SideCheckResult(
                side,
                EndpointFound: true,
                DescribeEndpoint(endpoint),
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

    private static EndpointDescription DescribeEndpoint(IpcEndpoint endpoint)
    {
        return new EndpointDescription(
            endpoint.Side,
            endpoint.Transport,
            endpoint.Host,
            endpoint.Path,
            endpoint.Port,
            endpoint.ProcessId,
            endpoint.StartedAtUtc,
            IpcRuntime.FormatEndpointAddress(endpoint));
    }

    internal sealed record EndpointListResult(
        string ManifestPath,
        IReadOnlyList<EndpointDescription> Endpoints);

    internal sealed record ToolchainCheckResult(
        DateTimeOffset CheckedAtUtc,
        string ManifestPath,
        bool Ready,
        McpServerCheckResult McpServer,
        IReadOnlyList<EndpointDescription> Endpoints,
        SideCheckResult Frontend,
        SideCheckResult Backend);

    internal sealed record McpServerCheckResult(
        bool Registered,
        EndpointDescription? Endpoint);

    internal sealed record PingResult(
        EndpointDescription Endpoint,
        IpcPingResponse Response);

    internal sealed record EndpointDescription(
        string Side,
        string Transport,
        string Host,
        string Path,
        int Port,
        int ProcessId,
        DateTimeOffset StartedAtUtc,
        string Address);

    internal sealed record SideCheckResult(
        string Side,
        bool EndpointFound,
        EndpointDescription? Endpoint,
        bool PingSucceeded,
        IpcPingResponse? Response,
        string? Error);
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(PluginIpcProxy.EndpointListResult))]
[JsonSerializable(typeof(PluginIpcProxy.ToolchainCheckResult))]
[JsonSerializable(typeof(PluginIpcProxy.PingResult))]
internal sealed partial class XiangshuMcpJsonContext : JsonSerializerContext;
