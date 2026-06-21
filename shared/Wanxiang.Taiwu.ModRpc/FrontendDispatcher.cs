#if NETSTANDARD2_1
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class FrontendDispatcher
{
    private static readonly object SyncRoot = new();

    private static State? s_state;

    internal static void EnsureDisplayHandler()
    {
        string localModId = LocalModBinding.RequireLocalModId();

        lock (SyncRoot)
        {
            _ = GetOrCreateState(localModId);
        }
    }

    internal static IDisposable RegisterRequestHandler(
        string methodName,
        Func<string, string> handler)
    {
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string localModId = LocalModBinding.RequireLocalModId();
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            State state = GetOrCreateState(localModId);

            if (state.RequestHandlers.ContainsKey(validatedMethodName))
            {
                throw new InvalidOperationException(
                    $"A frontend ModRpc handler is already registered for '{validatedMethodName}'.");
            }

            state.RequestHandlers.Add(validatedMethodName, handler);
        }

        return new HandlerRegistration(
            () => RemoveRequestHandler(validatedMethodName, handler));
    }

    internal static IDisposable SubscribeNotification(
        string methodName,
        Action<string> handler)
    {
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        string localModId = LocalModBinding.RequireLocalModId();
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            State state = GetOrCreateState(localModId);

            if (!state.NotificationHandlers.TryGetValue(validatedMethodName, out List<Action<string>>? handlers))
            {
                handlers = [];
                state.NotificationHandlers.Add(validatedMethodName, handlers);
            }

            handlers.Add(handler);
        }

        return new HandlerRegistration(
            () => RemoveNotificationHandler(validatedMethodName, handler));
    }

    private static State GetOrCreateState(string localModId)
    {
        if (s_state is not null)
        {
            return s_state;
        }

        static void DisplayHandler(string customData)
        {
            HandleDisplayEvent(customData);
        }

        State state = new();
        s_state = state;
        ModManager.RegisterModDisplayEventHandler(localModId, DisplayHandler);
        return state;
    }

    private static void HandleDisplayEvent(string customData)
    {
        if (!WireProtocol.TryDeserializeEnvelope(customData, out Envelope? request)
            || request is null
            || !string.Equals(request.Kind, WireProtocol.RequestKind, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(request.MethodName)
            || (request.ExpectsResponse && string.IsNullOrWhiteSpace(request.RequestId)))
        {
            return;
        }

        List<Action<string>>? notificationHandlers = null;
        Func<string, string>? requestHandler = null;

        lock (SyncRoot)
        {
            if (s_state is null)
            {
                return;
            }

            if (request.ExpectsResponse)
            {
                _ = s_state.RequestHandlers.TryGetValue(request.MethodName, out requestHandler);
            }

            if (!request.ExpectsResponse
                && s_state.NotificationHandlers.TryGetValue(request.MethodName, out List<Action<string>>? handlers))
            {
                notificationHandlers = [.. handlers];
            }
        }

        if (request.ExpectsResponse)
        {
            HandleRequest(request, requestHandler);
            return;
        }

        if (notificationHandlers is not null)
        {
            foreach (Action<string> handler in notificationHandlers)
            {
                InvokeNotificationHandler(handler, request.PayloadJson);
            }
        }

    }

    private static void HandleRequest(
        Envelope request,
        Func<string, string>? registeredHandler)
    {
        if (registeredHandler is null)
        {
            SendResponse(
                request.RequestId,
                JsonPayload.NullJson,
                $"No frontend ModRpc handler is registered for '{request.MethodName}'.");
            return;
        }

        InvokeRequestHandler(
            registeredHandler,
            request.PayloadJson,
            out string responsePayload,
            out string? error);

        SendResponse(request.RequestId, responsePayload, error);
    }

    private static void InvokeRequestHandler(
        Func<string, string> handler,
        string payloadJson,
        out string responsePayload,
        out string? error)
    {
        error = null;

        try
        {
            responsePayload = JsonPayload.Require(
                handler(payloadJson),
                nameof(responsePayload));
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            responsePayload = JsonPayload.NullJson;
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "Frontend ModRpc handler failed."
                : ex.Message;
        }
    }

    private static void InvokeNotificationHandler(
        Action<string> handler,
        string payloadJson)
    {
        try
        {
            handler(payloadJson);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
        }
    }

    private static void SendResponse(
        string requestId,
        string payloadJson,
        string? error)
    {
        ModDomainMethod.Call.CallModMethodWithParam(
            LocalModBinding.RequireLocalModId(),
            WireProtocol.ResponseMethodName,
            ModDataPayload.Create(WireProtocol.SerializeResponse(requestId, payloadJson, error)));
    }

    private static void RemoveRequestHandler(
        string methodName,
        Func<string, string> handler)
    {
        lock (SyncRoot)
        {
            if (s_state is null)
            {
                return;
            }

            if (s_state.RequestHandlers.TryGetValue(methodName, out Func<string, string>? registeredHandler)
                && ReferenceEquals(registeredHandler, handler))
            {
                _ = s_state.RequestHandlers.Remove(methodName);
            }
        }
    }

    private static void RemoveNotificationHandler(
        string methodName,
        Action<string> handler)
    {
        lock (SyncRoot)
        {
            if (s_state is null
                || !s_state.NotificationHandlers.TryGetValue(methodName, out List<Action<string>>? handlers))
            {
                return;
            }

            _ = handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _ = s_state.NotificationHandlers.Remove(methodName);
            }
        }
    }

    private sealed class State
    {
        public Dictionary<string, Func<string, string>> RequestHandlers { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, List<Action<string>>> NotificationHandlers { get; } =
            new(StringComparer.Ordinal);
    }
}
#endif
