using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using MessagePipe;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Scripting;

namespace Wanxiang.Xiangshu.Backend;

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

        Exception? lastException = null;

        for (int attempt = 0; attempt < MaxStartAttempts; attempt++)
        {
            int port = IpcRuntime.ReserveLoopbackPort();

            try
            {
                return StartOnPort(port);
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

        throw new InvalidOperationException("Failed to start Wanxiang.Xiangshu backend IPC server.", lastException);
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

    private IpcEndpoint StartOnPort(int port)
    {
        ServiceCollection services = new();
        IMessagePipeBuilder messagePipeBuilder = services.AddMessagePipe(
            options =>
            {
                options.EnableAutoRegistration = false;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });
        RegisterIpcScriptHandler(
            services,
            pluginDirectory,
            scriptEntryDispatcher);
        _ = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = XiangshuMessagePack.Options;
            });

        ServiceProvider provider = services.BuildServiceProvider();
        bool started = false;

        try
        {
            provider.GetRequiredService<TcpWorker>().StartReceiver();
            IpcEndpoint endpoint = new()
            {
                Role = IpcRuntime.BackendEndpointRole,
                Transport = IpcRuntime.IpcTransportName,
                Host = IpcRuntime.LoopbackHost,
                Port = port,
                ProcessId = Environment.ProcessId,
                StartedAtUtc = DateTimeOffset.UtcNow,
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

    private static void RegisterIpcScriptHandler(
        IServiceCollection services,
        string pluginDirectory,
        IDynamicScriptEntryDispatcher scriptEntryDispatcher)
    {
        _ = services
            .AddSingleton(
                new XiangshuScriptRunner(
                    new ScriptRunnerOptions(
                        IpcRuntime.BackendEndpointRole,
                        referenceDirectories: [pluginDirectory]),
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class BackendExecuteScriptHandler(XiangshuScriptRunner scriptRunner)
    : IAsyncRequestHandler<IpcRunScriptRequest, IpcRunScriptResponse>
{
    public async ValueTask<IpcRunScriptResponse> InvokeAsync(
        IpcRunScriptRequest request,
        CancellationToken cancellationToken = default)
    {
        return await scriptRunner.ExecuteAsync(request, cancellationToken);
    }
}
