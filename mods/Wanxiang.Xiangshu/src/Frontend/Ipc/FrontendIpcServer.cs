using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Frontend.PlayerView;
using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Taiwu.DynamicScripting.Frontend;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Scripting;

namespace Wanxiang.Xiangshu.Frontend.Ipc;

internal sealed class FrontendIpcServer(
    CurrentChatSessionBinding currentChatSessionBinding,
    DynamicScriptReferenceOptions scriptReferences) : IDisposable
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

        throw new InvalidOperationException("Failed to start Wanxiang.Xiangshu frontend IPC server.", lastException);
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

    private IpcEndpoint StartOnPort(int port)
    {
        ContainerBuilder builder = new();
        ConfigureContainer(builder, port);
        IObjectResolver container = builder.Build();
        bool started = false;

        try
        {
            container.Resolve<TcpWorker>().StartReceiver();
            IpcEndpoint endpoint = new()
            {
                Role = IpcRuntime.FrontendEndpointRole,
                Transport = IpcRuntime.IpcTransportName,
                Host = IpcRuntime.LoopbackHost,
                Port = port,
                ProcessId = Process.GetCurrentProcess().Id,
                StartedAtUtc = DateTimeOffset.UtcNow,
            };
            _registration = IpcEndpointRegistry.Register(
                endpoint);
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

    private void ConfigureContainer(IContainerBuilder builder, int port)
    {
        MessagePipeOptions options = builder.RegisterMessagePipe(
            options =>
            {
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });
        _ = builder.RegisterInstance(currentChatSessionBinding);
        _ = builder.RegisterInstance(
            new XiangshuScriptRunner(
                new ScriptRunnerOptions(
                    IpcRuntime.FrontendEndpointRole,
                    references: scriptReferences),
                new FrontendScriptEntryDispatcher()));
        _ = builder.RegisterAsyncRequestHandler<
            IpcRunScriptRequest,
            IpcRunScriptResponse,
            FrontendExecuteScriptHandler>(
            options);
        _ = builder.RegisterAsyncRequestHandler<
            IpcIntermediateReplyRequest,
            IpcNoContentResponse,
            FrontendIntermediateReplyHandler>(
            options);
        _ = builder.RegisterAsyncRequestHandler<
            IpcCapturePlayerViewRequest,
            IpcCapturePlayerViewResponse,
            FrontendCapturePlayerViewHandler>(
            options);

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = XiangshuMessagePack.Options;
            });
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcRunScriptRequest,
            IpcRunScriptResponse>(
            tcpOptions);
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcIntermediateReplyRequest,
            IpcNoContentResponse>(
            tcpOptions);
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcCapturePlayerViewRequest,
            IpcCapturePlayerViewResponse>(
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
internal sealed class FrontendExecuteScriptHandler(XiangshuScriptRunner scriptRunner)
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
internal sealed class FrontendIntermediateReplyHandler(CurrentChatSessionBinding currentChatSessionBinding)
    : IAsyncRequestHandler<IpcIntermediateReplyRequest, IpcNoContentResponse>
{
    public UniTask<IpcNoContentResponse> InvokeAsync(
        IpcIntermediateReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        currentChatSessionBinding.AddIntermediateReply(request.Content);

        return UniTask.FromResult(new IpcNoContentResponse());
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class FrontendCapturePlayerViewHandler
    : IAsyncRequestHandler<IpcCapturePlayerViewRequest, IpcCapturePlayerViewResponse>
{
    public async UniTask<IpcCapturePlayerViewResponse> InvokeAsync(
        IpcCapturePlayerViewRequest _,
        CancellationToken cancellationToken = default)
    {
        return await PlayerViewScreenshot.CaptureAsync(cancellationToken);
    }
}
