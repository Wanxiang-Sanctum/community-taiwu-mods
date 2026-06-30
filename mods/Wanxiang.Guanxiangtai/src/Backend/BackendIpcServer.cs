using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using MessagePipe;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Guanxiangtai.Ipc;
using Wanxiang.Guanxiangtai.Scripting;
using Wanxiang.Taiwu.DynamicScripting;

namespace Wanxiang.Guanxiangtai.Backend;

internal sealed class BackendIpcServer(
    string pluginDirectory,
    IDynamicScriptEntryDispatcher scriptEntryDispatcher) : IDisposable
{
    private const int MaxStartAttempts = 8;

    private ServiceProvider? _provider;
    private IpcEndpointRegistration? _registration;
    private IpcEndpoint? _endpoint;
    private bool _disposed;

    public IpcEndpoint Start()
    {
        ThrowIfDisposed();

        if (_provider is not null)
        {
            return _endpoint!;
        }

        DynamicScriptReferenceOptions scriptReferenceOptions =
            CreateScriptReferenceOptions(pluginDirectory);
        Exception? lastException = null;

        for (int attempt = 0; attempt < MaxStartAttempts; attempt++)
        {
            int port = IpcRuntime.ReserveLoopbackPort();

            try
            {
                return StartOnPort(port, scriptReferenceOptions);
            }
            catch (SocketException ex)
            {
                lastException = ex;
            }
            catch (InvalidOperationException ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException("Failed to start Wanxiang.Guanxiangtai backend IPC server.", lastException);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _registration?.Dispose();
        _registration = null;
        _endpoint = null;
        _provider?.Dispose();
        _provider = null;
    }

    private IpcEndpoint StartOnPort(
        int port,
        DynamicScriptReferenceOptions scriptReferenceOptions)
    {
        ServiceCollection services = new();
        IMessagePipeBuilder messagePipeBuilder = services.AddMessagePipe(
            options =>
            {
                options.EnableAutoRegistration = false;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });
        RegisterStatusHandler(services);
        RegisterIpcScriptHandler(
            services,
            scriptReferenceOptions,
            scriptEntryDispatcher);
        _ = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = IpcMessagePack.Options;
            });

        ServiceProvider provider = services.BuildServiceProvider();
        bool started = false;

        try
        {
            provider.GetRequiredService<TcpWorker>().StartReceiver();
            IpcEndpoint endpoint = new()
            {
                Role = IpcRuntime.BackendEndpointRole,
                Transport = IpcRuntime.TransportName,
                Host = IpcRuntime.LoopbackHost,
                Port = port,
                StartedAt = DateTimeOffset.UtcNow,
            };
            IpcEndpointRegistration registration = IpcEndpointRegistry.Register(endpoint);

            _provider = provider;
            _registration = registration;
            _endpoint = endpoint;
            started = true;
            return endpoint;
        }
        finally
        {
            if (!started)
            {
                provider.Dispose();
            }
        }
    }

    private static void RegisterStatusHandler(IServiceCollection services)
    {
        _ = services
            .AddSingleton<IAsyncRequestHandlerCore<IpcStatusRequest, IpcStatusResponse>, BackendStatusHandler>()
            .AddSingleton<IAsyncRequestHandler<IpcStatusRequest, IpcStatusResponse>>(
                provider => new AsyncRequestHandler<IpcStatusRequest, IpcStatusResponse>(
                    provider.GetRequiredService<IAsyncRequestHandlerCore<IpcStatusRequest, IpcStatusResponse>>(),
                    provider.GetRequiredService<FilterAttachedAsyncRequestHandlerFactory>()));
        AsyncRequestHandlerRegistory.Add(
            typeof(IpcStatusRequest),
            typeof(IpcStatusResponse),
            typeof(BackendStatusHandler));
    }

    private static void RegisterIpcScriptHandler(
        IServiceCollection services,
        DynamicScriptReferenceOptions scriptReferenceOptions,
        IDynamicScriptEntryDispatcher scriptEntryDispatcher)
    {
        _ = services
            .AddSingleton(
                new GuanxiangtaiScriptRunner(
                    new ScriptRunnerOptions(
                        IpcRuntime.BackendEndpointRole,
                        references: scriptReferenceOptions),
                    scriptEntryDispatcher))
            .AddSingleton<IAsyncRequestHandlerCore<IpcRunScriptRequest, IpcRunScriptResponse>, BackendExecuteScriptHandler>()
            .AddSingleton<IAsyncRequestHandler<IpcRunScriptRequest, IpcRunScriptResponse>>(
                provider => new AsyncRequestHandler<IpcRunScriptRequest, IpcRunScriptResponse>(
                    provider.GetRequiredService<IAsyncRequestHandlerCore<IpcRunScriptRequest, IpcRunScriptResponse>>(),
                    provider.GetRequiredService<FilterAttachedAsyncRequestHandlerFactory>()));
        AsyncRequestHandlerRegistory.Add(
            typeof(IpcRunScriptRequest),
            typeof(IpcRunScriptResponse),
            typeof(BackendExecuteScriptHandler));
    }

    private static DynamicScriptReferenceOptions CreateScriptReferenceOptions(string pluginDirectory)
    {
        return new DynamicScriptReferenceOptions(
        [
            ScriptReferencePaths.GetContractReferencePath(pluginDirectory),
        ]);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class BackendStatusHandler : IAsyncRequestHandler<IpcStatusRequest, IpcStatusResponse>
{
    public ValueTask<IpcStatusResponse> InvokeAsync(
        IpcStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<IpcStatusResponse>(new IpcStatusResponse());
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class BackendExecuteScriptHandler(GuanxiangtaiScriptRunner scriptRunner)
    : IAsyncRequestHandler<IpcRunScriptRequest, IpcRunScriptResponse>
{
    public async ValueTask<IpcRunScriptResponse> InvokeAsync(
        IpcRunScriptRequest request,
        CancellationToken cancellationToken = default)
    {
        return await scriptRunner.ExecuteAsync(request, cancellationToken);
    }
}
