#if NETSTANDARD2_1
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class FrontendDispatcher
{
    private static readonly object SyncRoot = new();

    private static readonly Dictionary<string, State> States =
        new(StringComparer.Ordinal);

    internal static IDisposable RegisterRequestHandler(
        string modId,
        string methodName,
        Func<string, string> handler)
    {
        string validatedModId = Guard.RequiredText(modId, nameof(modId));
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            State state = GetOrCreateState(validatedModId);

            if (state.RequestHandlers.ContainsKey(validatedMethodName))
            {
                throw new InvalidOperationException(
                    $"A frontend ModRpc handler is already registered for '{validatedMethodName}'.");
            }

            state.RequestHandlers.Add(validatedMethodName, handler);
        }

        return new HandlerRegistration(
            () => RemoveRequestHandler(validatedModId, validatedMethodName, handler));
    }

    internal static IDisposable SubscribeNotification(
        string modId,
        string methodName,
        Action<string> handler)
    {
        string validatedModId = Guard.RequiredText(modId, nameof(modId));
        string validatedMethodName = Guard.RequiredText(methodName, nameof(methodName));
        Guard.ThrowIfNull(handler, nameof(handler));

        lock (SyncRoot)
        {
            State state = GetOrCreateState(validatedModId);

            if (!state.NotificationHandlers.TryGetValue(validatedMethodName, out List<Action<string>>? handlers))
            {
                handlers = [];
                state.NotificationHandlers.Add(validatedMethodName, handlers);
            }

            handlers.Add(handler);
        }

        return new HandlerRegistration(
            () => RemoveNotificationHandler(validatedModId, validatedMethodName, handler));
    }

    private static State GetOrCreateState(string modId)
    {
        if (States.TryGetValue(modId, out State? state))
        {
            return state;
        }

        void DisplayHandler(string customData)
        {
            HandleDisplayEvent(modId, customData);
        }

        state = new State(DisplayHandler);
        States.Add(modId, state);
        ModManager.RegisterModDisplayEventHandler(modId, DisplayHandler);
        return state;
    }

    private static void HandleDisplayEvent(string modId, string customData)
    {
        if (!WireProtocol.TryDeserializeEnvelope(customData, out Envelope? request)
            || request is null
            || !string.Equals(request.Kind, WireProtocol.RequestKind, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(request.MethodName))
        {
            return;
        }

        Func<string, string>? registeredHandler = null;
        List<Action<string>>? notificationHandlers = null;

        lock (SyncRoot)
        {
            if (!States.TryGetValue(modId, out State? state))
            {
                return;
            }

            _ = state.RequestHandlers.TryGetValue(request.MethodName, out registeredHandler);

            if (state.NotificationHandlers.TryGetValue(request.MethodName, out List<Action<string>>? handlers))
            {
                notificationHandlers = [.. handlers];
            }
        }

        if (request.ExpectsResponse)
        {
            HandleRequest(modId, request, registeredHandler);
            return;
        }

        if (notificationHandlers is not null)
        {
            foreach (Action<string> handler in notificationHandlers)
            {
                InvokeNotificationHandler(handler, request.PayloadJson);
            }
        }

        if (registeredHandler is not null)
        {
            _ = TryInvokeRequestHandler(registeredHandler, request.PayloadJson, out _, out _);
        }
    }

    private static void HandleRequest(
        string modId,
        Envelope request,
        Func<string, string>? registeredHandler)
    {
        if (registeredHandler is null)
        {
            SendResponse(
                modId,
                request.RequestId,
                JsonPayload.NullJson,
                $"No frontend ModRpc handler is registered for '{request.MethodName}'.");
            return;
        }

        _ = TryInvokeRequestHandler(
            registeredHandler,
            request.PayloadJson,
            out string responsePayload,
            out string? error);

        SendResponse(modId, request.RequestId, responsePayload, error);
    }

    private static bool TryInvokeRequestHandler(
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
            return true;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            responsePayload = JsonPayload.NullJson;
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "Frontend ModRpc handler failed."
                : ex.Message;
            return false;
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
        string modId,
        string requestId,
        string payloadJson,
        string? error)
    {
        ModDomainMethod.Call.CallModMethodWithParam(
            modId,
            WireProtocol.ResponseMethodName,
            ModDataPayload.Create(WireProtocol.SerializeResponse(requestId, payloadJson, error)));
    }

    private static void RemoveRequestHandler(
        string modId,
        string methodName,
        Func<string, string> handler)
    {
        lock (SyncRoot)
        {
            if (!States.TryGetValue(modId, out State? state))
            {
                return;
            }

            if (state.RequestHandlers.TryGetValue(methodName, out Func<string, string>? registeredHandler)
                && ReferenceEquals(registeredHandler, handler))
            {
                _ = state.RequestHandlers.Remove(methodName);
            }

            RemoveStateIfEmpty(modId, state);
        }
    }

    private static void RemoveNotificationHandler(
        string modId,
        string methodName,
        Action<string> handler)
    {
        lock (SyncRoot)
        {
            if (!States.TryGetValue(modId, out State? state)
                || !state.NotificationHandlers.TryGetValue(methodName, out List<Action<string>>? handlers))
            {
                return;
            }

            _ = handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _ = state.NotificationHandlers.Remove(methodName);
            }

            RemoveStateIfEmpty(modId, state);
        }
    }

    private static void RemoveStateIfEmpty(string modId, State state)
    {
        if (state.RequestHandlers.Count != 0 || state.NotificationHandlers.Count != 0)
        {
            return;
        }

        _ = States.Remove(modId);
        _ = ModManager.UnRegisterModDisplayEventHandler(modId, state.DisplayHandler);
    }

    private sealed class State(Action<string> displayHandler)
    {
        public Action<string> DisplayHandler { get; } = displayHandler;

        public Dictionary<string, Func<string, string>> RequestHandlers { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, List<Action<string>>> NotificationHandlers { get; } =
            new(StringComparer.Ordinal);
    }
}
#endif
