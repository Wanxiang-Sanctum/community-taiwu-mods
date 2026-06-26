using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.Item;
using GameData.Domains.SpecialEffect;
using GameData.Domains.SpecialEffect.CombatSkill;
using GameCharacter = GameData.Domains.Character.Character;

namespace Wanxiang.Fabujiashen.Backend;

internal static class FabujiashenRules
{
    [ThreadStatic]
    private static int s_outerInjuryOverflowFatalScopeDepth;

    [ThreadStatic]
    private static int s_taiwuDirectFatalSourceScopeDepth;

    [ThreadStatic]
    private static int s_taiwuCombatStateSourceScopeDepth;

    internal static bool IsTaiwu(int charId)
    {
        return charId >= 0
            && TryGetTaiwuCharId(out int taiwuCharId)
            && charId == taiwuCharId;
    }

    internal static bool IsTaiwu(CombatCharacter? character)
    {
        return character is not null && IsTaiwu(character.GetId());
    }

    internal static bool IsTaiwu(GameCharacter? character)
    {
        return character is not null && IsTaiwu(character.GetId());
    }

    internal static bool IsTaiwuCombatant(int charId)
    {
        return IsTaiwu(charId)
            && DomainManager.Combat.IsCharInCombat(charId, checkCombatStatus: false);
    }

    internal static bool AllowsCombatSkillEffectRegistration(int charId, sbyte effectActiveType)
    {
        return !IsTaiwuCombatant(charId)
            || !IsCombatRuntimeEffect(effectActiveType);
    }

    internal static bool AllowsCombatSkillEffectModifier(AffectedDataKey dataKey, SpecialEffectBase effect)
    {
        return effect is not CombatSkillEffectBase combatSkillEffect
            || (!IsTaiwuCombatant(dataKey.CharId)
                && !IsTaiwuCombatant(combatSkillEffect.SkillKey.CharId)
                && !IsTaiwuCombatant(combatSkillEffect.CharacterId));
    }

    private static bool IsCombatRuntimeEffect(sbyte effectActiveType)
    {
        return effectActiveType is CombatSkillEffectActiveType.Cast or CombatSkillEffectActiveType.EnterCombat;
    }

    private static bool TryGetTaiwuCharId(out int taiwuCharId)
    {
        taiwuCharId = DomainManager.Taiwu.GetTaiwuCharId();
        return taiwuCharId >= 0
            && DomainManager.Character.TryGetElement_Objects(taiwuCharId, out _);
    }

    internal static void ShapeTaiwuCombatCharacter(CombatCharacter character, CombatDomain combatDomain, DataContext context)
    {
        if (!IsTaiwu(character))
        {
            return;
        }

        ShapeTaiwuDefeatMarkImmunities(character, combatDomain);
        ShapeTaiwuPoisonResist(character, context);
    }

    internal static void ShapeTaiwuDefeatMarkImmunities(CombatCharacter character, CombatDomain combatDomain)
    {
        combatDomain.SetDefeatMarkImmunity(
            character.GetId(),
            outerInjuryImmunity: character.GetOuterInjuryImmunity(),
            innerInjuryImmunity: true,
            mindImmunity: true,
            flawImmunity: true,
            acupointImmunity: true);
    }

    internal static void ShapeTaiwuDefeatMarkImmunityFlags(
        int charId,
        ref bool innerInjuryImmunity,
        ref bool mindImmunity,
        ref bool flawImmunity,
        ref bool acupointImmunity)
    {
        if (!IsTaiwu(charId))
        {
            return;
        }

        innerInjuryImmunity = true;
        mindImmunity = true;
        flawImmunity = true;
        acupointImmunity = true;
    }

    internal static void ShapeTaiwuPoisonResist(CombatCharacter character, DataContext context)
    {
        PoisonInts poisonResist = character.GetPoisonResist();
        RaisePoisonResistToImmunityThreshold(ref poisonResist);
        character.SetPoisonResist(ref poisonResist, context);
    }

    internal static bool NeedsTaiwuPoisonResistShaping(CombatCharacter character)
    {
        if (!IsTaiwu(character))
        {
            return false;
        }

        PoisonInts poisonResist = character.GetPoisonResist();
        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            if (poisonResist[poisonType] < GlobalConfig.MaxPoisonResistance)
            {
                return true;
            }
        }

