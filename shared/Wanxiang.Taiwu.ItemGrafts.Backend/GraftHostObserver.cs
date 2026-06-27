using System.Diagnostics.CodeAnalysis;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Item;
using HarmonyLib;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal static class GraftHostObserver
{
    private const string HarmonyOwnerSuffix = "Wanxiang.Taiwu.ItemGrafts.Backend";

    private static readonly object SyncRoot = new();

    private static Harmony? s_harmony;

    internal static event EventHandler<GraftHostEventArgs>? HostEvent;

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

    internal static void NotifyHostOwnerChanged(
        ItemKey hostKey,
        ItemOwnerKey fromOwner,
        ItemOwnerKey toOwner)
    {
        if (ObservedGraftHosts.ContainsHost(hostKey))
        {
            HostEvent?.Invoke(
                null,
                GraftHostEventArgs.OwnerChanged(
                    hostKey,
                    ToContractOwner(fromOwner),
                    ToContractOwner(toOwner)));
        }
    }

    internal static void NotifyHostDataChanged(ItemKey hostKey)
    {
        if (ObservedGraftHosts.ContainsHost(hostKey))
        {
            HostEvent?.Invoke(null, GraftHostEventArgs.DataChanged(hostKey));
        }
    }

    internal static void NotifyHostOwnerMaybeChanged(
        ItemBase item,
        ItemOwnerKey previousOwner)
    {
        ItemOwnerKey currentOwner = item.Owner;

        if (previousOwner.Equals(currentOwner))
        {
            return;
        }

        NotifyHostOwnerChanged(
            item.GetItemKey(),
            previousOwner,
            currentOwner);
    }

    private static GraftHostOwnerKey? ToContractOwner(ItemOwnerKey owner)
    {
        return owner.OwnerType == ItemOwnerType.None
            ? null
            : new GraftHostOwnerKey((sbyte)owner.OwnerType, owner.OwnerId);
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
    typeof(ItemBase),
    nameof(ItemBase.SetOwner),
    new[]
    {
        typeof(ItemOwnerType),
        typeof(int),
    })]
#pragma warning restore IDE0300
internal static class GraftHostItemOwnerSetPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        ItemBase __instance,
        out ItemOwnerKey __state)
    {
        __state = __instance.Owner;
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        ItemBase __instance,
        ItemOwnerKey __state)
    {
        GraftHostObserver.NotifyHostOwnerMaybeChanged(__instance, __state);
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemBase),
    nameof(ItemBase.RemoveOwner),
    new[]
    {
        typeof(ItemOwnerType),
        typeof(int),
    })]
#pragma warning restore IDE0300
internal static class GraftHostItemOwnerRemovePatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        ItemBase __instance,
        out ItemOwnerKey __state)
    {
        __state = __instance.Owner;
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        ItemBase __instance,
        ItemOwnerKey __state)
    {
        GraftHostObserver.NotifyHostOwnerMaybeChanged(__instance, __state);
    }
}

#pragma warning disable IDE0300
[HarmonyPatch(
    typeof(ItemBase),
    nameof(ItemBase.ResetOwner))]
#pragma warning restore IDE0300
internal static class GraftHostItemOwnerResetPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Prefix(
        ItemBase __instance,
        out ItemOwnerKey __state)
    {
        __state = __instance.Owner;
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static void Postfix(
        ItemBase __instance,
        ItemOwnerKey __state)
    {
        GraftHostObserver.NotifyHostOwnerMaybeChanged(__instance, __state);
    }
}
