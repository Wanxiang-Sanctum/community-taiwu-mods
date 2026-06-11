using System.Diagnostics.CodeAnalysis;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
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

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private BackendIpcServer? _ipcServer;

    public override void Initialize()
    {
        try
        {
            StartIpcServer();
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
        _ipcServer?.Dispose();
        _ipcServer = null;
    }

    private void StartIpcServer()
    {
        _ipcServer?.Dispose();

        string workingDirectory = ReadAgentWorkingDirectory();
        _ = Directory.CreateDirectory(workingDirectory);
        _ = Directory.CreateDirectory(XiangshuRuntimePaths.GetRuntimeDirectory(workingDirectory));
        IpcEndpointRegistry.ConfigureForWorkingDirectory(workingDirectory);
        _ipcServer = new BackendIpcServer();
        IpcEndpoint endpoint = _ipcServer.Start();
        Log.Info(
            "backend IPC listening",
            new
            {
                endpoint = IpcRuntime.FormatEndpointAddress(endpoint),
                processId = endpoint.ProcessId,
                manifest = IpcEndpointRegistry.ManifestPath,
            });
    }

    private string ReadAgentWorkingDirectory()
    {
        string modDirectory = DomainManager.Mod.GetModDirectory(ModIdStr);
        string value = XiangshuRuntimePaths.DefaultAgentWorkingDirectoryName;
        _ = DomainManager.Mod.GetSetting(ModIdStr, AgentWorkingDirectoryKey, ref value);
        return XiangshuRuntimePaths.ResolveAgentWorkingDirectory(modDirectory, value);
    }
}
