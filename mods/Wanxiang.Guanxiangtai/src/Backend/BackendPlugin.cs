using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Guanxiangtai.Ipc;
using Wanxiang.Taiwu.DynamicScripting.Backend;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Backend;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Guanxiangtai.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private const string PluginDirectoryName = "Backend";
    private const string PluginsDirectoryName = "Plugins";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Guanxiangtai");

    private BackendIpcServer? _ipcServer;
    private BackendScriptEntryDispatcher? _scriptEntryDispatcher;

    public override void Initialize()
    {
        TryStartIpcServer();
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
        _scriptEntryDispatcher?.Dispose();
        _scriptEntryDispatcher = null;
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

            _scriptEntryDispatcher ??= new BackendScriptEntryDispatcher();
            _ipcServer = new BackendIpcServer(
                GetPluginDirectory(modDirectory),
                _scriptEntryDispatcher);
            IpcEndpoint endpoint = _ipcServer.Start();
            Log.Info(
                "后端 IPC 已就绪",
                new
                {
                    endpoint.Role,
                    endpoint.Transport,
                    endpoint.Host,
                    endpoint.Port,
                    manifestPath = IpcEndpointRegistry.ManifestPath,
                    modDirectory,
                });
        }
        catch (Exception ex)
        {
            _ipcServer?.Dispose();
            _ipcServer = null;
            Log.Error(ex, "后端 IPC 启动失败");
        }
    }

    private static string GetPluginDirectory(string modDirectory)
    {
        return Path.Combine(
            modDirectory,
            PluginsDirectoryName,
            PluginDirectoryName);
    }
}
