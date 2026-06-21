#if NETSTANDARD2_1
using Cysharp.Threading.Tasks;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

public static class RpcPeer
{
    public static void Notify(
        string modId,
        string methodName,
        string payloadJson = "null")
    {
        FrontendTransport.Send(
            Guard.RequiredText(modId, nameof(modId)),
            Guard.RequiredText(methodName, nameof(methodName)),
            ModDataPayload.Create(JsonPayload.Require(payloadJson, nameof(payloadJson))));
    }

    public static void Notify<TPayload>(
        string modId,
        string methodName,
        TPayload payload)
    {
        Notify(modId, methodName, JsonCodec.SerializePayload(payload));
    }

    public static async UniTask<string> InvokeAsync(
        string modId,
        string methodName,
        string payloadJson = "null",
        CancellationToken cancellationToken = default)
    {
        SerializableModData response = await FrontendTransport.InvokeAsync(
            Guard.RequiredText(modId, nameof(modId)),
            Guard.RequiredText(methodName, nameof(methodName)),
            ModDataPayload.Create(JsonPayload.Require(payloadJson, nameof(payloadJson))),
            cancellationToken);

        return WireProtocol.ReadResponsePayload(ModDataPayload.Read(response));
    }

    public static async UniTask<TResponse> InvokeAsync<TResponse>(
        string modId,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            modId,
            methodName,
            JsonPayload.NullJson,
            cancellationToken);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static async UniTask<TResponse> InvokeAsync<TRequest, TResponse>(
        string modId,
        string methodName,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            modId,
            methodName,
            JsonCodec.SerializePayload(payload),
            cancellationToken);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static IDisposable Register(
        string modId,
        string methodName,
        Func<string, string> handler)
    {
        return FrontendDispatcher.RegisterRequestHandler(
            modId,
            methodName,
            handler);
    }

    public static IDisposable Register<TRequest, TResponse>(
        string modId,
        string methodName,
        Func<TRequest, TResponse> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Register(
            modId,
            methodName,
            payloadJson => JsonCodec.SerializePayload(
                handler(JsonCodec.DeserializePayload<TRequest>(payloadJson))));
    }

    public static IDisposable Subscribe(
        string modId,
        string methodName,
        Action<string> handler)
    {
        return FrontendDispatcher.SubscribeNotification(
            modId,
            methodName,
            handler);
    }

    public static IDisposable Subscribe<TPayload>(
        string modId,
        string methodName,
        Action<TPayload> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Subscribe(
            modId,
            methodName,
            payloadJson => handler(JsonCodec.DeserializePayload<TPayload>(payloadJson)));
    }
}
#endif
