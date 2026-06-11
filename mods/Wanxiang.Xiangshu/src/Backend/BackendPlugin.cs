using System.Diagnostics.CodeAnalysis;
using GameData.Domains;
using GameData.Utilities;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Backend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Xiangshu.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    private BackendIpcServer? _ipcServer;

    public override void Initialize()
    {
        try
        {
            IpcEndpointRegistry.ConfigureForModDirectory(
                DomainManager.Mod.GetModDirectory(ModIdStr));
            _ipcServer = new BackendIpcServer();
            IpcEndpoint endpoint = _ipcServer.Start();
            LogInfo(
                $"backend IPC listening at {IpcRuntime.FormatEndpointAddress(endpoint)}; pid={endpoint.ProcessId}; manifest={IpcEndpointRegistry.ManifestPath}.");
        }
        catch (Exception ex)
        {
            LogError("backend plugin initialization failed: " + ex);
            throw;
        }
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
    }

    private static void LogInfo(string message)
    {
        AdaptableLog.TagInfo("Wanxiang.Xiangshu", message);
    }

    private static void LogError(string message)
    {
        AdaptableLog.TagError("Wanxiang.Xiangshu", message);
    }
}
