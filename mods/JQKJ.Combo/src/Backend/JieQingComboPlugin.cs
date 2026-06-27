using GameData.Common;
using GameData.Domains;
using GameData.Domains.Combat;
using GameData.Domains.SpecialEffect.CombatSkill.Jieqingmen.Sword;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace JieQingCombo
{
    [PluginConfig("界青快剑-连击版", "duma", "2.0.0.4")]
    public class JieQingComboPlugin : TaiwuRemakePlugin
    {
        public override void Initialize()
        {
            _harmony = new Harmony("JieQingCombo.Plugin");

            var methodOnCastEnd = AccessTools.Method(typeof(JieQingKuaiJian), "OnCastSkillEnd");
            _harmony.Patch(
                methodOnCastEnd,
                prefix: new HarmonyMethod(GetType(), nameof(PrefixCastSkillEnd))
            );

            var methodOnPrepare = AccessTools.Method(typeof(JieQingKuaiJian), "OnPrepareSkillBegin");
            _harmony.Patch(
                methodOnPrepare,
                prefix: new HarmonyMethod(GetType(), nameof(PrefixPrepareSkillBegin))
            );

            var methodEndCombat = AccessTools.Method(typeof(CombatDomain), "EndCombat");
            _harmony.Patch(
                methodEndCombat,
                postfix: new HarmonyMethod(GetType(), nameof(PostfixEndCombat))
            );
        }

        [HarmonyPrefix]
        public static bool PrefixCastSkillEnd(
            JieQingKuaiJian __instance,
            DataContext context,
            int charId,
            bool isAlly,
            short skillId,
            sbyte power,
            bool interrupted)
        {
            try
            {
                if (interrupted)
                {
                    _comboStates.Remove(charId);
                    return false;
                }

                bool idMatch = (skillId == __instance.SkillTemplateId && charId == __instance.CharacterId);
                if (!idMatch)
                {
                    return false;
                }

                if (!_comboStates.TryGetValue(charId, out ComboData comboData))
                {
                    comboData = CreateComboData(__instance, context);
                    _comboStates[charId] = comboData;
                }

                bool power0 = __instance.PowerMatchAffectRequire(power, 0);
                bool power1 = __instance.PowerMatchAffectRequire(power, 1);

                if (!power0 && !power1)
                {
                    ResetComboData(comboData);
                    return false;
                }

                // 正逆练杀式获取：只在首次施展时发放，连击递归不重复发放
                var enemyChar = DomainManager.Combat.GetCombatCharacter(!isAlly, false);
                if (comboData.Count == 0)
                {
                    if (power0)
                    {
                        DomainManager.Combat.AddTrick(context, __instance.CombatChar, 19, true);
                    }
                    if (power1)
                    {
                        DomainManager.Combat.AddTrick(context, enemyChar, 19, false);
                    }
                }

                // 连击几率判定（上限100%，溢出部分保留用于后续递减）
                int odds = Math.Min(Math.Max(comboData.CurrentOdds, 20), 100);
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

                    // 连击成功补 1 个杀式，维持连击链
                    DomainManager.Combat.AddTrick(
                        context,
                        __instance.IsDirect
                            ? __instance.CombatChar
                            : DomainManager.Combat.GetCombatCharacter(!isAlly, false),
                        19,
                        __instance.IsDirect);
                }
                else
                {
                    ResetComboData(comboData);
                }

                return false;
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JQKJ] PrefixCastSkillEnd 异常: " + ex);
                return false;
            }
        }

        [HarmonyPrefix]
        public static bool PrefixPrepareSkillBegin(
            JieQingKuaiJian __instance,
            DataContext context,
            int charId,
            bool isAlly,
            short skillId)
        {
            try
            {
                bool idMatch = (skillId == __instance.SkillTemplateId && charId == __instance.CharacterId);
                if (!idMatch)
                {
                    return true;
                }

                if (_comboStates.TryGetValue(charId, out ComboData comboData) && comboData.Count > 0)
                {
                    int progress = __instance.CombatChar.SkillPrepareTotalProgress * comboData.PrepareBonus / 100;
                    DomainManager.Combat.ChangeSkillPrepareProgress(__instance.CombatChar, progress);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JQKJ] PrefixPrepareSkillBegin 异常: " + ex);
                return true;
            }
        }

        [HarmonyPostfix]
        public static void PostfixEndCombat()
        {
            try
            {
                foreach (ComboData data in _comboStates.Values)
                {
                    data.Count = 0;
                    data.PrepareBonus = 50;
                }
                int taiwuFortune = (int)(EventHelper.GetRolePersonality(DomainManager.Taiwu.GetTaiwu(), 5) / 2);
                foreach (ComboData data in _comboStates.Values)
                {
                    data.CurrentOdds = 60 + taiwuFortune;
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error("[JQKJ] PostfixEndCombat 异常: " + ex);
            }
        }

        /// <summary>
        /// 根据太吾福缘创建初始连击数据：初始几率 = 60% + 福缘/2。
        /// 不截断上限，溢出值留给连击递减消耗。
        /// </summary>
        private static ComboData CreateComboData(JieQingKuaiJian kuaiJian, DataContext context)
        {
            int taiwuFortune = (int)(EventHelper.GetRolePersonality(DomainManager.Taiwu.GetTaiwu(), 5) / 2);
            int odds = 60 + taiwuFortune;
            return new ComboData
            {
                Count = 0,
                CurrentOdds = odds,
                PrepareBonus = 50,
            };
        }

        private static void ResetComboData(ComboData data)
        {
            data.Count = 0;
            data.PrepareBonus = 50;
            int taiwuFortune = (int)(EventHelper.GetRolePersonality(DomainManager.Taiwu.GetTaiwu(), 5) / 2);
            data.CurrentOdds = 60 + taiwuFortune;
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _comboStates.Clear();
        }

        private Harmony _harmony;
        private static readonly Dictionary<int, ComboData> _comboStates = new();

        private class ComboData
        {
            public int Count;
            public int CurrentOdds;
            public int PrepareBonus;
        }
    }
}
