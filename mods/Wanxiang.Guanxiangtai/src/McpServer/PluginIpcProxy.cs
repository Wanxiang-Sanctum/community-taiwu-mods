using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Guanxiangtai.Ipc;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class PluginIpcProxy
{
    private const string NotRegisteredReason = "not_registered";
    private const string RoutingErrorReason = "routing_error";
    private const string TimeoutReason = "timeout";
    private const string UnreachableReason = "unreachable";
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(1);

    public static async Task<string> GetStatusJsonAsync(CancellationToken cancellationToken)
    {
        PluginRuntimeAvailability availability =
            await GetRuntimeAvailabilityAsync(cancellationToken);

        StatusToolJson.Response response = new(
            ConvertSideAvailability(availability.Frontend),
            ConvertSideAvailability(availability.Backend));

        return JsonSerializer.Serialize(
            response,
            StatusToolJsonContext.Default.Response);
    }

    public static async Task<PluginRuntimeAvailability> GetRuntimeAvailabilityAsync(
        CancellationToken cancellationToken)
    {
        Task<PluginSideAvailability> frontendTask = GetSideAvailabilityAsync(
            IpcRuntime.FrontendEndpointRole,
            cancellationToken);
        Task<PluginSideAvailability> backendTask = GetSideAvailabilityAsync(
            IpcRuntime.BackendEndpointRole,
            cancellationToken);

        return new PluginRuntimeAvailability(
            await frontendTask,
            await backendTask);
    }

    public static async Task<string> RunCSharpScriptAsync(
        McpPluginSide targetSide,
        string script,
        IReadOnlyDictionary<string, JsonElement> arguments,
        McpScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        string normalizedTargetSide = targetSide.ToIpcRole();
        IpcScriptEntryThread parsedEntryThread = entryThread.ToIpcEntryThread();
        IpcEndpoint endpoint = GetRegisteredEndpointOrThrow(normalizedTargetSide);

        IpcRunScriptResponse response = await InvokeAsync<IpcRunScriptRequest, IpcRunScriptResponse>(
            endpoint,
            new IpcRunScriptRequest(
                script,
                SerializeArgumentsObjectJson(arguments),
                parsedEntryThread),
            cancellationToken);

        return FormatRunScriptToolResponse(response);
    }

    public static async Task RequestGameQuitAsync(CancellationToken cancellationToken)
    {
        IpcEndpoint endpoint = GetRegisteredEndpointOrThrow(IpcRuntime.FrontendEndpointRole);

        _ = await InvokeAsync<IpcGameQuitRequest, IpcGameQuitResponse>(
            endpoint,
            new IpcGameQuitRequest(),
            cancellationToken,
            CommandTimeout);
    }

    private static async Task<PluginSideAvailability> GetSideAvailabilityAsync(
        string role,
        CancellationToken cancellationToken)
    {
        IpcEndpoint? endpoint;

        try
        {
            endpoint = IpcEndpointRegistry.GetRegisteredEndpoint(role);
        }
        catch (Exception ex) when (ex is IOException
            or InvalidDataException
            or UnauthorizedAccessException)
        {
            return CreateUnavailableAvailability(RoutingErrorReason);
        }

        if (endpoint is null)
        {
            return CreateUnavailableAvailability(NotRegisteredReason);
        }

        if (!string.Equals(endpoint.Transport, IpcRuntime.TransportName, StringComparison.OrdinalIgnoreCase))
        {
            return CreateUnavailableAvailability(RoutingErrorReason);
        }

        try
        {
            _ = await InvokeAsync<IpcStatusRequest, IpcStatusResponse>(
                endpoint,
                new IpcStatusRequest(),
                cancellationToken,
                StatusTimeout);
            return new PluginSideAvailability(true, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableAvailability(TimeoutReason);
        }
        catch (Exception ex) when (ex is ArgumentException
            or SocketException
            or IOException
            or InvalidOperationException
            or MessagePackSerializationException
            or ObjectDisposedException)
        {
            return CreateUnavailableAvailability(UnreachableReason);
        }
    }

    private static PluginSideAvailability CreateUnavailableAvailability(string reason)
    {
        return new PluginSideAvailability(false, reason);
    }

    private static StatusToolJson.SideStatus ConvertSideAvailability(
        PluginSideAvailability availability)
    {
        return availability.Available
            ? new StatusToolJson.AvailableStatus()
            : new StatusToolJson.UnavailableStatus(availability.Reason ?? UnreachableReason);
    }

    private static IpcEndpoint GetRegisteredEndpointOrThrow(string role)
    {
        IpcEndpoint? endpoint;

        try
        {
            endpoint = IpcEndpointRegistry.GetRegisteredEndpoint(role);
        }
        catch (Exception ex) when (ex is IOException
            or InvalidDataException
            or UnauthorizedAccessException)
        {
            throw new McpException(
                $"Wanxiang.Guanxiangtai {role} IPC endpoint registry could not be read.",
                ex);
        }

        if (endpoint is null)
        {
            throw new McpException(
                $"No registered Wanxiang.Guanxiangtai {role} IPC endpoint was found. Start the game mod first.");
        }

        if (!string.Equals(endpoint.Transport, IpcRuntime.TransportName, StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException(
                $"Registered Wanxiang.Guanxiangtai {role} IPC endpoint uses unsupported transport '{endpoint.Transport}'.");
        }

        return endpoint;
    }

    [SuppressMessage(
        "Usage",
        "ASP0000:Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'",
        Justification = "Each tool call needs an isolated MessagePipe client configured for the selected manifest endpoint.")]
    private static async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        IpcEndpoint endpoint,
        TRequest request,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        using CancellationTokenSource? timeoutSource = timeout is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } timeoutValue)
        {
            timeoutSource!.CancelAfter(timeoutValue);
        }

        CancellationToken effectiveCancellationToken =
            timeoutSource?.Token ?? cancellationToken;

        ServiceCollection services = new();
        _ = services
            .AddMessagePipe(
                options =>
                {
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.RequestHandlerLifetime = InstanceLifetime.Singleton;
                })
            .AddTcpInterprocess(
                endpoint.Host,
                endpoint.Port,
                options =>
                {
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.MessagePackSerializerOptions = IpcMessagePack.Options;
                });
        _ = services
            .AddSingleton<IAsyncPublisher<IInterprocessKey, IInterprocessValue>>(
                provider => new AsyncMessageBroker<IInterprocessKey, IInterprocessValue>(
                    new AsyncMessageBrokerCore<IInterprocessKey, IInterprocessValue>(
                        provider.GetRequiredService<MessagePipeDiagnosticsInfo>(),
                        provider.GetRequiredService<MessagePipeOptions>()),
                    provider.GetRequiredService<FilterAttachedAsyncMessageHandlerFactory>()))
            .AddSingleton<TcpWorker>(
                provider => new TcpWorker(
                    provider,
                    provider.GetRequiredService<MessagePipeInterprocessTcpOptions>(),
                    provider.GetRequiredService<IAsyncPublisher<IInterprocessKey, IInterprocessValue>>()))
            .AddSingleton<IRemoteRequestHandler<TRequest, TResponse>>(
                provider => new TcpRemoteRequestHandler<TRequest, TResponse>(
                    provider.GetRequiredService<TcpWorker>()));

        ServiceProvider? provider = null;

        try
        {
            provider = services.BuildServiceProvider();
            IRemoteRequestHandler<TRequest, TResponse> handler =
                provider.GetRequiredService<IRemoteRequestHandler<TRequest, TResponse>>();

            return await handler
                .InvokeAsync(request, effectiveCancellationToken);
        }
        finally
        {
            if (provider is not null)
            {
                await provider.DisposeAsync();
            }
        }
    }

    private static string SerializeArgumentsObjectJson(
        IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        if (arguments is null)
        {
            throw new McpException("arguments must be a JSON object. Use {} when no arguments are needed.");
        }

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);

        writer.WriteStartObject();
        foreach ((string name, JsonElement value) in arguments)
        {
            writer.WritePropertyName(name);
            value.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string FormatRunScriptToolResponse(IpcRunScriptResponse response)
    {
        RunScriptToolJson.Response toolResponse = response switch
        {
            IpcRunScriptNotInvokedResponse notInvoked =>
                new RunScriptToolJson.NotInvokedResponse(
                    notInvoked.Reason,
                    ConvertNotInvokedDetails(notInvoked.Details)),

            IpcRunScriptInvokedResponse
            {
                Outcome: IpcRunScriptExceptionOutcome exception,
            } =>
                new RunScriptToolJson.InvokedResponse(
                    new RunScriptToolJson.ExceptionOutcome(exception.Message)),

            IpcRunScriptInvokedResponse
            {
                Outcome: IpcRunScriptReturnValueOutcome returnValue,
            } =>
                new RunScriptToolJson.InvokedResponse(
                    new RunScriptToolJson.ReturnValueOutcome(
                        ParseReturnValueJson(returnValue.ReturnValueJson))),

            _ => throw new InvalidOperationException("Unhandled script response union case."),
        };

        return RunScriptToolJson.Serialize(toolResponse);
    }

    private static RunScriptToolJson.NotInvokedDetails? ConvertNotInvokedDetails(
        IpcRunScriptNotInvokedDetails? details)
    {
        if (details is null)
        {
            return null;
        }

        return new RunScriptToolJson.NotInvokedDetails(
            details.ReferenceDiagnostics,
            details.CompilationDiagnostics);
    }

    private static JsonElement ParseReturnValueJson(string returnValueJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(returnValueJson);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new McpException("Script returned invalid JSON.", ex);
        }
    }
}

