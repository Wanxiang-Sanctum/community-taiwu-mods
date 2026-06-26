using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Fabujiashen.Backend;

[PluginConfig("Wanxiang.Fabujiashen.Backend", "WanxiangSanctum", "0.1.0")]
public sealed class BackendPlugin : TaiwuRemakePlugin
{
    public override void Initialize()
    {
        FabujiashenPatches.Install(ModIdStr);
    }

    public override void Dispose()
    {
        FabujiashenPatches.Uninstall();
    }
}
