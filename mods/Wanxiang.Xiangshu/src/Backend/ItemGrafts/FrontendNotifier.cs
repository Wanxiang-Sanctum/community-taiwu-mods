using GameData.Domains.Item;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Ipc.ItemGrafts;

namespace Wanxiang.Xiangshu.Backend.ItemGrafts;

internal static class FrontendNotifier
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    public static void NotifyHostRemoved(ItemKey hostKey)
    {
        ulong serializedHostKey = (ulong)hostKey;
        _ = Task.Run(() => NotifyHostRemovedAsync(
            new HostRemovedRequest(serializedHostKey),
            "removed Xiangshu item graft host"));
    }

    public static void NotifyHostInventoryTransfer(
        ItemKey hostKey,
        int fromCharacterId,
        int toCharacterId,
        int amount,
        string eventName)
    {
        ulong serializedHostKey = (ulong)hostKey;
        _ = Task.Run(() => NotifyHostInventoryTransferAsync(
            new InventoryTransferRequest(
                serializedHostKey,
                fromCharacterId,
                toCharacterId,
                amount),
            eventName));
    }

    public static void NotifyTaiwuInventorySnapshotChanged()
    {
        _ = Task.Run(() => NotifyTaiwuInventorySnapshotChangedAsync(
            new TaiwuInventorySnapshotChangedRequest(),
            "Taiwu inventory snapshot change"));
    }

    private static async Task NotifyHostRemovedAsync(
        HostRemovedRequest request,
        string eventName)
    {
        await NotifyFrontendAsync<HostRemovedRequest>(request, eventName);
    }

    private static async Task NotifyHostInventoryTransferAsync(
        InventoryTransferRequest request,
        string eventName)
    {
        await NotifyFrontendAsync<InventoryTransferRequest>(request, eventName);
    }

    private static async Task NotifyTaiwuInventorySnapshotChangedAsync(
        TaiwuInventorySnapshotChangedRequest request,
        string eventName)
    {
        await NotifyFrontendAsync<TaiwuInventorySnapshotChangedRequest>(request, eventName);
    }

    private static async Task NotifyFrontendAsync<TRequest>(
        TRequest request,
        string eventName)
        where TRequest : class
    {
        try
        {
            IpcEndpoint? endpoint =
                IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.FrontendEndpointRole);

            if (endpoint is null)
            {
                return;
            }

            await using ServiceProvider provider = CreateClientProvider<TRequest>(endpoint);
            IRemoteRequestHandler<TRequest, IpcNoContentResponse> handler =
                provider.GetRequiredService<IRemoteRequestHandler<TRequest, IpcNoContentResponse>>();

            _ = await handler.InvokeAsync(request, CancellationToken.None);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Warning(
                $"failed to notify frontend about {eventName}",
                new
                {
                    exceptionType = ex.GetType().FullName,
                    exceptionMessage = ex.Message,
                    exception = ex.ToString(),
                });
        }
    }

    private static ServiceProvider CreateClientProvider<TRequest>(IpcEndpoint endpoint)
        where TRequest : class
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
                options =>
                {
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.MessagePackSerializerOptions = XiangshuMessagePack.Options;
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
            .AddSingleton<IRemoteRequestHandler<TRequest, IpcNoContentResponse>>(
                provider => new TcpRemoteRequestHandler<TRequest, IpcNoContentResponse>(
                    provider.GetRequiredService<TcpWorker>()));

        return services.BuildServiceProvider();
    }
}
