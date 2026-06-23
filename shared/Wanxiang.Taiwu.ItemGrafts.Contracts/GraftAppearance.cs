namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 描述嫁接宿主物品可选的前端外观覆盖值。
/// </summary>
/// <remarks>
/// 字符串覆盖会修剪首尾空白；<see langword="null"/> 或空白表示未提供。<paramref name="visualGrade"/> 为
/// <see langword="null"/> 时也表示未提供；非 <see langword="null"/> 值只透传，不解释取值、范围或显示效果。
/// </remarks>
/// <param name="name">名称覆盖。</param>
/// <param name="description">描述覆盖。</param>
/// <param name="detailDescription">详情描述覆盖。</param>
/// <param name="iconName">图标名称覆盖。</param>
/// <param name="visualGrade">视觉品级覆盖。</param>
public sealed class GraftAppearance(
    string? name = null,
    string? description = null,
    string? detailDescription = null,
    string? iconName = null,
    sbyte? visualGrade = null)
{
    /// <summary>
    /// 获取规范化后的名称覆盖。
    /// </summary>
    public string? Name { get; } = NormalizeOptionalText(name);

    /// <summary>
    /// 获取规范化后的描述覆盖。
    /// </summary>
    public string? Description { get; } = NormalizeOptionalText(description);

    /// <summary>
    /// 获取规范化后的详情描述覆盖。
    /// </summary>
    public string? DetailDescription { get; } = NormalizeOptionalText(detailDescription);

    /// <summary>
    /// 获取规范化后的图标名称覆盖。
    /// </summary>
    public string? IconName { get; } = NormalizeOptionalText(iconName);

    /// <summary>
    /// 获取视觉品级覆盖。
    /// </summary>
    public sbyte? VisualGrade { get; } = visualGrade;

    private static string? NormalizeOptionalText(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
