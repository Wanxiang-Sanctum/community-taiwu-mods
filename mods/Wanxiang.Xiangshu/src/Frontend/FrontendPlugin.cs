using System.Diagnostics.CodeAnalysis;
using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Xiangshu.Frontend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Xiangshu.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private FrontendIpcServer? _ipcServer;
    private McpSidecarProcess? _mcpServerProcess;

    public override void Initialize()
    {
        _ipcServer = new FrontendIpcServer();
        _ipcServer.Start();

        _mcpServerProcess = new McpSidecarProcess();
        _mcpServerProcess.Start();
    }

    public override void Dispose()
    {
        _mcpServerProcess?.Dispose();
        _mcpServerProcess = null;
        _ipcServer?.Dispose();
        _ipcServer = null;
    }
}
