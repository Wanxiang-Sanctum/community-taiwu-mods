using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Taiwu.DynamicScripting.Frontend;
using Wanxiang.Guanxiangtai.Ipc;
using Wanxiang.Guanxiangtai.McpServerRuntime;
using Wanxiang.Guanxiangtai.Scripting;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Frontend;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Guanxiangtai.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private const string PluginDirectoryName = "Frontend";
    private const string PluginsDirectoryName = "Plugins";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag(GuanxiangtaiMcp.ModId);

    private FrontendIpcServer? _ipcServer;

    public override void Initialize()
    {
        string modDirectory = GetModDirectory();
        TryStartIpcServer(modDirectory);
        McpServerLauncher.EnsureStarted(modDirectory, Log);
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
    }

    private static string GetModDirectory()
    {
        return Path.GetFullPath(
            global::ModManager.GetModInfo(GuanxiangtaiMcp.ModId).DirectoryName);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Frontend IPC startup failure should leave the MCP server able to report this side as unavailable.")]
    private void TryStartIpcServer(string modDirectory)
    {
        try
        {
            _ipcServer?.Dispose();

            IpcEndpointRegistry.ConfigureForModDirectory(modDirectory);

            string pluginDirectory = GetPluginDirectory(modDirectory);
            _ipcServer = new FrontendIpcServer(
                CreateScriptReferences(pluginDirectory));
            _ = _ipcServer.Start();
            Log.Info("frontend IPC ready");
        }
        catch (Exception ex)
        {
            _ipcServer?.Dispose();
            _ipcServer = null;
            Log.Error(ex, "frontend IPC failed to start");
        }
    }

    private static string GetPluginDirectory(string modDirectory)
    {
        return Path.Combine(
            modDirectory,
            PluginsDirectoryName,
            PluginDirectoryName);
    }

    private static DynamicScriptReferenceOptions CreateScriptReferences(string pluginDirectory)
    {
        return new DynamicScriptReferenceOptions(
        [
            DynamicScriptAssemblyReferenceResolver.ResolveRequiredAssemblyReferencePath(
                typeof(GuanxiangtaiScriptGlobals),
                [pluginDirectory]),
            .. FrontendScriptReferencePaths.GetAssemblyReferencePaths(
                pluginDirectory,
                FrontendScriptReferenceFeatures.UniTask),
        ]);
    }
}
