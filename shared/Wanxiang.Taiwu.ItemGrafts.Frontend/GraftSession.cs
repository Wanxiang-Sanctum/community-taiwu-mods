using Cysharp.Threading.Tasks;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;
using Wanxiang.Taiwu.ModRpc;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

public sealed class GraftSession : IAsyncDisposable
{
    private readonly Action<GraftHostEventArgs>? _onHostEvent;

    private IDisposable? _hostEventSubscription;

    private bool _isBackendSubscribed;

    private GraftSession(
        Graft graft,
        IDisposable? hostEventSubscription,
        Action<GraftHostEventArgs>? onHostEvent)
    {
        Graft = graft ?? throw new ArgumentNullException(nameof(graft));
        _hostEventSubscription = hostEventSubscription;
        _onHostEvent = onHostEvent;
    }

    public Graft Graft { get; }

    public bool IsActive => EndReason is null;

    public GraftSessionEndReason? EndReason { get; private set; }

    internal static async Task<GraftSession> CreateAsync(
        Graft graft,
        Action<GraftHostEventArgs>? onHostEvent,
        CancellationToken cancellationToken = default)
    {
        if (graft is null)
        {
            throw new ArgumentNullException(nameof(graft));
        }

        GraftSession session = new(
            graft,
            null,
            onHostEvent);

        session._hostEventSubscription = RpcPeer.Subscribe(
            GraftHostRpcProtocol.HostEventMethodName,
            session.HandleHostEventPayload);

        try
        {
            _ = await RpcPeer.InvokeAsync(
                GraftHostRpcProtocol.SubscribeHostMethodName,
                GraftHostRpcProtocol.CreateHostPayload(graft.HostKey),
                cancellationToken);
            if (session.IsActive)
            {
                session._isBackendSubscribed = true;
            }
        }
        finally
        {
            if (!session._isBackendSubscribed && session.IsActive)
            {
                session._hostEventSubscription.Dispose();
                session._hostEventSubscription = null;
            }
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException("Graft host ended before the session could be established.");
        }

        return session;
    }

    private void HandleHostEvent(GraftHostEventArgs hostEvent)
    {
        if (hostEvent is null)
        {
            throw new ArgumentNullException(nameof(hostEvent));
        }

        if (hostEvent.HostId != Graft.HostId)
        {
            throw new ArgumentException(
                "Host event does not belong to this graft session.",
                nameof(hostEvent));
        }

        Graft.UpdateHostKey(hostEvent.HostKey);

        if (hostEvent is GraftHostRemovedEventArgs)
        {
            End(GraftSessionEndReason.HostRemoved);
        }

        _onHostEvent?.Invoke(hostEvent);
    }

    public async ValueTask DisposeAsync()
    {
        await EndByCallerAsync();
    }

    private async UniTask EndByCallerAsync()
    {
        if (!IsActive)
        {
            return;
        }

        if (_isBackendSubscribed)
        {
            _ = await RpcPeer.InvokeAsync(
                GraftHostRpcProtocol.UnsubscribeHostMethodName,
                GraftHostRpcProtocol.CreateHostPayload(Graft.HostKey),
                CancellationToken.None);
        }

        if (IsActive)
        {
            End(GraftSessionEndReason.Canceled);
        }
    }

    private void HandleHostEventPayload(string payloadJson)
    {
        if (!IsActive
            || !GraftHostRpcProtocol.TryDeserializeHostEvent(payloadJson, out GraftHostEventArgs? hostEvent)
            || hostEvent is null)
        {
            return;
        }

        if (hostEvent.HostId != Graft.HostId)
        {
            return;
        }

        HandleHostEvent(hostEvent);
    }

    private void End(GraftSessionEndReason reason)
    {
        EndReason = reason;
        _isBackendSubscribed = false;
        _hostEventSubscription?.Dispose();
        _hostEventSubscription = null;
    }
}
