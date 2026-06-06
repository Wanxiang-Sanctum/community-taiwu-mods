using TaiwuModdingLib.Core.Plugin;
using Xiangshu.Mcp;

namespace Xiangshu.Frontend;

[PluginConfig("Xiangshu.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private XiangshuHttpMcpServer? _mcpServer;

    public override void Initialize()
    {
        _mcpServer = FrontendMcpServer.Start();
    }

    public override void Dispose()
    {
        _mcpServer?.Dispose();
        _mcpServer = null;
    }
}
