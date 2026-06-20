using System.Diagnostics.CodeAnalysis;
using GameData.Common;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using HarmonyLib;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Ipc.ItemGrafts;

namespace Wanxiang.Xiangshu.Backend.ItemGrafts;

internal static class ItemGraftPatches
{
    private const string HarmonyId = "Wanxiang.Xiangshu.Backend.ItemGrafts";

    [ThreadStatic]
    private static int s_characterInventoryTransferDepth;

    private static Harmony? s_harmony;

    public static void Install()
    {
        if (s_harmony is not null)
        {
            return;
        }

        s_harmony = new Harmony(HarmonyId);
        s_harmony.PatchAll(typeof(ItemGraftPatches).Assembly);
    }

    public static void Uninstall()
    {
        s_harmony?.UnpatchSelf();
        s_harmony = null;
        HostRegistration.Unregister();
    }

    public static bool IsCharacterInventoryTransferInProgress => s_characterInventoryTransferDepth > 0;

    public static void EnterCharacterInventoryTransfer()
    {
        s_characterInventoryTransferDepth++;
    }

    public static void ExitCharacterInventoryTransfer()
    {
        if (s_characterInventoryTransferDepth > 0)
        {
            s_characterInventoryTransferDepth--;
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemDomain),
    nameof(ItemDomain.RemoveItem),
    new[] { typeof(DataContext), typeof(ItemKey) })]
#pragma warning restore IDE0300
internal static class HostRemoveItemPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(ItemKey itemKey)
    {
        if (HostRegistration.IsRegistered(itemKey))
        {
            FrontendNotifier.NotifyHostRemoved(itemKey);
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemDomain),
    nameof(ItemDomain.ForceRemoveItem),
    new[] { typeof(DataContext), typeof(ItemKey) })]
#pragma warning restore IDE0300
internal static class HostForceRemoveItemPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(ItemKey itemKey)
    {
        if (HostRegistration.IsRegistered(itemKey))
        {
            FrontendNotifier.NotifyHostRemoved(itemKey);
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(Character),
    nameof(Character.AddInventoryItem),
    new[]
    {
        typeof(DataContext),
        typeof(ItemKey),
        typeof(int),
        typeof(bool),
        typeof(EItemAutoOperationSource),
    })]
#pragma warning restore IDE0300
internal static class XiangshuCharacterInventoryItemAddedPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        Character __instance,
        ItemKey itemKey,
        int amount,
        bool __result)
    {
        if (__result
            && amount > 0
            && !ItemGraftPatches.IsCharacterInventoryTransferInProgress
            && HostRegistration.IsRegistered(itemKey))
        {
            FrontendNotifier.NotifyHostInventoryTransfer(
                itemKey,
                InventoryTransferRequest.NoCharacterInventoryId,
                __instance.GetId(),
                amount,
                "Xiangshu item graft host entered character inventory");
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(Character),
    nameof(Character.RemoveInventoryItem),
    new[]
    {
        typeof(DataContext),
        typeof(ItemKey),
        typeof(int),
        typeof(bool),
        typeof(bool),
    })]
#pragma warning restore IDE0300
internal static class XiangshuCharacterInventoryItemRemovedPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        Character __instance,
        ItemKey itemKey,
        int amount)
    {
        if (amount <= 0
            || ItemGraftPatches.IsCharacterInventoryTransferInProgress
            || !HostRegistration.IsRegistered(itemKey)
            || __instance.GetInventory().Items.ContainsKey(itemKey))
        {
            return;
        }

        FrontendNotifier.NotifyHostInventoryTransfer(
            itemKey,
            __instance.GetId(),
            InventoryTransferRequest.NoCharacterInventoryId,
            amount,
            "Xiangshu item graft host left character inventory");
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(CharacterDomain),
    nameof(CharacterDomain.TransferInventoryItem),
    new[]
    {
        typeof(DataContext),
        typeof(Character),
        typeof(Character),
        typeof(ItemKey),
        typeof(int),
        typeof(EItemAutoOperationSource),
    })]
#pragma warning restore IDE0300
internal static class CharacterInventoryItemTransferredPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix()
    {
        ItemGraftPatches.EnterCharacterInventoryTransfer();
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        Character srcChar,
        Character destChar,
        ItemKey itemKey,
        int amount)
    {
        if (amount <= 0
            || !HostRegistration.IsRegistered(itemKey)
            || !destChar.GetInventory().Items.ContainsKey(itemKey))
        {
            return;
        }

        FrontendNotifier.NotifyHostInventoryTransfer(
            itemKey,
            srcChar.GetId(),
            destChar.GetId(),
            amount,
            "Xiangshu item graft host transferred between character inventories");
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Finalizer()
    {
        ItemGraftPatches.ExitCharacterInventoryTransfer();
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(Character),
    nameof(Character.SetInventory),
    new[] { typeof(Inventory), typeof(DataContext) })]
#pragma warning restore IDE0300
internal static class XiangshuTaiwuInventorySnapshotChangedPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(Character __instance)
    {
        if (__instance.IsTaiwu())
        {
            FrontendNotifier.NotifyTaiwuInventorySnapshotChanged();
        }
    }
}
