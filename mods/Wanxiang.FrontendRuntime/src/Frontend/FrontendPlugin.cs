using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePipe;
using Microsoft.NET.StringTools;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine.LowLevel;
using VContainer;

namespace Wanxiang.FrontendRuntime.Frontend;

[PluginConfig("Wanxiang.FrontendRuntime.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    public override void Initialize()
    {
        FrontendRuntimeAssemblies.KeepReferenced();
        UniTaskEnvironment.EnsureInjected();
    }

    public override void Dispose()
    {
    }
}

internal static class FrontendRuntimeAssemblies
{
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
        typeof(ImmutableArray<>),
        typeof(MetadataReader),
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
