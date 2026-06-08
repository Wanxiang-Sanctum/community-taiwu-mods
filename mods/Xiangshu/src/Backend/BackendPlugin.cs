using System.Diagnostics.CodeAnalysis;
using TaiwuModdingLib.Core.Plugin;

namespace Xiangshu.Backend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Xiangshu.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private BackendIpcServer? _ipcServer;

    public override void Initialize()
    {
        _ipcServer = new BackendIpcServer();
        _ipcServer.Start();
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
    }
}
