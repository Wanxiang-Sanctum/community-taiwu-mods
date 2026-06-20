using System.Diagnostics.CodeAnalysis;
using GameData.Domains.Item;
using MessagePipe;
using Wanxiang.Xiangshu.Backend.ItemGrafts;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Ipc.ItemGrafts;

namespace Wanxiang.Xiangshu.Backend.Ipc.ItemGrafts;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class RegisterHostHandler
    : IAsyncRequestHandler<RegisterHostRequest, IpcNoContentResponse>
{
    public ValueTask<IpcNoContentResponse> InvokeAsync(
        RegisterHostRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HostRegistration.Register((ItemKey)request.HostKey);

        return ValueTask.FromResult(new IpcNoContentResponse());
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class UnregisterHostHandler
    : IAsyncRequestHandler<UnregisterHostRequest, IpcNoContentResponse>
{
    public ValueTask<IpcNoContentResponse> InvokeAsync(
        UnregisterHostRequest _,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HostRegistration.Unregister();

        return ValueTask.FromResult(new IpcNoContentResponse());
    }
}
