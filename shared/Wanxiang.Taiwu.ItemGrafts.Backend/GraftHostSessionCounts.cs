using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal sealed class GraftHostSessionCounts
{
    private readonly Dictionary<GraftHostId, int> _sessionCountsByHost = [];

    private readonly object _syncRoot = new();

    public void AddSession(ItemKey hostKey)
    {
        if (!GraftHostId.TryCreate(hostKey, out GraftHostId hostId))
        {
            throw new ArgumentException("Host item key must be valid.", nameof(hostKey));
        }

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

    private static bool TryGetHostId(ItemKey hostKey, out GraftHostId hostId)
    {
        return GraftHostId.TryCreate(hostKey, out hostId);
    }
}
