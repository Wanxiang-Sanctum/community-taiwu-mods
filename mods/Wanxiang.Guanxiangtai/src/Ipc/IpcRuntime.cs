using System.Net;
using System.Net.Sockets;

namespace Wanxiang.Guanxiangtai.Ipc;

public static class IpcRuntime
{
    public const string FrontendEndpointRole = "frontend";

    public const string BackendEndpointRole = "backend";

    public const string LoopbackHost = "127.0.0.1";

    public const string TransportName = "messagepipe-tcp";

    public static int ReserveLoopbackPort()
    {
#if NET6_0_OR_GREATER
        using TcpListener socket = new(IPAddress.Loopback, port: 0);

        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
#else
        TcpListener socket = new(IPAddress.Loopback, port: 0);

        try
        {
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }
        finally
        {
            socket.Stop();
        }
#endif
    }
}
