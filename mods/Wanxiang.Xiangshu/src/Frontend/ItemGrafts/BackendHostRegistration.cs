using Cysharp.Threading.Tasks;
using GameData.Domains.Item;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Ipc.ItemGrafts;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal static class BackendHostRegistration
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly object SyncRoot = new();

    private static bool s_sending;
    private static bool s_hasPending;
    private static HostRegistrationKind s_pendingKind;
    private static ulong s_pendingHostKey;

    public static void RegisterHost(ItemKey hostKey)
    {
        if (!hostKey.IsValid())
        {
            Log.Warning("ignored invalid Xiangshu item graft host registration");
            return;
        }

        QueueRegistrationUpdate(HostRegistrationKind.Register, (ulong)hostKey);
    }

    public static void UnregisterHost()
    {
        QueueRegistrationUpdate(HostRegistrationKind.Unregister, hostKey: 0);
    }

    private static void QueueRegistrationUpdate(
        HostRegistrationKind kind,
        ulong hostKey)
    {
        lock (SyncRoot)
        {
            s_pendingKind = kind;
            s_pendingHostKey = hostKey;
            s_hasPending = true;

            if (s_sending)
            {
                return;
            }

            s_sending = true;
        }

        SendPendingUpdatesAsync().Forget();
    }

    private static async UniTaskVoid SendPendingUpdatesAsync()
    {
        while (true)
        {
            HostRegistrationKind kind;
            ulong hostKey;

            lock (SyncRoot)
            {
                if (!s_hasPending)
                {
                    s_sending = false;
                    return;
                }

                kind = s_pendingKind;
                hostKey = s_pendingHostKey;
                s_hasPending = false;
            }

            await SendRegistrationUpdateAsync(kind, hostKey);
        }
    }

    private static async UniTask SendRegistrationUpdateAsync(
        HostRegistrationKind kind,
        ulong hostKey)
    {
        try
        {
            IpcEndpoint? endpoint =
                IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.BackendEndpointRole);

            if (endpoint is null)
            {
                return;
            }

            using IObjectResolver container = CreateClientContainer(endpoint);

            if (kind == HostRegistrationKind.Register)
            {
                IRemoteRequestHandler<RegisterHostRequest, IpcNoContentResponse> handler =
                    container.Resolve<IRemoteRequestHandler<RegisterHostRequest, IpcNoContentResponse>>();

                _ = await handler.InvokeAsync(
                    new RegisterHostRequest(hostKey),
                    CancellationToken.None);
            }
            else
            {
                IRemoteRequestHandler<UnregisterHostRequest, IpcNoContentResponse> handler =
                    container.Resolve<IRemoteRequestHandler<UnregisterHostRequest, IpcNoContentResponse>>();

                _ = await handler.InvokeAsync(
                    new UnregisterHostRequest(),
                    CancellationToken.None);
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Warning(
                "failed to update backend Xiangshu item graft host registration",
                new
                {
                    exceptionType = ex.GetType().FullName,
                    exceptionMessage = ex.Message,
                    exception = ex.ToString(),
                });
        }
    }

    private static IObjectResolver CreateClientContainer(IpcEndpoint endpoint)
    {
        ContainerBuilder builder = new();
        _ = builder.RegisterMessagePipe(
            options =>
            {
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            endpoint.Host,
            endpoint.Port,
            options =>
            {
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = XiangshuMessagePack.Options;
            });

        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            RegisterHostRequest,
            IpcNoContentResponse>(
            tcpOptions);
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            UnregisterHostRequest,
            IpcNoContentResponse>(
            tcpOptions);

        return builder.Build();
    }

    private enum HostRegistrationKind
    {
        Unregister = 0,
        Register = 1,
    }
}
