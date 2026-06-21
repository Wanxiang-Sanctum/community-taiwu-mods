using Cysharp.Threading.Tasks;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;
using Wanxiang.Taiwu.ModRpc;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 持有一次嫁接的前端生命周期及其后端宿主订阅。
/// </summary>
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

    /// <summary>
    /// 获取本会话持有的活动嫁接状态。
    /// </summary>
    public Graft Graft { get; }

    /// <summary>
    /// 获取本会话是否仍在应用嫁接。
    /// </summary>
    public bool IsActive => EndReason is null;

    /// <summary>
    /// 获取本会话结束的原因；会话仍活动时为 null。
    /// </summary>
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
        Graft.UpdateHostKey(hostEvent.HostKey);

        if (hostEvent is GraftHostRemovedEventArgs)
        {
            End(GraftSessionEndReason.HostRemoved);
        }

        _onHostEvent?.Invoke(hostEvent);
    }

    /// <summary>
    /// 取消本嫁接会话，并释放对应的后端宿主订阅。
    /// </summary>
    /// <returns>本地会话结束后完成的 ValueTask。</returns>
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
            || !GraftHostRpcProtocol.TryDeserializeHostEvent(payloadJson, out GraftHostEventArgs? hostEvent))
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
