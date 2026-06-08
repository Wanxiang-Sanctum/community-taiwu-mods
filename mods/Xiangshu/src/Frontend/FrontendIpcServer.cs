using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using Xiangshu.Ipc;

namespace Xiangshu.Frontend;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "VContainer constructs this server through DI.")]
internal sealed class FrontendIpcServer : IDisposable
{
    private const int MaxStartAttempts = 8;

    private IServiceProvider? _provider;
    private IDisposable? _worker;
    private XiangshuIpcEndpointRegistration? _registration;
    private bool _disposed;

    public void Start()
    {
        ThrowIfDisposed();

        if (_provider is not null)
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
        _worker?.Dispose();
        _worker = null;
        _provider = null;
    }

    private void StartOnPort(int port)
    {
        BuiltinContainerBuilder builder = new();
        _ = builder
            .AddMessagePipe()
            .AddAsyncRequestHandler<XiangshuIpcPingRequest, XiangshuIpcPingResponse, FrontendIpcPingHandler>();

        IMessagePipeBuilder messagePipeBuilder = new MessagePipeBuilder(builder);
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

        IServiceProvider provider = builder.BuildServiceProvider();
        IDisposable worker = (IDisposable)(provider.GetService(typeof(TcpWorker))
            ?? throw new InvalidOperationException("MessagePipe TCP worker was not registered."));

        XiangshuIpcEndpointRegistration registration = XiangshuIpcEndpointRegistry.Register(
            new XiangshuIpcEndpoint
            {
                Side = XiangshuIpcRuntime.FrontendSide,
                Transport = XiangshuIpcRuntime.TransportName,
                Host = XiangshuIpcRuntime.LoopbackHost,
                Port = port,
                ProcessId = Process.GetCurrentProcess().Id,
                StartedAtUtc = DateTimeOffset.UtcNow,
            });

        _provider = provider;
        _worker = worker;
        _registration = registration;
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
