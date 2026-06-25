using GameData.Common;
using GameData.DomainEvents;
using GameData.Domains;
using GameData.Domains.Combat;
using GameData.Domains.CombatSkill;
using GameData.Domains.Extra;
using GameData.Domains.SpecialEffect;
using GameData.Domains.SpecialEffect.CombatSkill.Ranshanpai.Sword;
using GameData.Domains.TaiwuEvent.EventHelper;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace FeiJianShuMod
{
    [PluginConfig(pluginName: "FeiJianShuAutoCast", creatorId: "Duma", pluginVersion: "1.0.20.0")]
    public class FeiJianShuPlugin : TaiwuRemakePlugin
    {
        private Harmony? _harmony;
        public static readonly Dictionary<int, FeiJianShuState> CharSkillStates = new();
        private static string _currentBattleId = Guid.NewGuid().ToString();
        private static readonly List<Delegate> _registeredHandlers = new();

        public override void Dispose()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                foreach (var handler in _registeredHandlers)
                {
                    if (handler is Events.OnPrepareSkillBegin prepareHandler)
                        Events.UnRegisterHandler_PrepareSkillBegin(prepareHandler);
                    else if (handler is Events.OnCastSkillEnd castHandler)
                        Events.UnRegisterHandler_CastSkillEnd(castHandler);
                }
                CharSkillStates.Clear();
                _registeredHandlers.Clear();
                _currentBattleId = Guid.NewGuid().ToString();
            }
        }

        public override void Initialize()
        {
            try
            {
                _harmony = new Harmony("FeiJianShuMod.FixLoop");
                PatchMethod(typeof(FeiJianShu), "OnEnable", nameof(FeiJianShuPatches.FeiJianShu_OnEnable_Postfix));
                PatchMethod(typeof(CombatDomain), "StartCombat", nameof(FeiJianShuPatches.CombatDomain_StartCombat_Postfix));
                PatchMethod(typeof(CombatDomain), "EndCombat", nameof(FeiJianShuPatches.CombatDomain_EndCombat_Postfix));
                // 读档完成后重新初始化所有飞剑术状态
                PatchMethod(typeof(SpecialEffectDomain), "OnLoadedAllArchiveData",
                    nameof(FeiJianShuPatches.SpecialEffectDomain_OnLoadedAllArchiveData_Postfix));
            }
            catch { }
        }

        private void PatchMethod(Type targetType, string methodName, string patchMethodName)
        {
            try
            {
                var method = AccessTools.Method(targetType, methodName);
                if (method == null) return;
                _harmony.Patch(method,
                    postfix: new HarmonyMethod(typeof(FeiJianShuPatches), patchMethodName));
            }
            catch { }
        }

        public static string GetCurrentBattleId() => _currentBattleId;
        public static void SetCurrentBattleId(string battleId) => _currentBattleId = battleId;

        public static void RegisterEventHandler(Delegate handler)
        {
            if (handler != null && !_registeredHandlers.Contains(handler))
                _registeredHandlers.Add(handler);
        }
    }

    public class FeiJianShuState
    {
        public bool IsInited { get; set; } = false;
        public bool IsAutoCast { get; set; } = false;
        public int RemainingTriggerCount { get; set; } = 0;
        public short BindSkillId { get; set; } = 0;
        public int LastProficiency { get; set; } = 0;
        public string LastBattleId { get; set; } = Guid.NewGuid().ToString();
    }

    public static class FeiJianShuPatches
    {
        #region 1. 战斗开始：重置状态
        public static void CombatDomain_StartCombat_Postfix()
        {
            try
            {
                string newBattleId = Guid.NewGuid().ToString("N").Substring(0, 8);
                if (newBattleId == FeiJianShuPlugin.GetCurrentBattleId()) return;

                FeiJianShuPlugin.SetCurrentBattleId(newBattleId);
                foreach (var (charId, skillState) in FeiJianShuPlugin.CharSkillStates)
                {
                    skillState.LastProficiency = GetSkillProficiency(charId, skillState.BindSkillId);
                    UpdateTriggerCount(skillState.LastProficiency, skillState);
                    skillState.LastBattleId = newBattleId;
                    skillState.IsAutoCast = false;
                }
            }
            catch { }
        }
        #endregion

        #region 2. 战斗结束：重置自动标记
        public static void CombatDomain_EndCombat_Postfix()
        {
            try
            {
                foreach (var skillState in FeiJianShuPlugin.CharSkillStates.Values)
                    skillState.IsAutoCast = false;
            }
            catch { }
        }
        #endregion

        #region 3. 飞剑术初始化：绑定状态与事件
        public static void FeiJianShu_OnEnable_Postfix(FeiJianShu __instance)
        {
            try
            {
                if (__instance == null) return;
                int charId = __instance.CharacterId;
                short skillId = __instance.SkillTemplateId;

                if (!FeiJianShuPlugin.CharSkillStates.TryGetValue(charId, out var skillState))
                {
                    skillState = new FeiJianShuState
                    {
                        BindSkillId = skillId,
                        LastBattleId = FeiJianShuPlugin.GetCurrentBattleId()
                    };
                    skillState.LastProficiency = GetSkillProficiency(charId, skillId);
                    UpdateTriggerCount(skillState.LastProficiency, skillState);
                    FeiJianShuPlugin.CharSkillStates[charId] = skillState;
                }

                string currentBattleId = FeiJianShuPlugin.GetCurrentBattleId();
                if (skillState.LastBattleId != currentBattleId)
                {
                    skillState.LastProficiency = GetSkillProficiency(charId, skillId);
                    UpdateTriggerCount(skillState.LastProficiency, skillState);
                    skillState.LastBattleId = currentBattleId;
                    skillState.IsAutoCast = false;
                }

                if (!skillState.IsInited)
                {
                    RegisterSkillEvents(__instance, charId);
                    skillState.IsInited = true;
                }
            }
            catch { }
        }
        #endregion

        #region 4. 读档完成后重新初始化所有飞剑术状态
        public static void SpecialEffectDomain_OnLoadedAllArchiveData_Postfix()
        {
            try
            {
                FeiJianShuPlugin.CharSkillStates.Clear();
                foreach (var wrapper in DomainManager.SpecialEffect._effectDict.Values)
                {
                    if (wrapper.Effect is FeiJianShu feiJianShu)
                        FeiJianShu_OnEnable_Postfix(feiJianShu);
                }
            }
            catch { }
        }
        #endregion

        #region 5. 核心工具方法
        private static bool HasRemovableTrick(CombatCharacter character)
        {
            try
            {
                foreach (var trickType in character.GetTricks().Tricks.Values)
                {
                    if (!character.IsTrickUseless(trickType))
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static bool ConsumeOneTrick(DataContext context, CombatCharacter character)
        {
            try
            {
                var trickTypes = new List<sbyte>();
                foreach (var trickType in character.GetTricks().Tricks.Values)
                {
                    if (!character.IsTrickUseless(trickType))
                        trickTypes.Add(trickType);
                }
                if (trickTypes.Count == 0) return false;

                sbyte targetTrickType = trickTypes[context.Random.Next(0, trickTypes.Count)];
                DomainManager.Combat.RemoveTrick(context, character,
                    new List<sbyte> { targetTrickType }, true);
                return true;
            }
            catch { return false; }
        }

        private static int GetSkillProficiency(int charId, short skillId)
        {
            try
            {
                var skillKey = new CombatSkillKey(charId, skillId);
                if (DomainManager.Extra._combatSkillProficiencies.TryGetValue(skillKey, out int prof))
                    return prof;
                return 50;
            }
            catch { return 50; }
        }

        private static void UpdateTriggerCount(int proficiency, FeiJianShuState skillState)
        {
            skillState.RemainingTriggerCount = proficiency switch
            {
                < 300 => 0,
                < 900 => 1,
                < 1800 => 2,
                _ => 3
            };
        }

        private static void RegisterSkillEvents(FeiJianShu feiJianShu, int charId)
        {
            // 准备施展技能前：若为自动连发状态，将蓄力进度直接拉满
            Events.OnPrepareSkillBegin prepareHandler = (context, eventCharId, isAlly, skillId) =>
            {
                try
                {
                    if (eventCharId != charId) return;
                    if (!FeiJianShuPlugin.CharSkillStates.TryGetValue(charId, out var skillState)) return;
                    if (skillId != skillState.BindSkillId || !skillState.IsAutoCast || skillState.RemainingTriggerCount <= 0) return;

                    if (feiJianShu?.CombatChar != null)
                        DomainManager.Combat.ChangeSkillPrepareProgress(
                            feiJianShu.CombatChar, feiJianShu.CombatChar.SkillPrepareTotalProgress);
                }
                catch { }
            };
            Events.RegisterHandler_PrepareSkillBegin(prepareHandler);
            FeiJianShuPlugin.RegisterEventHandler(prepareHandler);

            // 施展技能结束后：判断是否继续连发
            Events.OnCastSkillEnd castHandler = (context, eventCharId, isAlly, skillId, power, interrupted) =>
            {
                try
                {
                    if (eventCharId != charId || interrupted) return;
                    if (!FeiJianShuPlugin.CharSkillStates.TryGetValue(charId, out var skillState)) return;
                    if (skillId != skillState.BindSkillId) return;

                    int currentProf = GetSkillProficiency(charId, skillId);
                    skillState.LastProficiency = currentProf;

                    if (skillState.IsAutoCast)
                    {
                        skillState.RemainingTriggerCount--;
                        if (skillState.RemainingTriggerCount > 0
                            && feiJianShu?.CombatChar != null
                            && DomainManager.Combat.CanCastSkill(feiJianShu.CombatChar, skillId, true, false))
                        {
                            if (!HasRemovableTrick(feiJianShu.CombatChar)
                                || !ConsumeOneTrick(context, feiJianShu.CombatChar))
                            {
                                skillState.IsAutoCast = false;
                                return;
                            }
                            DomainManager.Combat.CastSkillFree(context, feiJianShu.CombatChar,
                                skillId, ECombatCastFreePriority.Normal);
                        }
                        else
                            skillState.IsAutoCast = false;
                        return;
                    }

                    // 首次触发：熟练度 >= 300 且消耗一个式，进入自动连发
                    if (currentProf >= 300)
                    {
                        UpdateTriggerCount(currentProf, skillState);
                        if (feiJianShu?.CombatChar != null
                            && DomainManager.Combat.CanCastSkill(feiJianShu.CombatChar, skillId, true, false)
                            && HasRemovableTrick(feiJianShu.CombatChar)
                            && ConsumeOneTrick(context, feiJianShu.CombatChar))
                        {
                            skillState.IsAutoCast = true;
                            DomainManager.Combat.CastSkillFree(context, feiJianShu.CombatChar,
                                skillId, ECombatCastFreePriority.Normal);
                        }
                    }
                }
                catch { }
            };
            Events.RegisterHandler_CastSkillEnd(castHandler);
            FeiJianShuPlugin.RegisterEventHandler(castHandler);
        }
        #endregion
    }
}
