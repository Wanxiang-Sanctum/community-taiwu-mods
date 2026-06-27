namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

/// <summary>
/// 描述虚拟人物特性提供给原生人物特性 UI 的显示字段。
/// </summary>
/// <param name="featureType">原生人物特性类型。</param>
/// <param name="level">原生人物特性等级。</param>
/// <param name="duration">原生人物特性期限。</param>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="featureType"/> 不是支持的原生人物特性类型。</exception>
public sealed class FeatureStyle(
    ECharacterFeatureType featureType,
    sbyte level,
    sbyte duration)
{
    /// <summary>
    /// 获取默认显示字段。
    /// </summary>
    public static FeatureStyle Default { get; } = new(
        ECharacterFeatureType.Special,
        level: 0,
        duration: 0);

    /// <summary>
    /// 获取原生人物特性类型。
    /// </summary>
    public ECharacterFeatureType FeatureType { get; } = ValidateFeatureType(featureType, nameof(featureType));

    /// <summary>
    /// 获取原生人物特性等级。
    /// </summary>
    public sbyte Level { get; } = level;

    /// <summary>
    /// 获取原生人物特性期限。
    /// </summary>
    public sbyte Duration { get; } = duration;

    internal string GetDisplaySignature()
    {
        return string.Join(
            '\u001f',
            (sbyte)FeatureType,
            Level,
            Duration);
    }

    private static ECharacterFeatureType ValidateFeatureType(
        ECharacterFeatureType featureType,
        string parameterName)
    {
        if (!Enum.IsDefined(typeof(ECharacterFeatureType), featureType)
            || featureType == ECharacterFeatureType.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName, featureType, "Unsupported character feature type.");
        }

        return featureType;
    }
}
