using Config;
using GameData.Domains.Character.Display;

namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

internal static class FeatureDisplayState
{
    private static readonly object SyncRoot = new();

    private static readonly Dictionary<long, RegisteredFeature> Entries = [];

    private static readonly Dictionary<short, CharacterFeatureItem> DisplayItems = [];

    [ThreadStatic]
    private static int t_displayItemReadScopeDepth;

    private static long s_nextRegistrationId;

    internal static bool HasState
    {
        get
        {
            lock (SyncRoot)
            {
                return Entries.Count > 0 || DisplayItems.Count > 0;
            }
        }
    }

    internal static long Register(
        short featureId,
        int characterId)
    {
        lock (SyncRoot)
        {
            long registrationId = ++s_nextRegistrationId;
            Entries[registrationId] = new RegisteredFeature(
                featureId,
                characterId);
            return registrationId;
        }
    }

    internal static void SetDisplayItem(
        short featureId,
        CharacterFeatureItem item)
    {
        lock (SyncRoot)
        {
            DisplayItems[featureId] = item;
        }
    }

    internal static bool TryGetDisplayItem(
        short featureId,
        out CharacterFeatureItem item)
    {
        lock (SyncRoot)
        {
            return DisplayItems.TryGetValue(featureId, out item);
        }
    }

    internal static bool ContainsVirtualFeatureId(short featureId)
    {
        lock (SyncRoot)
        {
            return DisplayItems.ContainsKey(featureId);
        }
    }

    internal static bool IsDisplayItemReadScopeActive => t_displayItemReadScopeDepth > 0;

    internal static bool EnterDisplayItemReadScopeIfVirtual(short featureId)
    {
        if (!ContainsVirtualFeatureId(featureId))
        {
            return false;
        }

        t_displayItemReadScopeDepth++;
        return true;
    }

    internal static void ExitDisplayItemReadScope()
    {
        if (t_displayItemReadScopeDepth > 0)
        {
            t_displayItemReadScopeDepth--;
        }
    }

    internal static bool Unregister(long registrationId)
    {
        lock (SyncRoot)
        {
            return Entries.Remove(registrationId);
        }
    }

    internal static void Clear()
    {
        lock (SyncRoot)
        {
            Entries.Clear();
            DisplayItems.Clear();
        }
    }

    internal static void AppendVisibleFeatures(
        List<short> shownFeatureIds,
        CharacterDisplayData? displayData,
        int characterId)
    {
        if (shownFeatureIds is null)
        {
            return;
        }

        int resolvedCharacterId = ResolveCharacterId(displayData, characterId);
        RegisteredFeature[] entries = GetEntriesSnapshot();

        for (int i = 0; i < entries.Length; i++)
        {
            RegisteredFeature entry = entries[i];
            if (shownFeatureIds.Contains(entry.FeatureId)
                || entry.CharacterId != resolvedCharacterId)
            {
                continue;
            }

            shownFeatureIds.Add(entry.FeatureId);
        }
    }

    private static RegisteredFeature[] GetEntriesSnapshot()
    {
        lock (SyncRoot)
        {
            if (Entries.Count == 0)
            {
                return [];
            }

            RegisteredFeature[] entries = new RegisteredFeature[Entries.Count];
            Entries.Values.CopyTo(entries, 0);
            return entries;
        }
    }

    private static int ResolveCharacterId(
        CharacterDisplayData? displayData,
        int characterId)
    {
        return displayData?.CharacterId ?? characterId;
    }
}
