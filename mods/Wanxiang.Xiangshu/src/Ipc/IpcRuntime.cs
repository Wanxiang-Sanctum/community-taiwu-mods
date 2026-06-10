using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Wanxiang.Xiangshu.Ipc;

public static class IpcRuntime
{
    public const string FrontendSide = "frontend";

    public const string BackendSide = "backend";

    public const string McpServerSide = "mcp-server";

    public const string LoopbackHost = "127.0.0.1";

    public const string TransportName = "messagepipe-tcp";

    public const string McpTransportName = "mcp-streamable-http";

    public const string McpPath = "/mcp";

    public static string FormatEndpointAddress(IpcEndpoint endpoint)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(endpoint);
#else
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }
#endif

        string port = endpoint.Port.ToString(CultureInfo.InvariantCulture);

        if (string.Equals(endpoint.Transport, McpTransportName, StringComparison.OrdinalIgnoreCase))
        {
            return $"http://{endpoint.Host}:{port}{endpoint.Path}";
        }

        return $"{endpoint.Transport}://{endpoint.Host}:{port}";
    }

    public static int ReserveLoopbackPort()
    {
#if NET10_0_OR_GREATER
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
