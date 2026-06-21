#if NET8_0
using GameData.Common;

namespace Wanxiang.Taiwu.ModRpc;

public static class RpcPeer
{
    public static void Bind(string localModId)
    {
        LocalModBinding.Bind(localModId);
    }

    public static void Notify(
        string methodName,
        string payloadJson = "null")
    {
        BackendTransport.PublishDisplayEvent(
            LocalModBinding.RequireLocalModId(),
            WireProtocol.SerializeRequest(
                string.Empty,
                Guard.RequiredText(methodName, nameof(methodName)),
                JsonPayload.Require(payloadJson, nameof(payloadJson)),
                expectsResponse: false));
    }

    public static void Notify<TPayload>(
        string methodName,
        TPayload payload)
    {
        Notify(methodName, JsonCodec.SerializePayload(payload));
    }

    public static Task<string> InvokeAsync(
        string methodName,
        string payloadJson = "null",
        CancellationToken cancellationToken = default)
    {
        return BackendDispatcher.InvokeAsync(
            methodName,
            payloadJson,
            cancellationToken);
    }

    public static async Task<TResponse> InvokeAsync<TResponse>(
        string methodName,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            methodName,
            JsonPayload.NullJson,
            cancellationToken).ConfigureAwait(false);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string methodName,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        string responseJson = await InvokeAsync(
            methodName,
            JsonCodec.SerializePayload(payload),
            cancellationToken).ConfigureAwait(false);

        return JsonCodec.DeserializePayload<TResponse>(responseJson);
    }

    public static IDisposable Register(
        string methodName,
        Func<DataContext, string, string> handler)
    {
        return BackendDispatcher.RegisterRequestHandler(
            methodName,
            handler);
    }

    public static IDisposable Register<TRequest, TResponse>(
        string methodName,
        Func<DataContext, TRequest, TResponse> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Register(
            methodName,
            (context, payloadJson) => JsonCodec.SerializePayload(
                handler(context, JsonCodec.DeserializePayload<TRequest>(payloadJson))));
    }

    public static IDisposable Subscribe(
        string methodName,
        Action<DataContext, string> handler)
    {
        return BackendDispatcher.SubscribeNotification(
            methodName,
            handler);
    }

    public static IDisposable Subscribe<TPayload>(
        string methodName,
        Action<DataContext, TPayload> handler)
    {
        Guard.ThrowIfNull(handler, nameof(handler));
        return Subscribe(
            methodName,
            (context, payloadJson) => handler(context, JsonCodec.DeserializePayload<TPayload>(payloadJson)));
    }
}
#endif
