using System;
using GameData.Domains.Item;

namespace Wanxiang.Xiangshu.Backend.ItemGrafts;

internal static class HostRegistration
{
    private static readonly object SyncRoot = new();

    private static ItemKey s_hostKey = ItemKey.Invalid;

    public static void Register(ItemKey hostKey)
    {
        if (!hostKey.IsValid())
        {
            throw new ArgumentException("Host key must be valid.", nameof(hostKey));
        }

        lock (SyncRoot)
        {
            s_hostKey = hostKey;
        }
    }

    public static void Unregister()
    {
        lock (SyncRoot)
        {
            s_hostKey = ItemKey.Invalid;
        }
    }

    public static bool IsRegistered(ItemKey hostKey)
    {
        lock (SyncRoot)
        {
            return s_hostKey.IsValid()
                && s_hostKey == hostKey;
        }
    }
}
