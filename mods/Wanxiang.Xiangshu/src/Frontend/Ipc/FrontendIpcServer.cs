using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePack.Resolvers;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Scripting;

namespace Wanxiang.Xiangshu.Frontend.Ipc;

internal sealed class FrontendIpcServer(AgentChatSession chatSession) : IDisposable
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
                Side = IpcRuntime.FrontendSide,
                Transport = IpcRuntime.TransportName,
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
        _ = builder.RegisterInstance(chatSession);
        _ = builder.RegisterInstance(
            new XiangshuScriptRunner(IpcRuntime.FrontendSide));
        _ = builder.RegisterAsyncRequestHandler<
            IpcExecuteScriptRequest,
            IpcExecuteScriptResponse,
            FrontendExecuteScriptHandler>(
            options);
        _ = builder.RegisterAsyncRequestHandler<
            IpcIntermediateReplyRequest,
            IpcIntermediateReplyResponse,
            FrontendIntermediateReplyHandler>(
            options);

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.MessagePackSerializerOptions = StandardResolver.Options;
            });
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcExecuteScriptRequest,
            IpcExecuteScriptResponse>(
            tcpOptions);
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<
            IpcIntermediateReplyRequest,
            IpcIntermediateReplyResponse>(
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
    : IAsyncRequestHandler<IpcExecuteScriptRequest, IpcExecuteScriptResponse>
{
    public async UniTask<IpcExecuteScriptResponse> InvokeAsync(
        IpcExecuteScriptRequest request,
        CancellationToken cancellationToken = default)
    {
        return await scriptRunner.ExecuteAsync(request, cancellationToken);
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "MessagePipe constructs request handlers through DI and reflection.")]
internal sealed class FrontendIntermediateReplyHandler(AgentChatSession chatSession)
    : IAsyncRequestHandler<IpcIntermediateReplyRequest, IpcIntermediateReplyResponse>
{
    public UniTask<IpcIntermediateReplyResponse> InvokeAsync(
        IpcIntermediateReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        chatSession.AddIntermediateReply(request.Content);

        return UniTask.FromResult(new IpcIntermediateReplyResponse());
    }
}
