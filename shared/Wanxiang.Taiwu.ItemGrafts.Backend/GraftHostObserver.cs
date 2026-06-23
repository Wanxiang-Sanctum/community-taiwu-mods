using System.Diagnostics.CodeAnalysis;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using HarmonyLib;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal static class GraftHostObserver
{
    private const string HarmonyOwnerSuffix = "Wanxiang.Taiwu.ItemGrafts.Backend";

    private static readonly object SyncRoot = new();

    [ThreadStatic]
    private static int s_characterInventoryTransferDepth;

    private static Harmony? s_harmony;

    internal static event EventHandler<GraftHostEventArgs>? HostEvent;

    internal static bool IsInstalled => s_harmony is not null;

    internal static bool IsCharacterInventoryTransferInProgress => s_characterInventoryTransferDepth > 0;

    internal static void Install(string validatedModId)
    {
        lock (SyncRoot)
        {
            if (s_harmony is not null)
            {
                return;
            }

            s_harmony = new Harmony($"{validatedModId}.{HarmonyOwnerSuffix}");
            s_harmony.PatchAll(typeof(GraftHostObserver).Assembly);
        }
    }

    internal static void Uninstall()
    {
        lock (SyncRoot)
        {
            s_harmony?.UnpatchSelf();
            s_harmony = null;
            ObservedGraftHosts.Clear();
            HostEvent = null;
        }
    }

    internal static void EnterCharacterInventoryTransfer()
    {
        s_characterInventoryTransferDepth++;
    }

    internal static void ExitCharacterInventoryTransfer()
    {
        if (s_characterInventoryTransferDepth > 0)
        {
            s_characterInventoryTransferDepth--;
        }
    }

    internal static bool IsHostObserved(ItemKey hostKey)
    {
        return ObservedGraftHosts.ContainsHost(hostKey);
    }

    internal static ItemKey ResolveCurrentHostKey(ItemKey hostKey)
    {
        if (!hostKey.IsValid())
        {
            return hostKey;
        }

        ItemBase? item = DomainManager.Item.TryGetBaseItem(hostKey);
        return item?.GetItemKey() ?? hostKey;
    }

    internal static void NotifyHostRemoved(ItemKey hostKey)
    {
        if (ObservedGraftHosts.RemoveHost(hostKey))
        {
            HostEvent?.Invoke(null, GraftHostEventArgs.Removed(hostKey));
        }
    }

    internal static void NotifyHostLocationChanged(
        ItemKey hostKey,
        int? fromCharacterId,
        int? toCharacterId)
    {
        HostEvent?.Invoke(
            null,
            GraftHostEventArgs.LocationChanged(
                hostKey,
                fromCharacterId,
                toCharacterId));
    }

    internal static void NotifyHostDataChanged(ItemKey hostKey)
    {
        if (ObservedGraftHosts.ContainsHost(hostKey))
        {
            HostEvent?.Invoke(null, GraftHostEventArgs.DataChanged(hostKey));
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(BaseGameDataObject),
    "SetModifiedAndInvalidateInfluencedCache",
    new[] { typeof(ushort), typeof(DataContext) })]
#pragma warning restore IDE0300
internal static class GraftHostItemDataChangedPatch
{
    [SuppressMessage(
        "CodeQuality",
        "IDE0051:Remove unused private members",
        Justification = "Harmony invokes patch methods by signature.")]
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(BaseGameDataObject __instance)
    {
        if (__instance is not ItemBase item)
        {
            return;
        }

        ItemKey hostKey = item.GetItemKey();
        if (GraftHostObserver.IsHostObserved(hostKey))
        {
            GraftHostObserver.NotifyHostDataChanged(hostKey);
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemDomain),
    nameof(ItemDomain.RemoveItem),
    new[] { typeof(DataContext), typeof(ItemKey) })]
#pragma warning restore IDE0300
internal static class GraftHostRemoveItemPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(ItemKey itemKey, out ItemKey __state)
    {
        __state = GraftHostObserver.ResolveCurrentHostKey(itemKey);
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(ItemKey itemKey, ItemKey __state)
    {
        ItemKey hostKey = __state.IsValid() ? __state : itemKey;

        if (GraftHostObserver.IsHostObserved(hostKey))
        {
            GraftHostObserver.NotifyHostRemoved(hostKey);
        }
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemDomain),
    nameof(ItemDomain.ForceRemoveItem),
    new[] { typeof(DataContext), typeof(ItemKey) })]
#pragma warning restore IDE0300
internal static class GraftHostForceRemoveItemPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(ItemKey itemKey, out ItemKey __state)
    {
        __state = GraftHostObserver.ResolveCurrentHostKey(itemKey);
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(ItemKey itemKey, ItemKey __state)
    {
        ItemKey hostKey = __state.IsValid() ? __state : itemKey;

        if (GraftHostObserver.IsHostObserved(hostKey))
        {
            GraftHostObserver.NotifyHostRemoved(hostKey);
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
internal static class GraftHostCharacterInventoryItemAddedPatch
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
        ItemKey hostKey = GraftHostObserver.ResolveCurrentHostKey(itemKey);

        if (__result
            && amount > 0
            && !GraftHostObserver.IsCharacterInventoryTransferInProgress
            && GraftHostObserver.IsHostObserved(hostKey))
        {
            GraftHostObserver.NotifyHostLocationChanged(
                hostKey,
                null,
                __instance.GetId());
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
internal static class GraftHostCharacterInventoryItemRemovedPatch
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
        ItemKey hostKey = GraftHostObserver.ResolveCurrentHostKey(itemKey);

        if (amount <= 0
            || GraftHostObserver.IsCharacterInventoryTransferInProgress
            || !GraftHostObserver.IsHostObserved(hostKey)
            || __instance.GetInventory().Items.ContainsKey(itemKey))
        {
            return;
        }

        GraftHostObserver.NotifyHostLocationChanged(
            hostKey,
            __instance.GetId(),
            null);
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
internal static class GraftHostCharacterInventoryItemTransferredPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix()
    {
        GraftHostObserver.EnterCharacterInventoryTransfer();
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
        ItemKey hostKey = GraftHostObserver.ResolveCurrentHostKey(itemKey);

        if (amount <= 0
            || !GraftHostObserver.IsHostObserved(hostKey)
            || !destChar.GetInventory().Items.ContainsKey(itemKey))
        {
            return;
        }

        GraftHostObserver.NotifyHostLocationChanged(
            hostKey,
            srcChar.GetId(),
            destChar.GetId());
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Finalizer()
    {
        GraftHostObserver.ExitCharacterInventoryTransfer();
    }
}
