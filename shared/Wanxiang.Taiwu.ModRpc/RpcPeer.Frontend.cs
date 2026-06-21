#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

public static class RpcPeer
{
    public static void Bind(string localModId)
    {
        LocalModBinding.Bind(localModId);
        FrontendDispatcher.EnsureDisplayHandler();
    }

    public static void Notify(
        string methodName,
        string payloadJson = "null")
    {
        FrontendTransport.Send(
            LocalModBinding.RequireLocalModId(),
            Guard.RequiredText(methodName, nameof(methodName)),
            ModDataPayload.Create(JsonPayload.Require(payloadJson, nameof(payloadJson))));
    }

    public static void Notify<TPayload>(
        string methodName,
        TPayload payload)
    {
        Notify(methodName, JsonCodec.SerializePayload(payload));
    }

    public static async UniTask<string> InvokeAsync(
        string methodName,
        string payloadJson = "null",
        CancellationToken cancellationToken = default)
    {
        SerializableModData response = await FrontendTransport.InvokeAsync(
            LocalModBinding.RequireLocalModId(),
            Guard.RequiredText(methodName, nameof(methodName)),
            ModDataPayload.Create(JsonPayload.Require(payloadJson, nameof(payloadJson))),
            cancellationToken);

        return WireProtocol.ReadResponsePayload(ModDataPayload.Read(response));
    }

    public static async UniTask<TResponse> InvokeAsync<TResponse>(
        string methodName,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            methodName,
            JsonPayload.NullJson,
            cancellationToken);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static async UniTask<TResponse> InvokeAsync<TRequest, TResponse>(
        string methodName,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            methodName,
            JsonCodec.SerializePayload(payload),
            cancellationToken);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static IDisposable Register(
        string methodName,
        Func<string, string> handler)
    {
        return FrontendDispatcher.RegisterRequestHandler(
            methodName,
            handler);
    }

    public static IDisposable Register<TRequest, TResponse>(
        string methodName,
        Func<TRequest, TResponse> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Register(
            methodName,
            payloadJson => JsonCodec.SerializePayload(
                handler(JsonCodec.DeserializePayload<TRequest>(payloadJson))));
    }

    public static IDisposable Subscribe(
        string methodName,
        Action<string> handler)
    {
        return FrontendDispatcher.SubscribeNotification(
            methodName,
            handler);
    }

    public static IDisposable Subscribe<TPayload>(
        string methodName,
        Action<TPayload> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Subscribe(
            methodName,
            payloadJson => handler(JsonCodec.DeserializePayload<TPayload>(payloadJson)));
    }
}
#endif
