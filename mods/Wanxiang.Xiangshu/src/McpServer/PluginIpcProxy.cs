using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack.Resolvers;
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
            IpcEndpointRegistry.TryGetLiveEndpoint(IpcRuntime.FrontendSide)
            ?? throw new McpException(
                "No live Wanxiang.Xiangshu frontend IPC endpoint was found. Start the game mod first.");

        _ = await InvokeAsync<IpcIntermediateReplyRequest, IpcIntermediateReplyResponse>(
            endpoint,
            new IpcIntermediateReplyRequest
            {
                Content = content,
            },
            cancellationToken);

        return "Intermediate reply sent.";
    }

    public static async Task<string> ExecuteCSharpScriptAsync(
        string side,
        string script,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        string normalizedSide = NormalizeSide(side);
        IpcEndpoint endpoint =
            IpcEndpointRegistry.TryGetLiveEndpoint(normalizedSide)
            ?? throw new McpException(
                $"No live Wanxiang.Xiangshu {normalizedSide} IPC endpoint was found. Start the game mod first.");

        IpcExecuteScriptResponse response = await InvokeAsync<IpcExecuteScriptRequest, IpcExecuteScriptResponse>(
            endpoint,
            new IpcExecuteScriptRequest
            {
                Script = script,
                Arguments = ParseArgumentsJson(argumentsJson),
            },
            cancellationToken);

        return JsonSerializer.Serialize(
            response,
            XiangshuMcpJsonContext.Default.IpcExecuteScriptResponse);
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
                    options.MessagePackSerializerOptions = StandardResolver.Options;
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

    private static string NormalizeSide(string side)
    {
        if (string.IsNullOrWhiteSpace(side))
        {
            throw new McpException("Side must be either 'frontend' or 'backend'.");
        }

        string trimmedSide = side.Trim();

        if (string.Equals(trimmedSide, IpcRuntime.FrontendSide, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.FrontendSide;
        }

        if (string.Equals(trimmedSide, IpcRuntime.BackendSide, StringComparison.OrdinalIgnoreCase))
        {
            return IpcRuntime.BackendSide;
        }

        throw new McpException("Side must be either 'frontend' or 'backend'.");
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

}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(IpcExecuteScriptResponse))]
internal sealed partial class XiangshuMcpJsonContext : JsonSerializerContext;
