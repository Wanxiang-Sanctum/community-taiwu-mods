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

    private static readonly HashSet<string> RegisteredRequestMethods = new(StringComparer.Ordinal);

    private static readonly HashSet<string> RegisteredNotificationMethods = new(StringComparer.Ordinal);

    private static bool s_isResponseHandlerRegistered;

    internal static async Task<string> InvokeAsync(
        string methodName,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string validatedPayloadJson = JsonPayload.Require(payloadJson, nameof(payloadJson));
        string localModId = LocalModBinding.RequireLocalModId();
        string requestId = Guid.NewGuid().ToString("N");
        TaskCompletionSource<string> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        EnsureResponseMethodRegistered(localModId);

        lock (SyncRoot)
        {
            PendingResponses.Add(requestId, completionSource);
        }

        try
        {
            BackendTransport.PublishDisplayEvent(
                localModId,
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
        string methodName,
        Func<DataContext, string, string> handler)
    {
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            EnsureRequestMethodRegistered(LocalModBinding.RequireLocalModId(), validatedMethodName);

            if (RequestHandlers.ContainsKey(validatedMethodName))
            {
                throw new InvalidOperationException(
                    $"A backend ModRpc handler is already registered for '{validatedMethodName}'.");
            }

            RequestHandlers.Add(validatedMethodName, handler);
        }

        return new HandlerRegistration(() => RemoveRequestHandler(validatedMethodName, handler));
    }

    internal static IDisposable SubscribeNotification(
        string methodName,
        Action<DataContext, string> handler)
    {
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            EnsureNotificationMethodRegistered(LocalModBinding.RequireLocalModId(), validatedMethodName);

            if (!NotificationHandlers.TryGetValue(validatedMethodName, out List<Action<DataContext, string>>? handlers))
            {
                handlers = [];
                NotificationHandlers.Add(validatedMethodName, handlers);
            }

            handlers.Add(handler);
        }

        return new HandlerRegistration(() => RemoveNotificationHandler(validatedMethodName, handler));
    }

    private static void EnsureRequestMethodRegistered(
        string localModId,
        string methodName)
    {
        if (RegisteredRequestMethods.Contains(methodName))
        {
            return;
        }

        SerializableModData HandleRequest(DataContext context, SerializableModData payload)
        {
            return DispatchRequest(context, methodName, payload);
        }

        BackendTransport.Register(localModId, methodName, HandleRequest);
        _ = RegisteredRequestMethods.Add(methodName);
    }

    private static void EnsureNotificationMethodRegistered(
        string localModId,
        string methodName)
    {
        if (RegisteredNotificationMethods.Contains(methodName))
        {
            return;
        }

        void HandleNotification(DataContext context, SerializableModData payload)
        {
            DispatchNotification(context, methodName, payload);
        }

        BackendTransport.Register(localModId, methodName, HandleNotification);
        _ = RegisteredNotificationMethods.Add(methodName);
    }

    private static SerializableModData DispatchRequest(
        DataContext context,
        string methodName,
        SerializableModData payload)
    {
        Func<DataContext, string, string>? handler;

        lock (SyncRoot)
        {
            _ = RequestHandlers.TryGetValue(methodName, out handler);
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
        string methodName,
        SerializableModData payload)
    {
        List<Action<DataContext, string>>? handlers = null;

        lock (SyncRoot)
        {
            if (NotificationHandlers.TryGetValue(methodName, out List<Action<DataContext, string>>? registeredHandlers))
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

    private static void EnsureResponseMethodRegistered(string localModId)
    {
        lock (SyncRoot)
        {
            if (s_isResponseHandlerRegistered)
            {
                return;
            }

            static void HandleResponse(DataContext context, SerializableModData payload)
            {
                _ = context;
                CompletePendingResponse(payload);
            }

            BackendTransport.Register(localModId, WireProtocol.ResponseMethodName, HandleResponse);
            s_isResponseHandlerRegistered = true;
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
        string methodName,
        Func<DataContext, string, string> handler)
    {
        lock (SyncRoot)
        {
            if (RequestHandlers.TryGetValue(methodName, out Func<DataContext, string, string>? registeredHandler)
                && ReferenceEquals(registeredHandler, handler))
            {
                _ = RequestHandlers.Remove(methodName);
            }
        }
    }

    private static void RemoveNotificationHandler(
        string methodName,
        Action<DataContext, string> handler)
    {
        lock (SyncRoot)
        {
            if (!NotificationHandlers.TryGetValue(methodName, out List<Action<DataContext, string>>? handlers))
            {
                return;
            }

            _ = handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _ = NotificationHandlers.Remove(methodName);
            }
        }
    }
}
#endif
