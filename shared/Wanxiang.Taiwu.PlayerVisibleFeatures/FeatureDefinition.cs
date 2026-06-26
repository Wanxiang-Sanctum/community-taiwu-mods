namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

/// <summary>
/// 定义一项只在前端人物特性列表中显示的虚拟人物特性。
/// </summary>
/// <param name="name">玩家可见特性名。</param>
/// <param name="description">玩家可见特性描述。</param>
public sealed class FeatureDefinition(
    string name,
    string description)
{
    /// <summary>
    /// 玩家可见特性名。
    /// </summary>
    public string Name { get; } = ValidateText(name, nameof(name));

    /// <summary>
    /// 玩家可见特性描述。
    /// </summary>
    public string Description { get; } = description?.Trim() ?? string.Empty;

    /// <summary>
    /// 玩家可见特性效果描述；为空时不显示效果段。
    /// </summary>
    public string EffectDescription { get; private set; } = string.Empty;

    /// <summary>
    /// 获取虚拟特性的原生人物特性视觉样式。
    /// </summary>
    public FeatureStyle Style { get; private set; } = FeatureStyle.Default;

    /// <summary>
    /// 创建只修改效果描述的新定义。
    /// </summary>
    /// <param name="effectDescription">玩家可见特性效果描述；为空时不显示效果段。</param>
    /// <returns>效果描述已更新的新定义。</returns>
    public FeatureDefinition WithEffectDescription(string effectDescription)
    {
        return new FeatureDefinition(Name, Description)
        {
            EffectDescription = effectDescription?.Trim() ?? string.Empty,
            Style = Style,
        };
    }

    /// <summary>
    /// 创建只修改视觉样式的新定义。
    /// </summary>
    /// <param name="style">原生人物特性视觉样式。</param>
    /// <returns>视觉样式已更新的新定义。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="style"/> 为 null。</exception>
    public FeatureDefinition WithStyle(FeatureStyle style)
    {
        if (style is null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        return new FeatureDefinition(Name, Description)
        {
            EffectDescription = EffectDescription,
            Style = style,
        };
    }

    internal string GetDisplaySignature()
    {
        return string.Join(
            '\u001f',
            Name,
            Description,
            EffectDescription,
            Style.GetDisplaySignature());
    }

    private static string ValidateText(string value, string paramName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

        return normalized;
    }
}
