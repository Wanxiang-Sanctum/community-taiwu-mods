using TaiwuModdingLib.Core.Plugin;
using Xiangshu.Mcp;

namespace Xiangshu.Backend;

[PluginConfig("Xiangshu.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private XiangshuHttpMcpServer? _mcpServer;

    public override void Initialize()
    {
        _mcpServer = BackendMcpServer.Start();
    }

    public override void Dispose()
    {
        _mcpServer?.Dispose();
        _mcpServer = null;
    }
}
