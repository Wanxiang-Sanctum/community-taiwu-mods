using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Ipc.ItemGrafts;

namespace Wanxiang.Xiangshu.Frontend.Ipc.ItemGrafts;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class HostRemovedHandler(Action<HostRemovedRequest> onHostRemoved)
    : IAsyncRequestHandler<HostRemovedRequest, IpcNoContentResponse>
{
    public UniTask<IpcNoContentResponse> InvokeAsync(
        HostRemovedRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        onHostRemoved(request);

        return UniTask.FromResult(new IpcNoContentResponse());
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class InventoryTransferHandler(Action<InventoryTransferRequest> onInventoryTransfer)
    : IAsyncRequestHandler<InventoryTransferRequest, IpcNoContentResponse>
{
    public UniTask<IpcNoContentResponse> InvokeAsync(
        InventoryTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        onInventoryTransfer(request);

        return UniTask.FromResult(new IpcNoContentResponse());
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class TaiwuInventorySnapshotChangedHandler(Action onSnapshotChanged)
    : IAsyncRequestHandler<TaiwuInventorySnapshotChangedRequest, IpcNoContentResponse>
{
    public UniTask<IpcNoContentResponse> InvokeAsync(
        TaiwuInventorySnapshotChangedRequest _,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        onSnapshotChanged();

        return UniTask.FromResult(new IpcNoContentResponse());
    }
}
