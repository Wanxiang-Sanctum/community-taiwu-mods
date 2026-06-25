using System.Diagnostics.CodeAnalysis;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.Item;
using GameData.Domains.SpecialEffect;
using GameData.Domains.SpecialEffect.CombatSkill;
using HarmonyLib;
using GameCharacter = GameData.Domains.Character.Character;

#pragma warning disable IDE0051 // Harmony invokes patch methods by reflection.
#pragma warning disable IDE0300 // HarmonyPatch constructor overloads are clearer with explicit arrays.

namespace Wanxiang.Fabujiashen.Backend;

internal static class FabujiashenPatches
{
    private const string HarmonyOwnerSuffix = "Wanxiang.Fabujiashen.Backend";

    private static readonly object SyncRoot = new();

    private static Harmony? s_harmony;

    internal static void Install(string validatedModId)
    {
        lock (SyncRoot)
        {
            if (s_harmony is not null)
            {
                return;
            }

            s_harmony = new Harmony($"{validatedModId}.{HarmonyOwnerSuffix}");
            s_harmony.PatchAll(typeof(FabujiashenPatches).Assembly);
        }
    }

    internal static void Uninstall()
    {
        lock (SyncRoot)
        {
            s_harmony?.UnpatchSelf();
            s_harmony = null;
        }
    }
}

internal static class FabujiashenRules
{
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

    internal static bool IsTaiwuCharacterInCombat(GameCharacter? character)
    {
        return character is not null
            && IsTaiwu(character.GetId())
            && DomainManager.Combat.IsCharInCombat(character.GetId());
    }

    private static bool TryGetTaiwuCharId(out int taiwuCharId)
    {
        taiwuCharId = DomainManager.Taiwu.GetTaiwuCharId();
        return taiwuCharId >= 0
            && DomainManager.Character.TryGetElement_Objects(taiwuCharId, out _);
    }

    internal static void ShapeTaiwuCombatState(CombatCharacter character, CombatDomain combatDomain, DataContext context)
    {
        if (!IsTaiwu(character))
        {
            return;
        }

        ShapeDefeatMarkImmunities(character, combatDomain);
        ShapePoisonResist(character, context);
    }

    internal static void ShapeDefeatMarkImmunities(CombatCharacter character, CombatDomain combatDomain)
    {
        combatDomain.SetDefeatMarkImmunity(
            character.GetId(),
            outerInjuryImmunity: character.GetOuterInjuryImmunity(),
            innerInjuryImmunity: true,
            mindImmunity: true,
            flawImmunity: true,
            acupointImmunity: true);
    }

    internal static void ShapePoisonResist(CombatCharacter character, DataContext context)
    {
        PoisonInts poisonResist = character.GetPoisonResist();
        for (sbyte poisonType = 0; poisonType < PoisonType.Count; poisonType++)
        {
            poisonResist[poisonType] = Math.Max(poisonResist[poisonType], GlobalConfig.MaxPoisonResistance);
        }

        character.SetPoisonResist(ref poisonResist, context);
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

    internal static int[] ClearInnerDamageValues(int[] values)
    {
        return values.Length == 0 ? values : new int[values.Length];
    }

    internal static bool AllowsCombatStateChange(CombatCharacter? target, int power, int srcCharId)
    {
        if (IsTaiwu(srcCharId))
        {
            return false;
        }

        return !IsTaiwu(target) || power <= 0;
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.Init),
    new[] { typeof(CombatDomain), typeof(int), typeof(DataContext) })]
internal static class CombatCharacterInitPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(CombatCharacter __instance, CombatDomain combatDomain, DataContext context)
    {
        FabujiashenRules.ShapeTaiwuCombatState(__instance, combatDomain, context);
    }
}

[HarmonyPatch(
    typeof(GameCharacter),
    nameof(GameCharacter.SetInjuries),
    new[] { typeof(Injuries), typeof(DataContext) })]
