#if NET8_0
using GameData.Common;

namespace Wanxiang.Taiwu.ModRpc;

public static class RpcPeer
{
    public static void Notify(
        string modId,
        string methodName,
        string payloadJson = "null")
    {
        BackendTransport.PublishDisplayEvent(
            Guard.RequiredText(modId, nameof(modId)),
            WireProtocol.SerializeRequest(
                string.Empty,
                Guard.RequiredText(methodName, nameof(methodName)),
                JsonPayload.Require(payloadJson, nameof(payloadJson)),
                expectsResponse: false));
    }

    public static void Notify<TPayload>(
        string modId,
        string methodName,
        TPayload payload)
    {
        Notify(modId, methodName, JsonCodec.SerializePayload(payload));
    }

    public static Task<string> InvokeAsync(
        string modId,
        string methodName,
        string payloadJson = "null",
        CancellationToken cancellationToken = default)
    {
        return BackendDispatcher.InvokeAsync(
            modId,
            methodName,
            payloadJson,
            cancellationToken);
    }

    public static async Task<TResponse> InvokeAsync<TResponse>(
        string modId,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            modId,
            methodName,
            JsonPayload.NullJson,
            cancellationToken).ConfigureAwait(false);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string modId,
        string methodName,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            modId,
            methodName,
            JsonCodec.SerializePayload(payload),
            cancellationToken).ConfigureAwait(false);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static IDisposable Register(
        string modId,
        string methodName,
        Func<DataContext, string, string> handler)
    {
        return BackendDispatcher.RegisterRequestHandler(
            modId,
            methodName,
            handler);
    }

    public static IDisposable Register<TRequest, TResponse>(
        string modId,
        string methodName,
        Func<DataContext, TRequest, TResponse> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Register(
            modId,
            methodName,
            (context, payloadJson) => JsonCodec.SerializePayload(
                handler(context, JsonCodec.DeserializePayload<TRequest>(payloadJson))));
    }

    public static IDisposable Subscribe(
        string modId,
        string methodName,
        Action<DataContext, string> handler)
    {
        return BackendDispatcher.SubscribeNotification(
            modId,
            methodName,
            handler);
    }

    public static IDisposable Subscribe<TPayload>(
        string modId,
        string methodName,
        Action<DataContext, TPayload> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Subscribe(
            modId,
            methodName,
            (context, payloadJson) => handler(context, JsonCodec.DeserializePayload<TPayload>(payloadJson)));
    }
}
#endif
