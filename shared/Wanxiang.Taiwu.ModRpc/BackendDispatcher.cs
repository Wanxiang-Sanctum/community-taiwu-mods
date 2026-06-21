#if NET8_0
using GameData.Common;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class BackendDispatcher
{
    private static readonly object SyncRoot = new();

    private static readonly Dictionary<string, TaskCompletionSource<string>> PendingResponses =
        new(StringComparer.Ordinal);

    private static readonly Dictionary<string, Func<DataContext, string, string>> RequestHandlers =
        new(StringComparer.Ordinal);

    private static readonly Dictionary<string, List<Action<DataContext, string>>> NotificationHandlers =
        new(StringComparer.Ordinal);

    private static readonly HashSet<string> RequestMethodKeys = new(StringComparer.Ordinal);

    private static readonly HashSet<string> NotificationMethodKeys = new(StringComparer.Ordinal);

    private static readonly HashSet<string> ResponseHandlerModIds = new(StringComparer.Ordinal);

    internal static async Task<string> InvokeAsync(
        string modId,
        string methodName,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string validatedModId = Guard.RequiredText(modId, nameof(modId));
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string validatedPayloadJson = JsonPayload.Require(payloadJson, nameof(payloadJson));
        string requestId = Guid.NewGuid().ToString("N");
        TaskCompletionSource<string> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        EnsureResponseHandler(validatedModId);

        lock (SyncRoot)
        {
            PendingResponses.Add(requestId, completionSource);
        }

        try
        {
            BackendTransport.PublishDisplayEvent(
                validatedModId,
                WireProtocol.SerializeRequest(
                    requestId,
                    validatedMethodName,
                    validatedPayloadJson,
                    expectsResponse: true));
        }
        catch
        {
            _ = RemovePendingResponse(requestId);
            throw;
        }

        await using CancellationTokenRegistration registration = cancellationToken.Register(
            static state => CancelPendingResponse((string)state!),
            requestId);

        return await completionSource.Task.ConfigureAwait(false);
    }

    internal static IDisposable RegisterRequestHandler(
        string modId,
        string methodName,
        Func<DataContext, string, string> handler)
    {
        string validatedModId = Guard.RequiredText(modId, nameof(modId));
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string key = CreateHandlerKey(validatedModId, validatedMethodName);
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            EnsureRequestMethod(validatedModId, validatedMethodName, key);

            if (RequestHandlers.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"A backend ModRpc handler is already registered for '{validatedMethodName}'.");
            }

            RequestHandlers.Add(key, handler);
        }

        return new HandlerRegistration(() => RemoveRequestHandler(key, handler));
    }

    internal static IDisposable SubscribeNotification(
        string modId,
        string methodName,
        Action<DataContext, string> handler)
    {
        string validatedModId = Guard.RequiredText(modId, nameof(modId));
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string key = CreateHandlerKey(validatedModId, validatedMethodName);
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            EnsureNotificationMethod(validatedModId, validatedMethodName, key);

            if (!NotificationHandlers.TryGetValue(key, out List<Action<DataContext, string>>? handlers))
            {
                handlers = [];
                NotificationHandlers.Add(key, handlers);
            }

            handlers.Add(handler);
        }

        return new HandlerRegistration(() => RemoveNotificationHandler(key, handler));
    }

    private static void EnsureRequestMethod(
        string modId,
        string methodName,
        string key)
    {
        if (RequestMethodKeys.Contains(key))
        {
            return;
        }

        SerializableModData HandleRequest(DataContext context, SerializableModData payload)
        {
            return DispatchRequest(context, key, payload);
        }

        BackendTransport.Register(modId, methodName, HandleRequest);
        _ = RequestMethodKeys.Add(key);
    }

    private static void EnsureNotificationMethod(
        string modId,
        string methodName,
        string key)
    {
        if (NotificationMethodKeys.Contains(key))
        {
            return;
        }

        void HandleNotification(DataContext context, SerializableModData payload)
        {
            DispatchNotification(context, key, payload);
        }

        BackendTransport.Register(modId, methodName, HandleNotification);
        _ = NotificationMethodKeys.Add(key);
    }

    private static SerializableModData DispatchRequest(
        DataContext context,
        string key,
        SerializableModData payload)
    {
        Func<DataContext, string, string>? handler;

        lock (SyncRoot)
        {
            _ = RequestHandlers.TryGetValue(key, out handler);
        }

        if (handler is null)
        {
            return ModDataPayload.Create(WireProtocol.SerializeNativeResponse(
                JsonPayload.NullJson,
                "No backend ModRpc handler is registered."));
        }

        try
        {
            string payloadJson = ModDataPayload.Read(payload);
            string responsePayloadJson = JsonPayload.Require(
                handler(context, payloadJson),
                "responsePayloadJson");

            return ModDataPayload.Create(WireProtocol.SerializeNativeResponse(
                responsePayloadJson,
                null));
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return ModDataPayload.Create(WireProtocol.SerializeNativeResponse(
                JsonPayload.NullJson,
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Backend ModRpc handler failed."
                    : ex.Message));
        }
    }

    private static void DispatchNotification(
        DataContext context,
        string key,
        SerializableModData payload)
    {
        List<Action<DataContext, string>>? handlers = null;

        lock (SyncRoot)
        {
            if (NotificationHandlers.TryGetValue(key, out List<Action<DataContext, string>>? registeredHandlers))
            {
                handlers = [.. registeredHandlers];
            }
        }

        if (handlers is null)
        {
            return;
        }

        string payloadJson;

        try
        {
            payloadJson = ModDataPayload.Read(payload);
        }
        catch (ArgumentException)
        {
            return;
        }

        foreach (Action<DataContext, string> handler in handlers)
        {
            InvokeNotificationHandler(handler, context, payloadJson);
        }
    }

    private static void InvokeNotificationHandler(
        Action<DataContext, string> handler,
        DataContext context,
        string payloadJson)
    {
        try
        {
            handler(context, payloadJson);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
        }
    }

    private static void EnsureResponseHandler(string modId)
    {
        lock (SyncRoot)
        {
            if (ResponseHandlerModIds.Contains(modId))
            {
                return;
            }

            static void HandleResponse(DataContext context, SerializableModData payload)
            {
                _ = context;
                CompletePendingResponse(payload);
            }

            BackendTransport.Register(modId, WireProtocol.ResponseMethodName, HandleResponse);
            _ = ResponseHandlerModIds.Add(modId);
        }
    }

    private static void CompletePendingResponse(SerializableModData payload)
    {
        string responseJson;

        try
        {
            responseJson = ModDataPayload.Read(payload);
        }
        catch (ArgumentException)
        {
            return;
        }

        if (!WireProtocol.TryDeserializeEnvelope(responseJson, out Envelope? response)
            || response is null
            || !string.Equals(response.Kind, WireProtocol.ResponseKind, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(response.RequestId))
        {
            return;
        }

        TaskCompletionSource<string>? completionSource = RemovePendingResponse(response.RequestId);

        if (completionSource is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            _ = completionSource.TrySetException(new ModRpcException(response.Error));
            return;
        }

        _ = completionSource.TrySetResult(response.PayloadJson);
    }

    private static void CancelPendingResponse(string requestId)
    {
        TaskCompletionSource<string>? completionSource = RemovePendingResponse(requestId);
        _ = completionSource?.TrySetCanceled();
    }

    private static TaskCompletionSource<string>? RemovePendingResponse(string requestId)
    {
        lock (SyncRoot)
        {
            if (!PendingResponses.Remove(requestId, out TaskCompletionSource<string>? completionSource))
            {
                return null;
            }

            return completionSource;
        }
    }

    private static void RemoveRequestHandler(
        string key,
        Func<DataContext, string, string> handler)
    {
        lock (SyncRoot)
        {
            if (RequestHandlers.TryGetValue(key, out Func<DataContext, string, string>? registeredHandler)
                && ReferenceEquals(registeredHandler, handler))
            {
                _ = RequestHandlers.Remove(key);
            }
        }
    }

    private static void RemoveNotificationHandler(
        string key,
        Action<DataContext, string> handler)
    {
        lock (SyncRoot)
        {
            if (!NotificationHandlers.TryGetValue(key, out List<Action<DataContext, string>>? handlers))
            {
                return;
            }

            _ = handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _ = NotificationHandlers.Remove(key);
            }
        }
    }

    private static string CreateHandlerKey(string modId, string methodName)
    {
        return modId + "\n" + methodName;
    }
}
#endif
