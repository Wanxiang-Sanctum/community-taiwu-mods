#if NET8_0
using GameData.Common;

namespace Wanxiang.Taiwu.ModRpc;

/// <summary>
/// 为单个太吾 mod 的后端侧提供 JSON RPC 辅助入口。
/// </summary>
public static class RpcPeer
{
    /// <summary>
    /// 将此 ModRpc 副本绑定到本地 mod id。
    /// </summary>
    /// <param name="localModId">本地太吾 mod id。</param>
    public static void Bind(string localModId)
    {
        LocalModBinding.Bind(localModId);
    }

    /// <summary>
    /// 向同一 mod 的前端侧发送单向 JSON 通知。
    /// </summary>
    /// <param name="methodName">前端通知 method 名称。</param>
    /// <param name="payloadJson">JSON 载荷文本；JSON null 使用 "null"。</param>
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

    /// <summary>
    /// 序列化载荷 DTO，并作为单向通知发送到同一 mod 的前端侧。
    /// </summary>
    /// <typeparam name="TPayload">载荷 DTO 类型。</typeparam>
    /// <param name="methodName">前端通知 method 名称。</param>
    /// <param name="payload">要序列化为 JSON 的载荷 DTO。</param>
    public static void Notify<TPayload>(
        string methodName,
        TPayload payload)
    {
        Notify(methodName, JsonCodec.SerializePayload(payload));
    }

    /// <summary>
    /// 调用前端请求处理器，并返回 JSON 响应载荷。
    /// </summary>
    /// <param name="methodName">前端请求 method 名称。</param>
    /// <param name="payloadJson">JSON 请求载荷文本；JSON null 使用 "null"。</param>
    /// <param name="cancellationToken">用于停止等待响应的取消令牌。</param>
    /// <returns>返回 JSON 响应载荷的 Task。</returns>
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

    /// <summary>
    /// 使用 JSON null 调用前端请求处理器，并反序列化响应 DTO。
    /// </summary>
    /// <typeparam name="TResponse">响应 DTO 类型。</typeparam>
    /// <param name="methodName">前端请求 method 名称。</param>
    /// <param name="cancellationToken">用于停止等待响应的取消令牌。</param>
    /// <returns>返回反序列化响应 DTO 的 Task。</returns>
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

    /// <summary>
    /// 序列化请求 DTO，调用前端请求处理器，并反序列化响应 DTO。
    /// </summary>
    /// <typeparam name="TRequest">请求 DTO 类型。</typeparam>
    /// <typeparam name="TResponse">响应 DTO 类型。</typeparam>
    /// <param name="methodName">前端请求 method 名称。</param>
    /// <param name="payload">要序列化为 JSON 的请求 DTO。</param>
    /// <param name="cancellationToken">用于停止等待响应的取消令牌。</param>
    /// <returns>返回反序列化响应 DTO 的 Task。</returns>
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

    /// <summary>
    /// 注册可被同一 mod 前端侧调用的后端 JSON 请求处理器。
    /// </summary>
    /// <param name="methodName">后端请求 method 名称。</param>
    /// <param name="handler">接收后端上下文和 JSON 载荷文本，并返回 JSON 载荷文本的处理器。</param>
    /// <returns>释放时移除该处理器的注册对象。</returns>
    public static IDisposable Register(
        string methodName,
        Func<DataContext, string, string> handler)
    {
        return BackendDispatcher.RegisterRequestHandler(
            methodName,
            handler);
    }

    /// <summary>
    /// 注册可被同一 mod 前端侧调用的后端 DTO 请求处理器。
    /// </summary>
    /// <typeparam name="TRequest">请求 DTO 类型。</typeparam>
    /// <typeparam name="TResponse">响应 DTO 类型。</typeparam>
    /// <param name="methodName">后端请求 method 名称。</param>
    /// <param name="handler">接收后端上下文和请求 DTO，并返回响应 DTO 的处理器。</param>
    /// <returns>释放时移除该处理器的注册对象。</returns>
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

    /// <summary>
    /// 订阅同一 mod 前端侧发送给后端的 JSON 通知。
    /// </summary>
    /// <param name="methodName">后端通知 method 名称。</param>
    /// <param name="handler">接收后端上下文和 JSON 载荷文本的处理器。</param>
    /// <returns>释放时移除该处理器的注册对象。</returns>
    public static IDisposable Subscribe(
        string methodName,
        Action<DataContext, string> handler)
    {
        return BackendDispatcher.SubscribeNotification(
            methodName,
            handler);
    }

    /// <summary>
    /// 订阅同一 mod 前端侧发送给后端的 DTO 通知。
    /// </summary>
    /// <typeparam name="TPayload">通知 DTO 类型。</typeparam>
    /// <param name="methodName">后端通知 method 名称。</param>
    /// <param name="handler">接收后端上下文和反序列化载荷 DTO 的处理器。</param>
    /// <returns>释放时移除该处理器的注册对象。</returns>
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
