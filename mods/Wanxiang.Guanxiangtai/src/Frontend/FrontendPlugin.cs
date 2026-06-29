using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Guanxiangtai.McpServerRuntime;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Guanxiangtai.Frontend;

[PluginConfig("Wanxiang.Guanxiangtai.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag(GuanxiangtaiMcp.ModId);

    public override void Initialize()
    {
        McpServerLauncher.EnsureStarted(GetModDirectory(), Log);
    }

    public override void Dispose()
    {
    }

    private static string GetModDirectory()
    {
        return Path.GetFullPath(
            global::ModManager.GetModInfo(GuanxiangtaiMcp.ModId).DirectoryName);
    }
}
