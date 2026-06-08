using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Xiangshu.Ipc;

namespace Xiangshu.Frontend;

internal sealed class FrontendIpcServer : IDisposable
{
    private const int MaxStartAttempts = 8;

    private IDisposable? _containerScope;
    private XiangshuIpcEndpointRegistration? _registration;
    private bool _disposed;

    public void Start()
    {
        ThrowIfDisposed();

        if (_containerScope is not null)
        {
            return;
        }

        Exception? lastException = null;

        for (int attempt = 0; attempt < MaxStartAttempts; attempt++)
        {
            int port = XiangshuIpcRuntime.ReserveLoopbackPort();

            try
            {
                StartOnPort(port);
                return;
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

        throw new InvalidOperationException("Failed to start Xiangshu frontend IPC server.", lastException);
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
        _containerScope?.Dispose();
        _containerScope = null;
    }

    private void StartOnPort(int port)
    {
        ContainerBuilder builder = new();
        ConfigureContainer(builder, port);
        IObjectResolver container = builder.Build();
        bool started = false;

        try
        {
            _ = container.Resolve<TcpWorker>();
            _registration = XiangshuIpcEndpointRegistry.Register(
                new XiangshuIpcEndpoint
                {
                    Side = XiangshuIpcRuntime.FrontendSide,
                    Transport = XiangshuIpcRuntime.TransportName,
                    Host = XiangshuIpcRuntime.LoopbackHost,
                    Port = port,
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                });
            _containerScope = container;
            started = true;
        }
        finally
        {
            if (!started)
            {
                container.Dispose();
            }
        }
    }

    private static void ConfigureContainer(IContainerBuilder builder, int port)
    {
        MessagePipeOptions options = builder.RegisterMessagePipe(
            options =>
            {
                options.InstanceLifetime = InstanceLifetime.Singleton;
                options.RequestHandlerLifetime = InstanceLifetime.Singleton;
            });
        _ = builder.RegisterAsyncRequestHandler<XiangshuIpcPingRequest, XiangshuIpcPingResponse, FrontendIpcPingHandler>(
            options);

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            XiangshuIpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
            });
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<XiangshuIpcPingRequest, XiangshuIpcPingResponse>(
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
internal sealed class FrontendIpcPingHandler : IAsyncRequestHandler<XiangshuIpcPingRequest, XiangshuIpcPingResponse>
{
    public UniTask<XiangshuIpcPingResponse> InvokeAsync(
        XiangshuIpcPingRequest request,
        CancellationToken cancellationToken = default)
    {
        return UniTask.FromResult(
            new XiangshuIpcPingResponse
            {
                Side = XiangshuIpcRuntime.FrontendSide,
                Message = $"frontend pong: {request.Message}",
            });
    }
}
