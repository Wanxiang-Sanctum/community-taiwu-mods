using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.McpServer;

internal static class PluginIpcProxy
{
    public static async Task<string> SendIntermediateReplyAsync(
        string content,
        CancellationToken cancellationToken)
    {
        IpcEndpoint endpoint =
            IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.FrontendEndpointRole)
            ?? throw new McpException(
                "No live Wanxiang.Xiangshu frontend plugin connection was found. Start the game mod first.");

        _ = await InvokeAsync<IpcIntermediateReplyRequest, IpcNoContentResponse>(
            endpoint,
            new IpcIntermediateReplyRequest(content),
            cancellationToken);

        return "Intermediate reply sent.";
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
        IpcEndpoint endpoint =
            IpcEndpointRegistry.TryGetLiveEndpoint(normalizedTargetSide)
            ?? throw new McpException(
                $"No live Wanxiang.Xiangshu {normalizedTargetSide} plugin connection was found. Start the game mod first.");

        IpcRunScriptResponse response = await InvokeAsync<IpcRunScriptRequest, IpcRunScriptResponse>(
            endpoint,
            new IpcRunScriptRequest(
                script,
                SerializeArgumentsObjectJson(arguments),
                parsedEntryThread),
            cancellationToken);

        return FormatRunScriptToolResponse(response);
    }

    public static Task<IpcCapturePlayerViewResponse> CapturePlayerViewAsync(CancellationToken cancellationToken)
    {
        IpcEndpoint endpoint =
            IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.FrontendEndpointRole)
            ?? throw new McpException(
                "No live Wanxiang.Xiangshu frontend plugin connection was found. Start the game mod first.");

        return InvokeAsync<IpcCapturePlayerViewRequest, IpcCapturePlayerViewResponse>(
            endpoint,
            new IpcCapturePlayerViewRequest(),
            cancellationToken);
    }

    [SuppressMessage(
        "Usage",
        "ASP0000:Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'",
        Justification = "Each tool call needs an isolated MessagePipe client configured for the selected manifest endpoint.")]
    private static async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        IpcEndpoint endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
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
                    options.MessagePackSerializerOptions = XiangshuMessagePack.Options;
                });
        _ = services
            .AddSingleton<IAsyncPublisher<IInterprocessKey, IInterprocessValue>>(
                provider => new AsyncMessageBroker<IInterprocessKey, IInterprocessValue>(
                    new AsyncMessageBrokerCore<IInterprocessKey, IInterprocessValue>(
                        provider.GetRequiredService<MessagePipeDiagnosticsInfo>(),
                        provider.GetRequiredService<MessagePipeOptions>()),
                    provider.GetRequiredService<FilterAttachedAsyncMessageHandlerFactory>()))
            .AddSingleton(
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
                .InvokeAsync(request, cancellationToken);
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
