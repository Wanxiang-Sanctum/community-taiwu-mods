using System.Text;
using Cysharp.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine.LowLevel;
using Wanxiang.Prelude.PluginLoading;

namespace Wanxiang.Prelude.Frontend;

[PluginConfig("Wanxiang.Prelude.Frontend", "WanxiangPrelude", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    public override void Initialize()
    {
        PluginLoadBridge.Apply(
            "Wanxiang.Prelude.Frontend.PluginLoading",
            typeof(FrontendPlugin).Assembly,
            GetPluginDirectory("Frontend"),
            GetPluginDirectory(string.Empty));
        PreludeFrontendAssemblies.KeepReferenced();
        UniTaskEnvironment.EnsureInjected();
    }

    public override void Dispose()
    {
        PluginLoadBridge.Unpatch();
    }

    private string GetPluginDirectory(string side)
    {
        string modDirectory = Path.GetFullPath(
            ModManager.GetModInfo(ModIdStr).DirectoryName);
        string pluginRootDirectory = Path.Combine(modDirectory, "Plugins");
        return string.IsNullOrEmpty(side)
            ? pluginRootDirectory
            : Path.Combine(pluginRootDirectory, side);
    }
}

internal static class PreludeFrontendAssemblies
{
    /// <summary>
    /// Root runtime assemblies whose local AssemblyRef graphs should be preloaded.
    /// </summary>
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
        typeof(CodePagesEncodingProvider),
        typeof(MessagePipe.Interprocess.MessagePipeInterprocessOptions),
        typeof(MessagePipe.ContainerBuilderExtensions),
        typeof(CSharpCompilation),
        typeof(Cysharp.Threading.Tasks.Linq.IAsyncWriter<>),
        typeof(TextMeshProAsyncExtensions),
        typeof(AddressablesAsyncExtensions),
    ];

    public static void KeepReferenced()
    {
        _ = RuntimeAssemblyMarkers.Length;
    }
}

internal static class UniTaskEnvironment
{
    public static void EnsureInjected()
    {
        if (PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
        {
            return;
        }

        PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoopHelper.Initialize(ref playerLoop);
    }
}
