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
            "此身不与常人相同，诸般异力皆难停驻；凡不由皮肉筋骨而生之患，及身之时，往往散作虚无。")
            .WithEffectDescription(
                "内伤、心神、破绽、封穴、毒素及战斗状态难以加诸其身；此人出手，也多止于外伤，难以令他人生出诸般妨害。")
            .WithStyle(FabujiashenFeatureStyle);
    }
}
