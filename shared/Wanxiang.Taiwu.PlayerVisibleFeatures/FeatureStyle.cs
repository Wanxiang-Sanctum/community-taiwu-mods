using Config.ConfigCells.Character;

namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

/// <summary>
/// 描述虚拟人物特性提供给原生人物特性 UI 的显示字段。
/// </summary>
/// <param name="featureType">原生人物特性类型。</param>
/// <param name="level">原生人物特性等级。</param>
/// <param name="duration">原生人物特性期限。</param>
/// <param name="featureMedals">原生人物特性三组奖章布局。</param>
/// <exception cref="ArgumentException"><paramref name="featureMedals"/> 不是三组布局。</exception>
/// <exception cref="ArgumentNullException"><paramref name="featureMedals"/> 或其中任一组为 null。</exception>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="featureType"/> 不是支持的原生人物特性类型。</exception>
public sealed class FeatureStyle(
    ECharacterFeatureType featureType,
    sbyte level,
    sbyte duration,
    IReadOnlyList<FeatureMedals> featureMedals)
{
    private const int FeatureMedalGroupCount = 3;

    private readonly FeatureMedals[] _featureMedals =
        CopyFeatureMedals(featureMedals ?? throw new ArgumentNullException(nameof(featureMedals)));

    /// <summary>
    /// 获取默认显示字段。
    /// </summary>
    public static FeatureStyle Default { get; } = new(
        ECharacterFeatureType.Special,
        level: 0,
        duration: 0,
        EmptyFeatureMedals());

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

    /// <summary>
    /// 获取原生人物特性三组奖章布局。
    /// </summary>
    public IReadOnlyList<FeatureMedals> FeatureMedals => CopyFeatureMedals(_featureMedals);

    internal FeatureMedals[] ToFeatureMedals()
    {
        return CopyFeatureMedals(_featureMedals);
    }

    internal string GetDisplaySignature()
    {
        return string.Join(
            '\u001f',
            (sbyte)FeatureType,
            Level,
            Duration,
            GetFeatureMedalsSignature(_featureMedals));
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

    private static FeatureMedals[] CopyFeatureMedals(IReadOnlyList<FeatureMedals> featureMedals)
    {
        if (featureMedals.Count != FeatureMedalGroupCount)
        {
            throw new ArgumentException(
                "Character feature medals must contain exactly three groups.",
                nameof(featureMedals));
        }

        FeatureMedals[] copied = new FeatureMedals[featureMedals.Count];
        for (int i = 0; i < featureMedals.Count; i++)
        {
            copied[i] = CopyFeatureMedal(featureMedals[i], nameof(featureMedals));
        }

        return copied;
    }

    private static FeatureMedals CopyFeatureMedal(
        FeatureMedals featureMedal,
        string parameterName)
    {
        if (featureMedal is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        FeatureMedals copied = new();
        copied.Values.AddRange(featureMedal.Values);

        return copied;
    }

    private static FeatureMedals[] EmptyFeatureMedals()
    {
        return
        [
            new FeatureMedals([]),
            new FeatureMedals([]),
            new FeatureMedals([]),
        ];
    }

    private static string GetFeatureMedalsSignature(FeatureMedals[] featureMedals)
    {
        string[] groups = new string[featureMedals.Length];
        for (int i = 0; i < featureMedals.Length; i++)
        {
            groups[i] = string.Join(",", featureMedals[i].Values);
        }

        return string.Join("|", groups);
    }
}
