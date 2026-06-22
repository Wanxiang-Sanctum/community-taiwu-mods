using Config;
using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Frontend;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal static class XiangshuGraftState
{
    private const string BowlName = "低语的陶土药钵";
    private const string BowlDescription = "药杵未动，钵底却传出细碎低语，自称相枢。";
    private const string ChatOperationLabel = "对话";

    private static readonly object SyncRoot = new();
    private static Graft? s_currentGraft;
    private static Action? s_openChatWindow;

    private static bool s_hostInTaiwuInventory;

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
            s_hostInTaiwuInventory = false;
            s_openChatWindow = null;
        }
    }

    public static void ClearCurrent()
    {
        lock (SyncRoot)
        {
            s_currentGraft = null;
            s_hostInTaiwuInventory = false;
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
            s_hostInTaiwuInventory = isInTaiwuInventory;
        }
    }

    public static bool SetHostInTaiwuInventory(
        ItemKey hostKey,
        bool isInTaiwuInventory)
    {
        lock (SyncRoot)
        {
            if (s_currentGraft?.HostId.Matches(hostKey) == true)
            {
                bool changed = s_hostInTaiwuInventory != isInTaiwuInventory;
                s_hostInTaiwuInventory = isInTaiwuInventory;
                return changed;
            }
        }

        return false;
    }

    public static bool ClearIfHost(
        ItemKey hostKey,
        out bool wasInTaiwuInventory)
    {
        lock (SyncRoot)
        {
            if (s_currentGraft?.HostId.Matches(hostKey) == true)
            {
                wasInTaiwuInventory = s_hostInTaiwuInventory;
                s_currentGraft = null;
                s_hostInTaiwuInventory = false;
                return true;
            }
        }

        wasInTaiwuInventory = false;
        return false;
    }

    public static bool IsHostInTaiwuInventory
    {
        get
        {
            lock (SyncRoot)
            {
                return s_currentGraft is not null
                    && s_hostInTaiwuInventory;
            }
        }
    }

    public static bool TryGetHost(out ItemKey hostKey)
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

    public static GraftDefinition CreateDefinition()
    {
        CraftToolItem hostTemplate = CraftTool.DefValue.Medicine0;

        return new GraftDefinition(
            appearance: new GraftAppearance(
                name: BowlName,
                description: BowlDescription,
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
                || !s_hostInTaiwuInventory)
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
