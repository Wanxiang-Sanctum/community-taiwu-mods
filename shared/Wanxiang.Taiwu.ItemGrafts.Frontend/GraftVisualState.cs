using System.Diagnostics.CodeAnalysis;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

internal static class GraftVisualState
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<GraftHostId, List<Graft>> GraftsByHost = [];

    internal static bool HasActiveGrafts
    {
        get
        {
            lock (SyncRoot)
            {
                return GraftsByHost.Count > 0;
            }
        }
    }

    internal static void Add(Graft graft)
    {
        if (graft is null)
        {
            throw new ArgumentNullException(nameof(graft));
        }

        lock (SyncRoot)
        {
            if (!GraftsByHost.TryGetValue(graft.HostId, out List<Graft> grafts))
            {
                grafts = [];
                GraftsByHost.Add(graft.HostId, grafts);
            }

            grafts.Add(graft);
        }
    }

    internal static void Remove(Graft graft)
    {
        if (graft is null)
        {
            throw new ArgumentNullException(nameof(graft));
        }

        lock (SyncRoot)
        {
            if (!GraftsByHost.TryGetValue(graft.HostId, out List<Graft> grafts))
            {
                return;
            }

            _ = grafts.Remove(graft);

            if (grafts.Count == 0)
            {
                _ = GraftsByHost.Remove(graft.HostId);
            }
        }
    }

    internal static void Clear()
    {
        lock (SyncRoot)
        {
            GraftsByHost.Clear();
        }
    }

    internal static bool TryGet(
        ITradeableContent? content,
        [NotNullWhen(returnValue: true)] out Graft? graft)
    {
        if (content is null)
        {
            graft = null;
            return false;
        }

        return TryGet(content.RealKey, out graft);
    }

    internal static bool TryGet(
        ItemKey key,
        [NotNullWhen(returnValue: true)] out Graft? graft)
    {
        if (!GraftHostId.TryCreate(key, out GraftHostId hostId))
        {
            graft = null;
            return false;
        }

        lock (SyncRoot)
        {
            if (!GraftsByHost.TryGetValue(hostId, out List<Graft> grafts)
                || grafts.Count == 0)
            {
                graft = null;
                return false;
            }

            graft = grafts[^1];
            return true;
        }
    }
}
