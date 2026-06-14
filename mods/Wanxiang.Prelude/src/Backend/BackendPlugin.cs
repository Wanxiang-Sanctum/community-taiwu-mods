using GameData.Domains;
using MessagePack;
using MessagePipe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.NET.StringTools;
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
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
        typeof(MessagePackObjectAttribute),
        typeof(MessagePackSerializer),
        typeof(IAsyncRequestHandler<,>),
        typeof(MessagePipe.Interprocess.MessagePipeInterprocessOptions),
        typeof(Compilation),
        typeof(CSharpCompilation),
        typeof(IServiceCollection),
        typeof(ServiceProvider),
        typeof(SpanBasedStringBuilder),
    ];

    public static void KeepReferenced()
    {
        _ = RuntimeAssemblyMarkers.Length;
    }
}