internal static class RunScriptToolJson
{
    public static string Serialize(Response response)
    {
        return JsonSerializer.Serialize(
            response,
            typeof(Response),
            RunScriptToolJsonContext.Default);
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(NotInvokedResponse), "notInvoked")]
    [JsonDerivedType(typeof(InvokedResponse), "invoked")]
    internal abstract class Response;

    internal sealed class NotInvokedResponse(
        string reason,
        NotInvokedDetails? details) : Response
    {
        public string Reason { get; } =
            reason ?? throw new ArgumentNullException(nameof(reason));

        public NotInvokedDetails? Details { get; } = details;
    }

    internal sealed class NotInvokedDetails(
        IReadOnlyList<string> referenceDiagnostics,
        IReadOnlyList<string> compilationDiagnostics)
    {
        public IReadOnlyList<string> ReferenceDiagnostics { get; } =
            referenceDiagnostics ?? throw new ArgumentNullException(nameof(referenceDiagnostics));

        public IReadOnlyList<string> CompilationDiagnostics { get; } =
            compilationDiagnostics ?? throw new ArgumentNullException(nameof(compilationDiagnostics));
    }

    internal sealed class InvokedResponse(
        InvokedOutcome outcome) : Response
    {
        public InvokedOutcome Outcome { get; } =
            outcome ?? throw new ArgumentNullException(nameof(outcome));
    }

    [
        JsonPolymorphic(TypeDiscriminatorPropertyName = "kind"),
        JsonDerivedType(typeof(ReturnValueOutcome), "returnValue"),
        JsonDerivedType(typeof(ExceptionOutcome), "exception"),
    ]
    internal abstract class InvokedOutcome;

    internal sealed class ReturnValueOutcome(JsonElement value) : InvokedOutcome
    {
        public JsonElement Value { get; } = value;
    }

    internal sealed class ExceptionOutcome(string message) : InvokedOutcome
    {
        public string Message { get; } =
            message ?? throw new ArgumentNullException(nameof(message));
    }
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(RunScriptToolJson.Response))]
internal sealed partial class RunScriptToolJsonContext : JsonSerializerContext;
