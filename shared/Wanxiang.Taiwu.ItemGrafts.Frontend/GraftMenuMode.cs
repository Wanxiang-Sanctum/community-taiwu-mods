namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 表达嫁接定义交给菜单适配的操作组合策略。
/// </summary>
public enum GraftMenuMode
{
    /// <summary>
    /// 保留原生菜单项，并追加嫁接操作；由使用方自有菜单代码解释。
    /// </summary>
    Append = 0,

    /// <summary>
    /// 用嫁接操作替换原生菜单项；共享可视化层在太吾行囊入口实现此模式。
    /// </summary>
    Replace = 1,
}
