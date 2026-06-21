namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 控制嫁接操作如何与宿主物品的原生前端菜单组合。
/// </summary>
public enum GraftMenuMode
{
    /// <summary>
    /// 保留原生菜单项，并追加嫁接操作。
    /// </summary>
    Append = 0,

    /// <summary>
    /// 用嫁接操作替换原生菜单项。
    /// </summary>
    Replace = 1,
}
