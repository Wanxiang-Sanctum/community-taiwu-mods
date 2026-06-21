using System.Diagnostics.CodeAnalysis;
using Config;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Frontend;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal static class ItemGraftRuntime
{
    private const string GraftName = "低语的陶土药钵";
    private const string GraftDescription = "药杵未动，钵底却传出细碎低语，自称相枢。";
    private const string ChatOperationLabel = "对话";

    private static readonly object SyncRoot = new();
    private static Graft? s_currentGraft;
    private static Action? s_openChatWindow;

    private static bool s_currentHostInTaiwuInventory;

    public static void Configure(Action openChatWindow)
    {
        if (openChatWindow is null)
        {
            throw new ArgumentNullException(nameof(openChatWindow));
        }

        lock (SyncRoot)
        {
            s_openChatWindow = openChatWindow;
        }
    }

    public static void Reset()
    {
        lock (SyncRoot)
        {
            s_currentGraft = null;
            s_currentHostInTaiwuInventory = false;
            s_openChatWindow = null;
        }
    }

    public static void ClearCurrent()
    {
        lock (SyncRoot)
        {
            s_currentGraft = null;
            s_currentHostInTaiwuInventory = false;
        }
    }

    public static void SetCurrent(
        Graft graft,
        bool isInTaiwuInventory)
    {
        if (graft is null)
        {
            throw new ArgumentNullException(nameof(graft));
        }

        lock (SyncRoot)
        {
            s_currentGraft = graft;
            s_currentHostInTaiwuInventory = isInTaiwuInventory;
        }
    }

    public static bool SetCurrentHostInTaiwuInventory(
        ItemKey hostKey,
        bool isInTaiwuInventory)
    {
        lock (SyncRoot)
        {
            if (s_currentGraft?.HostId.Matches(hostKey) == true)
            {
                bool changed = s_currentHostInTaiwuInventory != isInTaiwuInventory;
                s_currentHostInTaiwuInventory = isInTaiwuInventory;
                return changed;
            }
        }

        return false;
    }

    public static bool ClearCurrentIfHost(
        ItemKey hostKey,
        out bool wasInTaiwuInventory)
    {
        lock (SyncRoot)
        {
            if (s_currentGraft?.HostId.Matches(hostKey) == true)
            {
                wasInTaiwuInventory = s_currentHostInTaiwuInventory;
                s_currentGraft = null;
                s_currentHostInTaiwuInventory = false;
                return true;
            }
        }

        wasInTaiwuInventory = false;
        return false;
    }

    public static bool IsCurrentHostInTaiwuInventory
    {
        get
        {
            lock (SyncRoot)
            {
                return s_currentGraft is not null
                    && s_currentHostInTaiwuInventory;
            }
        }
    }

    public static bool TryGetCurrentHost(out ItemKey hostKey)
    {
        lock (SyncRoot)
        {
            if (s_currentGraft is not null)
            {
                hostKey = s_currentGraft.HostKey;
                return true;
            }
        }

        hostKey = ItemKey.Invalid;
        return false;
    }

    public static bool TryGet(
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

    public static bool TryGet(
        ItemKey key,
        [NotNullWhen(returnValue: true)] out Graft? graft)
    {
        if (!key.IsValid())
        {
            graft = null;
            return false;
        }

        lock (SyncRoot)
        {
            graft = s_currentGraft;
            return graft?.HostId.Matches(key) == true;
        }
    }

    public static GraftDefinition CreateDefinition()
    {
        CraftToolItem hostTemplate = CraftTool.DefValue.Medicine0;

        return new GraftDefinition(
            appearance: new GraftAppearance(
                name: GraftName,
                description: GraftDescription,
                iconName: hostTemplate.Icon,
                grade: hostTemplate.Grade),
            menuMode: GraftMenuMode.Replace,
            operations:
            [
                new GraftOperation(ChatOperationLabel, OpenChatWindow),
            ]);
    }

    private static void OpenChatWindow(ItemKey hostKey)
    {
        Action? openChatWindow;

        lock (SyncRoot)
        {
            if (s_currentGraft?.HostId.Matches(hostKey) != true
                || !s_currentHostInTaiwuInventory)
            {
                return;
            }

            openChatWindow = s_openChatWindow;
        }

        if (openChatWindow is null)
        {
            throw new InvalidOperationException("Xiangshu chat window opener is not configured.");
        }

        openChatWindow();
    }
}
