using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Taiwu.DynamicScripting.Frontend;
using Wanxiang.Guanxiangtai.Ipc;
using Wanxiang.Guanxiangtai.Scripting;

namespace Wanxiang.Guanxiangtai.Frontend;

internal sealed class FrontendIpcServer(string pluginDirectory) : IDisposable
{
    private const int MaxStartAttempts = 8;

    private IDisposable? _containerScope;
    private IpcEndpointRegistration? _registration;
    private IpcEndpoint? _endpoint;
    private bool _disposed;

    public IpcEndpoint Start()
    {
        ThrowIfDisposed();

        if (_containerScope is not null)
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

        throw new InvalidOperationException("Failed to start Wanxiang.Guanxiangtai frontend IPC server.", lastException);
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
        _containerScope?.Dispose();
        _containerScope = null;
    }

    private IpcEndpoint StartOnPort(
        int port,
        DynamicScriptReferenceOptions scriptReferenceOptions)
    {
        ContainerBuilder builder = new();
        ConfigureContainer(builder, port, scriptReferenceOptions);
        IObjectResolver container = builder.Build();
        bool started = false;

        try
        {
            container.Resolve<TcpWorker>().StartReceiver();
            IpcEndpoint endpoint = new()
            {
                Role = IpcRuntime.FrontendEndpointRole,
                Transport = IpcRuntime.TransportName,
                Host = IpcRuntime.LoopbackHost,
                Port = port,
                StartedAt = DateTimeOffset.UtcNow,
            };
            _registration = IpcEndpointRegistry.Register(endpoint);
            _endpoint = endpoint;
            _containerScope = container;
            started = true;
            return endpoint;
        }
        finally
        {
            if (!started)
            {
                container.Dispose();
            }
        }
    }

    private static DynamicScriptReferenceOptions CreateScriptReferenceOptions(string pluginDirectory)
    {
        return ScriptReferences.Create(pluginDirectory);
    }

    private static void ConfigureContainer(
        IContainerBuilder builder,
        int port,
        DynamicScriptReferenceOptions scriptReferenceOptions)
    {
        MessagePipeOptions options = builder.RegisterMessagePipe(
            options =>
            {
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });
        _ = builder.RegisterInstance(
            new GuanxiangtaiScriptRunner(
                new ScriptRunnerOptions(
                    IpcRuntime.FrontendEndpointRole,
                    references: scriptReferenceOptions),
                new FrontendScriptEntryDispatcher()));
        _ = builder.RegisterAsyncRequestHandler<
            IpcRunScriptRequest,
            IpcRunScriptResponse,
            FrontendExecuteScriptHandler>(
            options);
        _ = builder.RegisterAsyncRequestHandler<
            IpcStatusRequest,
            IpcStatusResponse,
            FrontendStatusHandler>(
            options);

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = IpcMessagePack.Options;
            });
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcRunScriptRequest,
            IpcRunScriptResponse>(
            tcpOptions);
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcStatusRequest,
            IpcStatusResponse>(
            tcpOptions);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FrontendIpcServer));
        }
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class FrontendExecuteScriptHandler(GuanxiangtaiScriptRunner scriptRunner)
    : IAsyncRequestHandler<IpcRunScriptRequest, IpcRunScriptResponse>
{
    public async UniTask<IpcRunScriptResponse> InvokeAsync(
        IpcRunScriptRequest request,
        CancellationToken cancellationToken = default)
    {
        return await scriptRunner.ExecuteAsync(request, cancellationToken);
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class FrontendStatusHandler : IAsyncRequestHandler<IpcStatusRequest, IpcStatusResponse>
{
    public UniTask<IpcStatusResponse> InvokeAsync(
        IpcStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return UniTask.FromResult(new IpcStatusResponse());
    }
}
