using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePipe;
using Microsoft.NET.StringTools;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine.LowLevel;
using VContainer;
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
            global::ModManager.GetModInfo(ModIdStr).DirectoryName);
        string pluginRootDirectory = Path.Combine(modDirectory, "Plugins");
        return string.IsNullOrEmpty(side)
            ? pluginRootDirectory
            : Path.Combine(pluginRootDirectory, side);
    }
}

internal static class PreludeFrontendAssemblies
{
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
        typeof(CodePagesEncodingProvider),
        typeof(SpanBasedStringBuilder),
        typeof(MessagePackObjectAttribute),
        typeof(MessagePackSerializer),
        typeof(IAsyncRequestHandler<,>),
        typeof(MessagePipe.Interprocess.MessagePipeInterprocessOptions),
        typeof(MessagePipe.ContainerBuilderExtensions),
        typeof(IContainerBuilder),
        typeof(UniTask),
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
