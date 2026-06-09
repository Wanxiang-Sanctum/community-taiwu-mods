using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MessagePipe;
using ModelContextProtocol;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.McpServer;

internal static class PluginIpcProxy
{
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
}
