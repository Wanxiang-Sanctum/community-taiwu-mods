namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 描述嫁接宿主物品在前端显示时可选的覆盖信息。
/// </summary>
/// <param name="name">名称覆盖；为 null 或空白时沿用宿主名称。</param>
/// <param name="description">描述覆盖；为 null 或空白时沿用宿主描述。</param>
/// <param name="iconName">图标名称覆盖；为 null 或空白时沿用宿主图标。</param>
/// <param name="grade">品级覆盖；为 null 时沿用宿主品级。</param>
public sealed class GraftAppearance(
    string? name = null,
    string? description = null,
    string? iconName = null,
    sbyte? grade = null)
{
    /// <summary>
    /// 获取规范化后的名称覆盖；为 null 时应沿用宿主名称。
    /// </summary>
    public string? Name { get; } = NormalizeOptionalText(name);

    /// <summary>
    /// 获取规范化后的描述覆盖；为 null 时应沿用宿主描述。
    /// </summary>
    public string? Description { get; } = NormalizeOptionalText(description);

    /// <summary>
    /// 获取规范化后的图标名称覆盖；为 null 时应沿用宿主图标。
    /// </summary>
    public string? IconName { get; } = NormalizeOptionalText(iconName);

    /// <summary>
    /// 获取品级覆盖；为 null 时应沿用宿主品级。
    /// </summary>
    public sbyte? Grade { get; } = grade;

    private static string? NormalizeOptionalText(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
