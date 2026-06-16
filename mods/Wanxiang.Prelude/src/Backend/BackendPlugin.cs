using GameData.Domains;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Prelude.PluginLoading;

namespace Wanxiang.Prelude.Backend;

[PluginConfig("Wanxiang.Prelude.Backend", "WanxiangPrelude", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    public override void Initialize()
    {
        PluginLoadBridge.Apply(
            "Wanxiang.Prelude.Backend.PluginLoading",
            typeof(BackendPlugin).Assembly,
            GetPluginDirectory("Backend"),
            GetPluginDirectory(string.Empty));
        PreludeBackendAssemblies.KeepReferenced();
    }

    public override void Dispose()
    {
        PluginLoadBridge.Unpatch();
    }

    private string GetPluginDirectory(string side)
    {
        string modDirectory = Path.GetFullPath(
            DomainManager.Mod.GetModDirectory(ModIdStr));
        string pluginRootDirectory = Path.Combine(modDirectory, "Plugins");
        return string.IsNullOrEmpty(side)
            ? pluginRootDirectory
            : Path.Combine(pluginRootDirectory, side);
    }
}

internal static class PreludeBackendAssemblies
{
    /// <summary>
    /// Root runtime assemblies whose local AssemblyRef graphs should be preloaded.
    /// </summary>
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
        typeof(MessagePipe.Interprocess.MessagePipeInterprocessOptions),
        typeof(CSharpCompilation),
        typeof(ServiceProvider),
    ];

    public static void KeepReferenced()
    {
        _ = RuntimeAssemblyMarkers.Length;
    }
}
