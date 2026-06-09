using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using MessagePipe;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Backend;

internal sealed class BackendIpcServer : IDisposable
{
    private const int MaxStartAttempts = 8;

    private ServiceProvider? _provider;
    private WanxiangXiangshuIpcEndpointRegistration? _registration;
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
            int port = WanxiangXiangshuIpcRuntime.ReserveLoopbackPort();

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
        _provider?.Dispose();
        _provider = null;
    }

    private void StartOnPort(int port)
    {
        ServiceCollection services = new();
        _ = services
            .AddMessagePipe(
                options =>
                {
                    options.EnableAutoRegistration = true;
                    options.SetAutoRegistrationSearchTypes(typeof(BackendIpcPingHandler));
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                    options.RequestHandlerLifetime = InstanceLifetime.Singleton;
                })
            .AddTcpInterprocess(
                WanxiangXiangshuIpcRuntime.LoopbackHost,
                port,
                options =>
                {
                    options.HostAsServer = true;
                    options.InstanceLifetime = InstanceLifetime.Singleton;
                });

        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<TcpWorker>();

        WanxiangXiangshuIpcEndpointRegistration registration = WanxiangXiangshuIpcEndpointRegistry.Register(
            new WanxiangXiangshuIpcEndpoint
            {
                Side = WanxiangXiangshuIpcRuntime.BackendSide,
                Transport = WanxiangXiangshuIpcRuntime.TransportName,
                Host = WanxiangXiangshuIpcRuntime.LoopbackHost,
                Port = port,
                ProcessId = Environment.ProcessId,
                StartedAtUtc = DateTimeOffset.UtcNow,
            });

        _provider = provider;
        _registration = registration;
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
internal sealed class BackendIpcPingHandler : IAsyncRequestHandler<WanxiangXiangshuIpcPingRequest, WanxiangXiangshuIpcPingResponse>
{
    public ValueTask<WanxiangXiangshuIpcPingResponse> InvokeAsync(
        WanxiangXiangshuIpcPingRequest request,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new WanxiangXiangshuIpcPingResponse
            {
                Side = WanxiangXiangshuIpcRuntime.BackendSide,
                Message = $"backend pong: {request.Message}",
            });
    }
}
