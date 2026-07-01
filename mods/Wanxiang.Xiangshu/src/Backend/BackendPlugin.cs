using System.Diagnostics.CodeAnalysis;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.DynamicScripting.Backend;
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
            BackendItemGrafts.Install(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "后端插件初始化失败");
            throw;
        }
    }

    public override void OnModSettingUpdate()
    {
        Log.Info("后端设置已更新；重启游戏后相枢运行设置生效。");
    }

    public override void Dispose()
    {
        _ = BackendItemGrafts.Uninstall();
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
        IpcEndpoint endpoint = _ipcServer.Start();
        Log.Info(
            "后端 IPC 已就绪",
            new
            {
                endpoint.Role,
                endpoint.Transport,
                endpoint.Host,
                endpoint.Port,
                endpoint.ProcessId,
                manifestPath = IpcEndpointRegistry.ManifestPath,
                workingDirectory,
            });
    }

    private string ReadAgentWorkingDirectory(string modDirectory)
    {
        string value = XiangshuRuntimePaths.DefaultAgentWorkingDirectoryName;
        _ = DomainManager.Mod.GetSetting(ModIdStr, AgentWorkingDirectoryKey, ref value);
        return XiangshuRuntimePaths.ResolveAgentWorkingDirectory(modDirectory, value);
    }
}