internal static class CharacterSetInjuriesPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(GameCharacter __instance, ref Injuries injuries)
    {
        if (FabujiashenRules.IsTaiwuCharacterInCombat(__instance))
        {
            FabujiashenRules.PreventInnerInjuryIncrease(ref injuries, __instance.GetInjuries());
        }
    }
}

[HarmonyPatch(typeof(GameCharacter), nameof(GameCharacter.GetInnerInjuryImmunity))]
internal static class CharacterInnerInjuryImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(GameCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwuCharacterInCombat(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(GameCharacter), nameof(GameCharacter.GetMindImmunity))]
internal static class CharacterMindImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(GameCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwuCharacterInCombat(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetInnerInjuryImmunity))]
internal static class CombatCharacterInnerInjuryImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(CombatCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetMindImmunity))]
internal static class CombatCharacterMindImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(CombatCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(GameCharacter), nameof(GameCharacter.GetFlawImmunity))]
internal static class CharacterFlawImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(GameCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwuCharacterInCombat(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(GameCharacter), nameof(GameCharacter.GetAcupointImmunity))]
internal static class CharacterAcupointImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(GameCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwuCharacterInCombat(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetFlawImmunity))]
internal static class CombatCharacterFlawImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(CombatCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetAcupointImmunity))]
internal static class CombatCharacterAcupointImmunityPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(CombatCharacter __instance, ref bool __result)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.SetInnerDamageValue),
    new[] { typeof(int[]), typeof(DataContext) })]
internal static class CombatCharacterSetInnerDamageValuePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(CombatCharacter __instance, ref int[] innerDamageValue)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            innerDamageValue = FabujiashenRules.ClearInnerDamageValues(innerDamageValue);
        }
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.SetMindDamageValue),
    new[] { typeof(int), typeof(DataContext) })]
internal static class CombatCharacterSetMindDamageValuePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(CombatCharacter __instance, ref int mindDamageValue)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            mindDamageValue = 0;
        }
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.AddInjury),
    new[]
    {
        typeof(DataContext),
        typeof(sbyte),
        typeof(bool),
        typeof(int),
        typeof(bool),
        typeof(bool),
    })]
internal static class CombatCharacterAddInjuryPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        CombatCharacter __instance,
        DataContext context,
        bool isInner)
    {
        if (isInner && FabujiashenRules.IsTaiwu(__instance))
        {
            __instance.ShowImmunityEffectTips(context, EMarkType.Inner);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.AddRandomInjury),
    new[] { typeof(DataContext), typeof(bool), typeof(int), typeof(bool) })]
internal static class CombatCharacterAddRandomInjuryPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        CombatCharacter __instance,
        DataContext context,
        bool inner)
    {
        if (inner && FabujiashenRules.IsTaiwu(__instance))
        {
            __instance.ShowImmunityEffectTips(context, EMarkType.Inner);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.AddMindDamage),
    new[] { typeof(DataContext), typeof(int), typeof(short) })]
internal static class CombatCharacterAddMindDamagePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(CombatCharacter __instance, DataContext context)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __instance.ShowImmunityEffectTips(context, EMarkType.Mind);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(
    typeof(CombatCharacter),
    nameof(CombatCharacter.AddMindMark),
    new[] { typeof(DataContext), typeof(int), typeof(short), typeof(bool) })]
internal static class CombatCharacterAddMindMarkPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(CombatCharacter __instance, DataContext context)
    {
        if (FabujiashenRules.IsTaiwu(__instance))
        {
            __instance.ShowImmunityEffectTips(context, EMarkType.Mind);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(
    typeof(CombatDomain),
    nameof(CombatDomain.AddPoison),
    new[]
    {
        typeof(DataContext),
        typeof(CombatCharacter),
        typeof(CombatCharacter),
        typeof(sbyte),
        typeof(sbyte),
        typeof(int),
        typeof(short),
        typeof(bool),
        typeof(bool),
        typeof(ItemKey),
        typeof(bool),
        typeof(bool),
        typeof(bool),
    })]
internal static class CombatDomainAddPoisonPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        CombatCharacter? attacker,
        CombatCharacter defender)
    {
        return !FabujiashenRules.IsTaiwu(defender)
            && !FabujiashenRules.IsTaiwu(attacker);
    }
}

