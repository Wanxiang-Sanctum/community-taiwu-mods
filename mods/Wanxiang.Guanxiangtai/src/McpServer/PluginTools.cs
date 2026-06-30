using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Wanxiang.Guanxiangtai.McpServer;

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
        Name = "guanxiangtai_status",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    [Description(
        "Reports whether the Wanxiang.Guanxiangtai frontend and backend plugins can answer internal status IPC requests. "
        + "The result describes only those game-side IPC checks; it does not describe MCP server availability or expose internal endpoint addresses.")]
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return PluginStatusProbe.GetStatusJsonAsync(cancellationToken);
    }
}
