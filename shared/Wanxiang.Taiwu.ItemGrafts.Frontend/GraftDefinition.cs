using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 描述创建嫁接时使用的前端外观、菜单策略和操作。
/// </summary>
/// <param name="appearance">应用到宿主物品上的显示覆盖信息。</param>
/// <param name="menuMode">菜单组合模式。</param>
/// <param name="operations">嫁接暴露的前端操作。</param>
public sealed class GraftDefinition(
    GraftAppearance appearance,
    GraftMenuMode menuMode,
    IReadOnlyList<GraftOperation> operations)
{
    /// <summary>
    /// 获取应用到宿主物品上的显示覆盖信息。
    /// </summary>
    public GraftAppearance Appearance { get; } =
        appearance ?? throw new ArgumentNullException(nameof(appearance));

    /// <summary>
    /// 获取菜单组合模式。
    /// </summary>
    public GraftMenuMode MenuMode { get; } = Graft.ValidateMenuMode(menuMode, nameof(menuMode));

    /// <summary>
    /// 获取嫁接暴露的前端操作。
    /// </summary>
    public IReadOnlyList<GraftOperation> Operations { get; } = Graft.CopyOperations(operations);
}
