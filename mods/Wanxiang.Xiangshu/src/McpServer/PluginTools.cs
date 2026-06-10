using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Wanxiang.Xiangshu.McpServer;

[McpServerToolType]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "ModelContextProtocol constructs tool instances through its explicit generic tool registration.")]
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Instance methods keep ModelContextProtocol generic tool registration trim-friendly.")]
internal sealed class PluginTools
{
    [McpServerTool(
        Name = "xiangshu_list_endpoints",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    [Description("Lists live Wanxiang.Xiangshu frontend and backend IPC endpoints discovered from the local manifest.")]
    public string ListEndpoints()
    {
        return PluginIpcProxy.ListEndpoints();
    }

    [McpServerTool(
        Name = "xiangshu_check_toolchain",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    [Description("Checks whether the Wanxiang.Xiangshu MCP server can discover and ping the frontend and backend IPC endpoints.")]
    public Task<string> CheckToolchainAsync(
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.CheckToolchainAsync(cancellationToken);
    }

    [McpServerTool(
        Name = "xiangshu_ping_plugin",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    [Description("Pings the Wanxiang.Xiangshu frontend or backend plugin through its local MessagePipe IPC endpoint.")]
    public Task<string> PingPluginAsync(
        [Description("Plugin side to ping. Valid values: frontend, backend.")]
        string side,
        [Description("Message to include in the ping request.")]
        string message = "hello from MCP",
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.PingAsync(side, message, cancellationToken);
    }
}
