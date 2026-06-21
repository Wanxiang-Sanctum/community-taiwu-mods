using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal static class ObservedGraftHosts
{
    private static readonly GraftHostSessionCounts SessionCounts = new();

    internal static void AddSession(ItemKey hostKey)
    {
        SessionCounts.AddSession(hostKey);
    }

    internal static bool RemoveSession(ItemKey hostKey)
    {
        return SessionCounts.RemoveSession(hostKey);
    }

    internal static bool RemoveHost(ItemKey hostKey)
    {
        return SessionCounts.RemoveHost(hostKey);
    }

    internal static void Clear()
    {
        SessionCounts.Clear();
    }

    internal static bool ContainsHost(ItemKey hostKey)
    {
        return SessionCounts.ContainsHost(hostKey);
    }
}
