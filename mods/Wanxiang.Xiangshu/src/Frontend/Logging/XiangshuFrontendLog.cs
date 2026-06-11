using GameData.Utilities;

namespace Wanxiang.Xiangshu.Frontend.Logging;

internal static class XiangshuFrontendLog
{
    private const string Tag = "Wanxiang.Xiangshu";

    public static void Info(string message)
    {
        AdaptableLog.TagInfo(Tag, message);
    }

    public static void Warning(string message)
    {
        AdaptableLog.TagWarning(Tag, message);
    }

    public static void Error(string message)
    {
        AdaptableLog.TagError(Tag, message);
    }
}
