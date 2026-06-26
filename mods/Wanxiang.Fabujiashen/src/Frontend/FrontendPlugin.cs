using FrameWork;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.PlayerVisibleFeatures;

namespace Wanxiang.Fabujiashen.Frontend;

[PluginConfig("Wanxiang.Fabujiashen.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private const sbyte SpecialFeatureLevel = 0;

    private const sbyte PermanentFeatureDuration = 0;

    private static readonly FeatureStyle FabujiashenFeatureStyle = new(
        ECharacterFeatureType.Special,
        SpecialFeatureLevel,
        PermanentFeatureDuration);

    private FeatureRegistration? _registration;

    private int _registeredTaiwuCharId = -1;

    public override void Initialize()
    {
        VisibleFeatures.Install(this);
        GEvent.Add(EEvents.OnGameStateChange, OnGameStateChange);
        RegisterForCurrentTaiwu();
    }

    public override void Dispose()
    {
        GEvent.Remove(EEvents.OnGameStateChange, OnGameStateChange);
        ClearRegistration();
        _ = VisibleFeatures.Uninstall();
    }

    private void OnGameStateChange(ArgumentBox argBox)
    {
        if (!argBox.Get("newState", out Enum newState))
        {
            return;
        }

        if ((EGameState)(object)newState != EGameState.InGame)
        {
            ClearRegistration();
            return;
        }

        RegisterForCurrentTaiwu();
    }

    private void RegisterForCurrentTaiwu()
    {
        if (!TryGetTaiwuCharId(out int taiwuCharId)
            || (_registration is not null && _registeredTaiwuCharId == taiwuCharId))
        {
            return;
        }

        if (_registration is not null)
        {
            _ = VisibleFeatures.Unregister(_registration);
        }

        _registration = VisibleFeatures.Register(
            taiwuCharId,
            CreateFeatureDefinition());
        _registeredTaiwuCharId = taiwuCharId;
    }

    private void ClearRegistration()
    {
        if (_registration is not null)
        {
            _ = VisibleFeatures.Unregister(_registration);
            _registration = null;
        }

        _registeredTaiwuCharId = -1;
    }

    private static bool TryGetTaiwuCharId(out int taiwuCharId)
    {
        taiwuCharId = -1;

        if (GameApp.Instance is null
            || GameApp.Instance.GetCurrentGameStateName() != EGameState.InGame)
        {
            return false;
        }

        taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
        return taiwuCharId >= 0;
    }

    private static FeatureDefinition CreateFeatureDefinition()
    {
        return new FeatureDefinition(
            "法不加身",
            "此人似与常法隔了一重，诸般异力临身难驻，由身而发亦难着人；唯皮肉筋骨之创，仍循凡胎常理。")
            .WithEffectDescription(
                "内伤、心神、毒素、破绽、封穴与诸般异状，凡加诸其身或借其手施于他人者，多半无从成事；交战之际，凡涉此人的功法异效，亦多归于无用。")
            .WithStyle(FabujiashenFeatureStyle);
    }
}
