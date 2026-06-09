using System.Net;
using System.Net.Sockets;

namespace Wanxiang.Xiangshu.Ipc;

public static class WanxiangXiangshuIpcRuntime
{
    public const string FrontendSide = "frontend";

    public const string BackendSide = "backend";

    public const string LoopbackHost = "127.0.0.1";

    public const string TransportName = "messagepipe-tcp";

    public static int ReserveLoopbackPort()
    {
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
    }
}
