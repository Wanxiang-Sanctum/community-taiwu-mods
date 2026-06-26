using FrameWork;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.PlayerVisibleFeatures;

namespace Wanxiang.Fabujiashen.Frontend;

[PluginConfig("Wanxiang.Fabujiashen.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
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
            "法不近其身，妄伤难落。此为法不加身 Mod 的前端标记，不是存档中的真实人物特性。")
            .WithEffectDescription(
                "法不加身规则正在作用于太吾：内伤、心神、破绽、封穴、毒素和相关战斗状态会按 Mod 边界被拦截或塑形。");
    }
}