        return false;
    }

    private static void RaisePoisonResistToImmunityThreshold(ref PoisonInts poisonResist)
    {
        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            poisonResist[poisonType] = Math.Max(poisonResist[poisonType], GlobalConfig.MaxPoisonResistance);
        }
    }

    internal static void PreventInnerInjuryIncrease(ref Injuries injuries, Injuries currentInjuries)
    {
        for (sbyte bodyPart = 0; bodyPart < BodyPartType.Count; bodyPart++)
        {
            sbyte current = currentInjuries.Get(bodyPart, isInnerInjury: true);
            sbyte next = injuries.Get(bodyPart, isInnerInjury: true);
            if (next > current)
            {
                injuries.Change(bodyPart, isInnerInjury: true, (sbyte)(current - next));
            }
        }
    }

    internal static void PreventInnerInjuryDeltaIncrease(ref Injuries delta)
    {
        for (sbyte bodyPart = 0; bodyPart < BodyPartType.Count; bodyPart++)
        {
            sbyte innerDelta = delta.Get(bodyPart, isInnerInjury: true);
            if (innerDelta > 0)
            {
                delta.Change(bodyPart, isInnerInjury: true, -innerDelta);
            }
        }
    }

    internal static bool AllowsInnerInjuryChange(GameCharacter? character, bool isInnerInjury, int delta)
    {
        return !IsTaiwu(character) || !isInnerInjury || delta <= 0;
    }

    internal static bool AllowsInnerInjuryChange(GameCharacter? character, sbyte bodyPartType, bool isInnerInjury, int delta)
    {
        return !IsTaiwu(character)
            || !isInnerInjury
            || delta <= 0
            || bodyPartType is < 0 or >= BodyPartType.Count;
    }

    internal static void ApplyRandomDamageWithInnerInjuryImmunity(GameCharacter character, DataContext context, int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        Injuries injuries = character.GetInjuries();
        Span<sbyte> availableSlots = stackalloc sbyte[BodyPartType.Count * 2];
        for (int i = 0; i < damage; i++)
        {
            int availableCount = 0;
            for (sbyte injurySlot = 0; injurySlot < BodyPartType.Count * 2; injurySlot++)
            {
                sbyte bodyPart = (sbyte)(injurySlot / 2);
                bool isInnerInjury = injurySlot % 2 == 1;
                if (injuries.Get(bodyPart, isInnerInjury) < Injuries.MaxLevel)
                {
                    availableSlots[availableCount] = injurySlot;
                    availableCount++;
                }
            }

            if (availableCount == 0)
            {
                break;
            }

            sbyte selectedSlot = availableSlots[context.Random.Next(availableCount)];
            if (selectedSlot % 2 == 0)
            {
                injuries.Change((sbyte)(selectedSlot / 2), isInnerInjury: false, 1);
            }
        }

        character.SetInjuries(injuries, context);
    }

    internal static int[] ClearInnerDamageValues(int[] values)
    {
        return values.Length == 0 ? values : new int[values.Length];
    }

    internal static bool AllowsCombatStateChange(CombatCharacter? target, int power, int srcCharId)
    {
        if (IsTaiwu(srcCharId) || s_taiwuCombatStateSourceScopeDepth > 0)
        {
            return false;
        }

        return !IsTaiwu(target) || power <= 0;
    }

    internal static bool EnterTaiwuCombatStateSourceScope(SpecialEffectBase effect)
    {
        if (!IsTaiwuCombatant(effect.CharacterId))
        {
            return false;
        }

        s_taiwuCombatStateSourceScopeDepth++;
        return true;
    }

    internal static void ExitTaiwuCombatStateSourceScope(bool active)
    {
        if (active)
        {
            s_taiwuCombatStateSourceScopeDepth--;
        }
    }

    internal static bool AllowsFatalDamage(CombatCharacter? target, int damageValue)
    {
        return damageValue <= 0 || AllowsDirectFatalEntry(target);
    }

    internal static bool AllowsFatalMarkIncrease(CombatCharacter? target, int count)
    {
        return count <= 0 || AllowsDirectFatalEntry(target);
    }

    internal static bool AllowsFatalMarkTransfer(
        CombatCharacter? source,
        CombatCharacter? target,
        int count)
    {
        return count <= 0
            || (!IsTaiwu(source) && AllowsDirectFatalEntry(target));
    }

    private static bool AllowsDirectFatalEntry(CombatCharacter? target)
    {
        return s_outerInjuryOverflowFatalScopeDepth > 0
            || (!IsTaiwu(target) && s_taiwuDirectFatalSourceScopeDepth <= 0);
    }

    internal static bool EnterTaiwuDirectFatalSourceScope(SpecialEffectBase effect)
    {
        if (!IsTaiwuCombatant(effect.CharacterId))
        {
            return false;
        }

        s_taiwuDirectFatalSourceScopeDepth++;
        return true;
    }

    internal static void ExitTaiwuDirectFatalSourceScope(bool active)
    {
        if (active)
        {
            s_taiwuDirectFatalSourceScopeDepth--;
        }
    }

    internal static int AddFatalDamageFromInjuryOverflow(
        CombatCharacter target,
        DataContext context,
        int damageValue,
        int type,
        sbyte bodyPart,
        short skillId,
        EDamageType damageType)
    {
        if (type != 0)
        {
            return target.AddFatalDamage(context, damageValue, type, bodyPart, skillId, damageType);
        }

        s_outerInjuryOverflowFatalScopeDepth++;
        try
        {
            return target.AddFatalDamage(context, damageValue, type, bodyPart, skillId, damageType);
        }
        finally
        {
            s_outerInjuryOverflowFatalScopeDepth--;
        }
    }

    internal static void PreventPoisonIncrease(ref PoisonInts poisoned, PoisonInts currentPoisoned)
    {
        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            if (poisoned[poisonType] > currentPoisoned[poisonType])
            {
                poisoned[poisonType] = currentPoisoned[poisonType];
            }
        }
    }

    internal static void PreventPoisonDeltaIncrease(ref PoisonInts delta)
    {
        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            if (delta[poisonType] > 0)
            {
                delta[poisonType] = 0;
            }
        }
    }

    internal static bool AllowsPoisonChange(GameCharacter? character, sbyte poisonType, int delta)
    {
        return !IsTaiwu(character)
            || delta <= 0
            || poisonType is < 0 or >= PoisonType.Count;
    }

    internal static bool AllowsPoisonChange(GameCharacter? character, ref PoisonsAndLevels delta)
    {
        if (character is null || !IsTaiwu(character))
        {
            return true;
        }

        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            if (delta.GetValue(poisonType) > 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool AllowsSpecifiedPoisonedValue(GameCharacter? character, sbyte poisonType, int value)
    {
        if (character is not { } gameCharacter || !IsTaiwu(gameCharacter))
        {
            return true;
        }

        if (poisonType is < 0)
        {
            return value <= 0;
        }

        if (poisonType is >= PoisonType.Count)
        {
            return true;
        }

        return value <= gameCharacter.GetPoisoned()[poisonType];
    }
}
