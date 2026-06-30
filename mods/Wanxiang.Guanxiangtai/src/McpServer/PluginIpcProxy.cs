using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
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
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(1);

    public static async Task<string> GetStatusJsonAsync(CancellationToken cancellationToken)
    {
        Task<StatusToolJson.SideStatus> frontendTask = GetSideStatusAsync(
            IpcRuntime.FrontendEndpointRole,
            cancellationToken);
        Task<StatusToolJson.SideStatus> backendTask = GetSideStatusAsync(
            IpcRuntime.BackendEndpointRole,
            cancellationToken);

        StatusToolJson.Response response = new(
            await frontendTask,
            await backendTask);

        return JsonSerializer.Serialize(
            response,
            StatusToolJsonContext.Default.Response);
    }

    public static async Task<string> RunCSharpScriptAsync(
        string targetSide,
        string script,
        string entryThread,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        string normalizedTargetSide = NormalizeTargetSide(targetSide);
        IpcScriptEntryThread parsedEntryThread =
            ParseEntryThread(entryThread);
        IpcEndpoint endpoint = GetRegisteredEndpointOrThrow(normalizedTargetSide);

        IpcRunScriptResponse response = await InvokeAsync<IpcRunScriptRequest, IpcRunScriptResponse>(
            endpoint,
            new IpcRunScriptRequest(
                script,
                ParseArgumentsJson(argumentsJson),
                parsedEntryThread),
            cancellationToken);

        return FormatRunScriptToolResponse(response);
    }

    private static async Task<StatusToolJson.SideStatus> GetSideStatusAsync(
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
            return CreateUnavailableStatus(RoutingErrorReason);
        }

        if (endpoint is null)
        {
            return CreateUnavailableStatus(NotRegisteredReason);
        }

        if (!string.Equals(endpoint.Transport, IpcRuntime.TransportName, StringComparison.OrdinalIgnoreCase))
        {
            return CreateUnavailableStatus(RoutingErrorReason);
        }

        try
        {
            _ = await InvokeAsync<IpcStatusRequest, IpcStatusResponse>(
                endpoint,
                new IpcStatusRequest(),
                cancellationToken,
                StatusTimeout);
            return new StatusToolJson.AvailableStatus();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableStatus(TimeoutReason);
        }
        catch (Exception ex) when (ex is ArgumentException
            or SocketException
            or IOException
            or InvalidOperationException
            or MessagePackSerializationException
            or ObjectDisposedException)
        {
            return CreateUnavailableStatus(UnreachableReason);
        }
    }

    private static StatusToolJson.UnavailableStatus CreateUnavailableStatus(string reason)
    {
        return new StatusToolJson.UnavailableStatus(reason);
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

    private static string NormalizeTargetSide(string targetSide)
    {
        if (string.IsNullOrWhiteSpace(targetSide))
        {
            throw new McpException("targetSide must be either 'frontend' or 'backend'.");
        }

        string trimmedTargetSide = targetSide.Trim();

        if (string.Equals(trimmedTargetSide, IpcRuntime.FrontendEndpointRole, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.FrontendEndpointRole;
        }

        if (string.Equals(trimmedTargetSide, IpcRuntime.BackendEndpointRole, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.BackendEndpointRole;
        }

        throw new McpException("targetSide must be either 'frontend' or 'backend'.");
    }

    private static IpcScriptEntryThread ParseEntryThread(string entryThread)
    {
        if (string.IsNullOrWhiteSpace(entryThread))
        {
            throw new McpException("entryThread must be either 'current' or 'mainThread'.");
        }

        string trimmedEntryThread = entryThread.Trim();

        if (string.Equals(trimmedEntryThread, "current", StringComparison.OrdinalIgnoreCase))
        {
            return IpcScriptEntryThread.Current;
        }

        if (string.Equals(trimmedEntryThread, "mainThread", StringComparison.OrdinalIgnoreCase))
        {
            return IpcScriptEntryThread.MainThread;
        }

        throw new McpException("entryThread must be either 'current' or 'mainThread'.");
    }

    private static Dictionary<string, string> ParseArgumentsJson(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return [];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new McpException("argumentsJson must be a JSON object.");
            }

            Dictionary<string, string> arguments = new(StringComparer.Ordinal);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                arguments[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.GetRawText();
            }

            return arguments;
        }
        catch (JsonException ex)
        {
            throw new McpException("argumentsJson must be a valid JSON object.", ex);
        }
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
