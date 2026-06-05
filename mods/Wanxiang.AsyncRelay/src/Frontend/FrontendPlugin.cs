using System.Reflection;
using Cysharp.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine.LowLevel;

namespace Wanxiang.AsyncRelay.Frontend;

[PluginConfig("Wanxiang.AsyncRelay.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    public override void Initialize()
    {
        UniTaskRuntimeAssemblies.KeepReferenced();
        UniTaskEnvironment.Inject();
    }

    public override void Dispose()
    {
    }
}

internal static class UniTaskRuntimeAssemblies
{
    private static readonly Type[] RuntimeAssemblyMarkers =
    [
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
    private const string UniTaskInitMethodName = "Init";

    public static void Inject()
    {
        MethodInfo? initMethod = typeof(PlayerLoopHelper).GetMethod(
            UniTaskInitMethodName,
            BindingFlags.Static | BindingFlags.NonPublic);

        if (initMethod is not null)
        {
            _ = initMethod.Invoke(null, null);
            return;
        }

        PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoopHelper.Initialize(ref playerLoop);
    }
}
