using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Guanxiangtai.Ipc;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Backend;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Guanxiangtai.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Guanxiangtai");

    private BackendIpcServer? _ipcServer;

    public override void Initialize()
    {
        TryStartIpcServer();
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Backend IPC startup failure should be observable through the MCP status tool instead of aborting game initialization.")]
    private void TryStartIpcServer()
    {
        try
        {
            _ipcServer?.Dispose();

            string modDirectory = Path.GetFullPath(
                DomainManager.Mod.GetModDirectory(ModIdStr));
            IpcEndpointRegistry.ConfigureForModDirectory(modDirectory);

            _ipcServer = new BackendIpcServer();
            _ = _ipcServer.Start();
            Log.Info("backend IPC ready");
        }
        catch (Exception ex)
        {
            _ipcServer?.Dispose();
            _ipcServer = null;
            Log.Error(ex, "backend IPC failed to start");
        }
    }
}
