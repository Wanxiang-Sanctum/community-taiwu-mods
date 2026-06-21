using System.Diagnostics.CodeAnalysis;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.ItemGrafts.Backend;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Backend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Xiangshu.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private const string AgentWorkingDirectoryKey = "AgentWorkingDirectory";
    private const string PluginDirectoryName = "Backend";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private BackendIpcServer? _ipcServer;
    private BackendScriptEntryDispatcher? _scriptEntryDispatcher;

    public override void Initialize()
    {
        try
        {
            StartIpcServer();
            BackendInventoryGrafts.Install(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "backend plugin initialization failed");
            throw;
        }
    }

    public override void OnModSettingUpdate()
    {
        Log.Info("backend settings updated; restart the game to apply Wanxiang.Xiangshu runtime settings.");
    }

    public override void Dispose()
    {
        _ = BackendInventoryGrafts.Uninstall();
        _ipcServer?.Dispose();
        _ipcServer = null;
        _scriptEntryDispatcher?.Dispose();
        _scriptEntryDispatcher = null;
    }

    private void StartIpcServer()
    {
        _ipcServer?.Dispose();

        string modDirectory = DomainManager.Mod.GetModDirectory(ModIdStr);
        string workingDirectory = ReadAgentWorkingDirectory(modDirectory);
        _ = Directory.CreateDirectory(workingDirectory);
        _ = Directory.CreateDirectory(XiangshuRuntimePaths.GetRuntimeDirectory(workingDirectory));
        IpcEndpointRegistry.ConfigureForWorkingDirectory(workingDirectory);
        _scriptEntryDispatcher ??= new BackendScriptEntryDispatcher();
        _ipcServer = new BackendIpcServer(
            XiangshuRuntimePaths.GetPluginDirectory(modDirectory, PluginDirectoryName),
            _scriptEntryDispatcher);
        _ = _ipcServer.Start();
        Log.Info("backend IPC ready");
    }

    private string ReadAgentWorkingDirectory(string modDirectory)
    {
        string value = XiangshuRuntimePaths.DefaultAgentWorkingDirectoryName;
        _ = DomainManager.Mod.GetSetting(ModIdStr, AgentWorkingDirectoryKey, ref value);
        return XiangshuRuntimePaths.ResolveAgentWorkingDirectory(modDirectory, value);
    }
}
