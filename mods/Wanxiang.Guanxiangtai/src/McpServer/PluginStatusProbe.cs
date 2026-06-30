using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text.Json;
using MessagePack;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Guanxiangtai.Ipc;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class PluginStatusProbe
{
    private const string NotRegisteredReason = "not_registered";
    private const string RoutingErrorReason = "routing_error";
    private const string TimeoutReason = "timeout";
    private const string UnreachableReason = "unreachable";
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(1);

    public static async Task<string> GetStatusJsonAsync(CancellationToken cancellationToken)
    {
        Task<StatusToolJson.SideStatus> frontendTask = GetSideStatusAsync(
            IpcRuntime.FrontendEndpointRole,
            cancellationToken);
        Task<StatusToolJson.SideStatus> backendTask = GetSideStatusAsync(
            IpcRuntime.BackendEndpointRole,
            cancellationToken);

        StatusToolJson.Response response = new(
            await frontendTask,
            await backendTask);

        return JsonSerializer.Serialize(
            response,
            StatusToolJsonContext.Default.Response);
    }

    private static async Task<StatusToolJson.SideStatus> GetSideStatusAsync(
        string role,
        CancellationToken cancellationToken)
    {
        IpcEndpoint? endpoint;

        try
        {
            endpoint = IpcEndpointRegistry.GetRegisteredEndpoint(role);
        }
        catch (Exception ex) when (ex is IOException
            or InvalidDataException
            or UnauthorizedAccessException)
        {
            return CreateUnavailableStatus(RoutingErrorReason);
        }

        if (endpoint is null)
        {
            return CreateUnavailableStatus(NotRegisteredReason);
        }

        if (!string.Equals(endpoint.Transport, IpcRuntime.TransportName, StringComparison.OrdinalIgnoreCase))
        {
            return CreateUnavailableStatus(RoutingErrorReason);
        }

        try
        {
            await InvokeStatusAsync(
                endpoint,
                cancellationToken);
            return new StatusToolJson.AvailableStatus();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableStatus(TimeoutReason);
        }
        catch (Exception ex) when (ex is ArgumentException
            or SocketException
            or IOException
            or InvalidOperationException
            or MessagePackSerializationException
            or ObjectDisposedException)
        {
            return CreateUnavailableStatus(UnreachableReason);
        }
    }

    private static StatusToolJson.UnavailableStatus CreateUnavailableStatus(string reason)
    {
        return new StatusToolJson.UnavailableStatus(reason);
    }

    [SuppressMessage(
        "Usage",
        "ASP0000:Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'",
        Justification = "Each tool call needs an isolated MessagePipe client configured for the selected manifest endpoint.")]
    private static async Task InvokeStatusAsync(
        IpcEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StatusTimeout);

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
                options =>
                {
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.MessagePackSerializerOptions = IpcMessagePack.Options;
                });
        _ = services
            .AddSingleton<IAsyncPublisher<IInterprocessKey, IInterprocessValue>>(
                provider => new AsyncMessageBroker<IInterprocessKey, IInterprocessValue>(
                    new AsyncMessageBrokerCore<IInterprocessKey, IInterprocessValue>(
                        provider.GetRequiredService<MessagePipeDiagnosticsInfo>(),
                        provider.GetRequiredService<MessagePipeOptions>()),
                    provider.GetRequiredService<FilterAttachedAsyncMessageHandlerFactory>()))
            .AddSingleton<TcpWorker>(
                provider => new TcpWorker(
                    provider,
                    provider.GetRequiredService<MessagePipeInterprocessTcpOptions>(),
                    provider.GetRequiredService<IAsyncPublisher<IInterprocessKey, IInterprocessValue>>()))
            .AddSingleton<IRemoteRequestHandler<IpcStatusRequest, IpcStatusResponse>>(
                provider => new TcpRemoteRequestHandler<IpcStatusRequest, IpcStatusResponse>(
                    provider.GetRequiredService<TcpWorker>()));

        ServiceProvider? provider = null;

        try
        {
            provider = services.BuildServiceProvider();
            IRemoteRequestHandler<IpcStatusRequest, IpcStatusResponse> handler =
                provider.GetRequiredService<IRemoteRequestHandler<IpcStatusRequest, IpcStatusResponse>>();

            _ = await handler.InvokeAsync(
                new IpcStatusRequest(),
                timeout.Token);
        }
        finally
        {
            if (provider is not null)
            {
                await provider.DisposeAsync();
            }
        }
    }
}
