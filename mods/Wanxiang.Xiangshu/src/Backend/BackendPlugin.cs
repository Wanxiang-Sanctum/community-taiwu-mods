using System.Diagnostics.CodeAnalysis;
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
            _ipcServer = new BackendIpcServer();
            IpcEndpoint endpoint = _ipcServer.Start();
            LogInfo(
                $"backend IPC listening at {IpcRuntime.FormatEndpointAddress(endpoint)}; pid={endpoint.ProcessId}; manifest={IpcEndpointRegistry.GetManifestPath()}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wanxiang.Xiangshu backend plugin initialization failed: {ex}");
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
        Console.WriteLine("Wanxiang.Xiangshu " + message);
    }
}
