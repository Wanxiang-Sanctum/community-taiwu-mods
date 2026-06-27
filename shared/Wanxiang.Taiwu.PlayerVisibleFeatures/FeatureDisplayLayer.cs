using System.Diagnostics.CodeAnalysis;
using Config;
using Config.Common;
using FrameWork;
using Game.Components.Character;
using GameData.Domains.Character.Display;
using HarmonyLib;

namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

#pragma warning disable IDE0300 // HarmonyPatch attribute array arguments are clearer in the form Harmony expects.

internal static class FeatureDisplayLayer
{
    private const string HarmonyOwnerSuffix = "Wanxiang.Taiwu.PlayerVisibleFeatures";

    private static readonly object SyncRoot = new();

    private static Harmony? s_harmony;

    internal static bool IsInstalled => s_harmony is not null;

    internal static void Install(string validatedModId)
    {
        lock (SyncRoot)
        {
            if (s_harmony is not null)
            {
                return;
            }

            s_harmony = new Harmony($"{validatedModId}.{HarmonyOwnerSuffix}");
            s_harmony.PatchAll(typeof(FeatureDisplayLayer).Assembly);
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

[HarmonyPatch(typeof(FeatureScroll), nameof(FeatureScroll.Set))]
[HarmonyPatch(
    new[]
    {
        typeof(CharacterDisplayData),
        typeof(bool),
        typeof(bool),
        typeof(Dictionary<short, int>),
    })]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class CharacterDisplayFeatureScrollPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        FeatureScroll __instance,
        CharacterDisplayData displayData)
    {
        if (displayData is null)
        {
            return;
        }

        FeatureDisplayState.AppendVisibleFeatures(
            __instance._showFeatureList,
            displayData,
            displayData.CharacterId);

        __instance.infinityScroll?.SetDataCount(__instance._showFeatureList.Count);
    }
}

[HarmonyPatch(typeof(FeatureScroll), nameof(FeatureScroll.Set))]
[HarmonyPatch(
    new[]
    {
        typeof(List<short>),
        typeof(bool),
        typeof(bool),
        typeof(Dictionary<short, int>),
        typeof(int),
    })]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class ListFeatureScrollPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    [SuppressMessage(
        "Style",
        "IDE0060:Remove unused parameter",
        Justification = "Harmony requires this patch method to keep the original method signature.")]
    [SuppressMessage(
        "Roslynator",
        "RCS1163:Unused parameter",
        Justification = "Harmony requires this patch method to keep the original method signature.")]
    private static void Postfix(
        FeatureScroll __instance,
        List<short> featureIds,
        int characterId)
    {
        FeatureDisplayState.AppendVisibleFeatures(
            __instance._showFeatureList,
            displayData: null,
            characterId);

        __instance.infinityScroll?.SetDataCount(__instance._showFeatureList.Count);
    }
}

/// <summary>
/// 在原生人物特性条目渲染期间打开虚拟显示项读取作用域。
/// </summary>
[HarmonyPatch(typeof(Feature), nameof(Feature.Set))]
[HarmonyPatch(new[] { typeof(short), typeof(int), typeof(bool), typeof(int) })]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class FeatureDisplayItemReadScopePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        short featureId,
        out bool __state)
    {
        __state = FeatureDisplayState.EnterDisplayItemReadScopeIfVirtual(featureId);
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Finalizer(bool __state)
    {
        if (__state)
        {
            FeatureDisplayState.ExitDisplayItemReadScope();
        }
    }
}

/// <summary>
/// 在原生人物特性 tooltip 渲染期间打开虚拟显示项读取作用域。
/// </summary>
[HarmonyPatch(typeof(MouseTipFeature), "Init")]
[HarmonyPatch(new[] { typeof(ArgumentBox) })]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class FeatureTooltipDisplayItemReadScopePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    [SuppressMessage(
        "Style",
        "IDE0051:Remove unused private members",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        ArgumentBox argsBox,
        out bool __state)
    {
        __state = false;
        if (argsBox is null)
        {
            return;
        }

        _ = argsBox.Get("FeatureId", out short featureId);
        __state = FeatureDisplayState.EnterDisplayItemReadScopeIfVirtual(featureId);
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    [SuppressMessage(
        "Style",
        "IDE0051:Remove unused private members",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Finalizer(bool __state)
    {
        if (__state)
        {
            FeatureDisplayState.ExitDisplayItemReadScope();
        }
    }
}

/// <summary>
/// 仅在原生人物特性 UI 渲染作用域内提供虚拟特性显示数据。
/// </summary>
[HarmonyPatch(typeof(ConfigData<CharacterFeatureItem, short>), nameof(ConfigData<,>.GetItem))]
[HarmonyPatch(new[] { typeof(short) })]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class CharacterFeatureConfigDataPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused member declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        ConfigData<CharacterFeatureItem, short> __instance,
        short id,
        ref CharacterFeatureItem __result)
    {
        if (!FeatureDisplayState.IsDisplayItemReadScopeActive
            || !ReferenceEquals(__instance, CharacterFeature.Instance)
            || !FeatureDisplayState.TryGetDisplayItem(id, out CharacterFeatureItem item))
        {
            return true;
        }

        __result = item;
        return false;
    }
}

#pragma warning restore IDE0300