[HarmonyPatch(
    typeof(CombatDomain),
    nameof(CombatDomain.AddCombatState),
    new[]
    {
        typeof(DataContext),
        typeof(CombatCharacter),
        typeof(sbyte),
        typeof(short),
        typeof(int),
        typeof(bool),
        typeof(bool),
        typeof(int),
    })]
internal static class CombatDomainAddCombatStatePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        CombatCharacter character,
        int power,
        int srcCharId)
    {
        return FabujiashenRules.AllowsCombatStateChange(character, power, srcCharId);
    }
}

[HarmonyPatch(
    typeof(CombatDomain),
    nameof(CombatDomain.AddInjuryDamageValue),
    new[]
    {
        typeof(CombatCharacter),
        typeof(CombatCharacter),
        typeof(sbyte),
        typeof(int),
        typeof(int),
        typeof(short),
        typeof(bool),
        typeof(bool),
    })]
internal static class CombatDomainAddInjuryDamageValuePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        CombatCharacter attacker,
        CombatCharacter defender,
        ref int innerDamage)
    {
        if (innerDamage <= 0)
        {
            return;
        }

        if (FabujiashenRules.IsTaiwu(defender) || FabujiashenRules.IsTaiwu(attacker))
        {
            innerDamage = 0;
        }
    }
}

[HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ApplyMixedInjury))]
internal static class CombatDomainApplyMixedInjuryPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        CombatContext context,
        ref CombatDamageResultMixed result)
    {
        if (FabujiashenRules.IsTaiwu(context.DefenderId))
        {
            if (result.Inner.TotalDamage > 0 || result.Inner.MarkCount > 0)
            {
                context.Defender.ShowImmunityEffectTips(context, EMarkType.Inner);
            }

            result = WithoutInner(result);
            return;
        }

        if (FabujiashenRules.IsTaiwu(context.AttackerId))
        {
            result = WithoutInner(result);
        }
    }

    private static CombatDamageResultMixed WithoutInner(CombatDamageResultMixed result)
    {
        return new CombatDamageResultMixed
        {
            Outer = result.Outer,
            Inner = default,
            CriticalPercent = result.CriticalPercent,
        };
    }
}

[HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ApplyMindInjury))]
internal static class CombatDomainApplyMindInjuryPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(CombatContext context, CombatDamageResult result)
    {
        if (FabujiashenRules.IsTaiwu(context.DefenderId))
        {
            if (result.TotalDamage > 0 || result.MarkCount > 0)
            {
                context.Defender.ShowImmunityEffectTips(context, EMarkType.Mind);
            }

            return false;
        }

        return !FabujiashenRules.IsTaiwu(context.AttackerId);
    }
}

[HarmonyPatch(
    typeof(SpecialEffectDomain),
    nameof(SpecialEffectDomain.Add),
    new[]
    {
        typeof(DataContext),
        typeof(int),
        typeof(short),
        typeof(sbyte),
        typeof(sbyte),
    })]
internal static class SpecialEffectDomainAddCombatSkillEffectPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(int charId)
    {
        return !FabujiashenRules.IsTaiwu(charId);
    }
}

[HarmonyPatch(
    typeof(SpecialEffectDomain),
    nameof(SpecialEffectDomain.CalcCustomModifyEffectList))]
internal static class SpecialEffectDomainCalcCustomModifyEffectListPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        AffectedDataKey dataKey,
        List<SpecialEffectBase> customEffectList)
    {
        if (customEffectList.Count == 0)
        {
            return;
        }

        bool targetIsTaiwu = FabujiashenRules.IsTaiwu(dataKey.CharId);
        _ = customEffectList.RemoveAll(
            effect =>
                effect is CombatSkillEffectBase combatSkillEffect
                && (targetIsTaiwu
                    || FabujiashenRules.IsTaiwu(combatSkillEffect.SkillKey.CharId)
                    || FabujiashenRules.IsTaiwu(combatSkillEffect.CharacterId)));
    }
}
