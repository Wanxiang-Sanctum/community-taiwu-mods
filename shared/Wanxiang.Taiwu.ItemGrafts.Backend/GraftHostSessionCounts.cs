using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal sealed class GraftHostSessionCounts
{
    private readonly Dictionary<GraftHostId, int> _sessionCountsByHost = [];

    private readonly object _syncRoot = new();

    public void AddSession(ItemKey hostKey)
    {
        GraftHostId hostId = GetHostId(hostKey);

        lock (_syncRoot)
        {
            _sessionCountsByHost[hostId] = _sessionCountsByHost.TryGetValue(
                hostId,
                out int sessionCount)
                ? sessionCount + 1
                : 1;
        }
    }

    public bool RemoveSession(ItemKey hostKey)
    {
        if (!TryGetHostId(hostKey, out GraftHostId hostId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (!_sessionCountsByHost.TryGetValue(hostId, out int sessionCount))
            {
                return false;
            }

            if (sessionCount <= 1)
            {
                _ = _sessionCountsByHost.Remove(hostId);
            }
            else
            {
                _sessionCountsByHost[hostId] = sessionCount - 1;
            }

            return true;
        }
    }

    public bool RemoveHost(ItemKey hostKey)
    {
        if (!TryGetHostId(hostKey, out GraftHostId hostId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _sessionCountsByHost.Remove(hostId);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _sessionCountsByHost.Clear();
        }
    }

    public bool ContainsHost(ItemKey hostKey)
    {
        if (!TryGetHostId(hostKey, out GraftHostId hostId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _sessionCountsByHost.ContainsKey(hostId);
        }
    }

    private static GraftHostId GetHostId(ItemKey hostKey)
    {
        return new GraftHostId(hostKey);
    }

    private static bool TryGetHostId(ItemKey hostKey, out GraftHostId hostId)
    {
        hostId = default;

        if (!GraftHostValidation.IsValidKey(hostKey))
        {
            return false;
        }

        hostId = new GraftHostId(hostKey);
        return true;
    }
}
