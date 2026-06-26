using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.SpecialEffect.CombatSkill.Jieqingmen.Sword;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace JieQingCombo
{
    /// <summary>
    /// 界青快剑-连击版：施展界青快剑时概率触发连击，持续消耗杀式。
    /// 初始连击几率受福缘影响，每次连击后几率递减，保底20%。
    /// 连击时释放进度逐次增加，最高99%。
    /// </summary>
    [PluginConfig("界青快剑-连击版", "duma", "2.0.0.4")]
    public class JieQingComboPlugin : TaiwuRemakePlugin
    {
        public static JieQingComboPlugin Instance { get; private set; }

        private Harmony _harmony;

        public override void Initialize()
        {
            Instance = this;
            _harmony = new Harmony("com.yourname.jieqing.combo");

            _harmony.Patch(
                AccessTools.Method(typeof(JieQingKuaiJian), "OnCastSkillEnd"),
                prefix: new HarmonyMethod(GetType(), nameof(PrefixCastSkillEnd))
            );
            _harmony.Patch(
                AccessTools.Method(typeof(JieQingKuaiJian), "OnPrepareSkillBegin"),
                prefix: new HarmonyMethod(GetType(), nameof(PrefixPrepareSkillBegin))
            );
            _harmony.Patch(
                AccessTools.Method(typeof(CombatDomain), "EndCombat"),
                postfix: new HarmonyMethod(GetType(), nameof(PostfixCombatEnd))
            );
        }

        /// <summary>
        /// 施展技能结束时判定连击：根据几率判定是否继续连击，消耗杀式并增加释放进度。
        /// </summary>
        [HarmonyPrefix]
        public static bool PrefixCastSkillEnd(
            JieQingKuaiJian __instance,
            DataContext context,
            int charId,
            bool isAlly,
            short skillId,
            sbyte power,
            bool interrupted,
            ref bool __runOriginal)
        {
            try
            {
                if (skillId != __instance.SkillTemplateId || charId != __instance.CharacterId)
                {
                    if (interrupted)
                    {
                        _comboStates.Remove(charId);
                    }
                    return true;
                }

                if (!_comboStates.TryGetValue(charId, out ComboData comboData))
                {
                    comboData = CreateComboData();
                    _comboStates[charId] = comboData;
                }

                if (!__instance.PowerMatchAffectRequire(power, 0) ||
                    !__instance.PowerMatchAffectRequire(power, 1) ||
                    !DomainManager.Combat.CanCastSkill(__instance.CombatChar, __instance.SkillTemplateId, true, false))
                {
                    return true;
                }

                int odds = Math.Max(comboData!.CurrentOdds, 20);
                int roll = EventHelper.GetRandom(0, 100);

                if (roll < odds)
                {
                    comboData.Count++;
                    comboData.CurrentOdds = Math.Max(comboData.CurrentOdds - 15, 20);
                    comboData.PrepareBonus = comboData.Count <= 5
                        ? Math.Min(50 + comboData.Count * 10, 99)
                        : 99;

                    DomainManager.Combat.CastSkillFree(context, __instance.CombatChar, __instance.SkillTemplateId, ECombatCastFreePriority.Normal);
                    __instance.ShowSpecialEffectTips(1);
                    DomainManager.Combat.AddTrick(
                        context,
                        __instance.IsDirect
                            ? __instance.CombatChar
                            : DomainManager.Combat.GetCombatCharacter(!isAlly, false),
                        19,
                        __instance.IsDirect);

                    __runOriginal = false;
                    return false;
                }

                ResetComboData(comboData);
                __runOriginal = false;
                return false;
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JieQingFix] PrefixCastSkillEnd: " + ex);
                return true;
            }
        }

        /// <summary>
        /// 准备施展技能前：如果处于连击状态，根据连击数提升释放进度。
        /// </summary>
        [HarmonyPrefix]
        public static bool PrefixPrepareSkillBegin(
            JieQingKuaiJian __instance,
            DataContext context,
            int charId,
            bool isAlly,
            short skillId,
            ref bool __runOriginal)
        {
            try
            {
                if (skillId != __instance.SkillTemplateId || charId != __instance.CharacterId)
                {
                    return true;
                }

                if (_comboStates.TryGetValue(charId, out ComboData comboData))
                {
                    int progress = comboData!.Count == 0
                        ? 0
                        : __instance.CombatChar.SkillPrepareTotalProgress * comboData.PrepareBonus / 100;

                    DomainManager.Combat.ChangeSkillPrepareProgress(__instance.CombatChar, progress);
                    __runOriginal = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JieQingFix] PrefixPrepareSkillBegin: " + ex);
            }
            return true;
        }

        /// <summary>
        /// 根据太吾福缘计算初始连击几率：60% + 福缘/2，最高100%。
        /// </summary>
        private static ComboData CreateComboData()
        {
            int taiwuFortune = (int)(EventHelper.GetRolePersonality(DomainManager.Taiwu.GetTaiwu(), 5) / 2);
            return new ComboData
            {
                Count = 0,
                CurrentOdds = Math.Min(60 + taiwuFortune, 100),
                PrepareBonus = 50
            };
        }

        private static void ResetComboData(ComboData data)
        {
            data.Count = 0;
            data.PrepareBonus = 50;
            int taiwuFortune = (int)(EventHelper.GetRolePersonality(DomainManager.Taiwu.GetTaiwu(), 5) / 2);
            data.CurrentOdds = Math.Min(60 + taiwuFortune, 100);
        }

        /// <summary>
        /// 战斗结束时重置所有角色的连击状态。
        /// </summary>
        [HarmonyPostfix]
        public static void PostfixCombatEnd()
        {
            try
            {
                foreach (int charId in _comboStates.Keys)
                {
                    if (_comboStates.TryGetValue(charId, out ComboData data))
                    {
                        ResetComboData(data!);
                    }
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JieQingFix] PostfixCombatEnd: " + ex);
            }
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _comboStates.Clear();
        }

        private static readonly Dictionary<int, ComboData> _comboStates = new();

        private class ComboData
        {
            public int Count;
            public int CurrentOdds;
            public int PrepareBonus;
        }
    }
}
