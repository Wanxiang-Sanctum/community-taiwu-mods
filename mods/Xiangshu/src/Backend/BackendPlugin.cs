using TaiwuModdingLib.Core.Plugin;
using Xiangshu.BackendAgentTools;

namespace Xiangshu.Backend;

[PluginConfig("Xiangshu.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private XiangshuBackendAgentToolServer? _mcpServer;

    public override void Initialize()
    {
        _mcpServer = XiangshuBackendAgentToolServer.Start();
    }

    public override void Dispose()
    {
        _mcpServer?.Dispose();
        _mcpServer = null;
    }
}
