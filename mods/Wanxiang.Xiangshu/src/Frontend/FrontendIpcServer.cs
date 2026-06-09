using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using VContainer;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend;

internal sealed class FrontendIpcServer : IDisposable
{
    private const int MaxStartAttempts = 8;

    private IDisposable? _containerScope;
    private IpcEndpointRegistration? _registration;
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
            int port = IpcRuntime.ReserveLoopbackPort();

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
            _registration = IpcEndpointRegistry.Register(
                new IpcEndpoint
                {
                    Side = IpcRuntime.FrontendSide,
                    Transport = IpcRuntime.TransportName,
                    Host = IpcRuntime.LoopbackHost,
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
        _ = builder.RegisterAsyncRequestHandler<IpcPingRequest, IpcPingResponse, FrontendIpcPingHandler>(
            options);

        IMessagePipeBuilder messagePipeBuilder = builder.ToMessagePipeBuilder();
        MessagePipeInterprocessOptions tcpOptions = messagePipeBuilder.AddTcpInterprocess(
            IpcRuntime.LoopbackHost,
            port,
            options =>
            {
                options.HostAsServer = true;
                options.InstanceLifetime = InstanceLifetime.Singleton;
            });
        _ = messagePipeBuilder.RegisterTcpRemoteRequestHandler<IpcPingRequest, IpcPingResponse>(
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
internal sealed class FrontendIpcPingHandler : IAsyncRequestHandler<IpcPingRequest, IpcPingResponse>
{
    public UniTask<IpcPingResponse> InvokeAsync(
        IpcPingRequest request,
        CancellationToken cancellationToken = default)
    {
        return UniTask.FromResult(
            new IpcPingResponse
            {
                Side = IpcRuntime.FrontendSide,
                Message = $"frontend pong: {request.Message}",
            });
    }
}
