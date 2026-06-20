using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Config;
using FrameWork;
using Game.Components.Item;
using Game.Components.ListStyleGeneralScroll.Item;
using Game.Views;
using Game.Views.CharacterMenu;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.Taiwu;
using GameDataExtensions;
using HarmonyLib;
using TMPro;
using Wanxiang.Taiwu.ItemGrafts;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal static class ItemGraftPatches
{
    private const string HarmonyId = "Wanxiang.Xiangshu.ItemGrafts";
    private static Harmony? s_harmony;

    public static void Install(FrontendPlugin plugin)
    {
        if (plugin is null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }

        ItemGraftRuntime.Configure(plugin.OpenChatWindow);

        if (s_harmony is not null)
        {
            return;
        }

        s_harmony = new Harmony(HarmonyId);
        s_harmony.PatchAll(typeof(ItemGraftPatches).Assembly);
    }

    public static void Uninstall()
    {
        s_harmony?.UnpatchSelf();
        s_harmony = null;
        ItemGraftRuntime.Reset();
    }
}

internal static class ItemGraftVisuals
{
    private static readonly MethodInfo SetItemDescMethod =
        AccessTools.Method(typeof(MouseTipBase), "SetItemDesc");

    public static string FormatName(
        ITradeableContent content,
        GraftAppearance appearance,
        bool withGradeColor)
    {
        string? name = appearance.Name;

        if (name is null)
        {
            return string.Empty;
        }

        if (!withGradeColor)
        {
            return name;
        }

        sbyte grade = appearance.Grade ?? content.Grade;
        return grade >= 0
            ? name.SetColor(Colors.Instance.GradeColors[grade])
            : name;
    }

    public static void ApplyToItemBack(
        ItemBack itemBack,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemBack.SetIcon(appearance.IconName);
        }

        if (appearance.Grade is sbyte grade)
        {
            itemBack.SetBack(grade);
        }
    }

    public static void ApplyToCommonItemBack(
        CommonItemBack itemBack,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemBack.CGet<CImage>("Icon").SetSprite(appearance.IconName);
        }

        if (appearance.Grade is sbyte grade)
        {
            itemBack.CGet<CImage>("GradeBack").SetSprite(CommonItemBack.GetGradeBack(grade));
        }
    }

    public static void ApplyToItemView(
        ItemView itemView,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemView.SetIcon(appearance.IconName);
        }

        if (appearance.Grade is sbyte grade)
        {
            itemView.SetGrade(showGrade: true, grade);
        }

        if (appearance.Name is not null
            && itemView.Names.Contains("Name"))
        {
            itemView.CGet<TextMeshProUGUI>("Name").text = appearance.Name;
        }
    }

    public static void ApplyToMakingToolTip(
        MouseTipMakingTool toolTip,
        ItemDisplayData itemData,
        GraftAppearance appearance)
    {
        if (appearance.Name is not null)
        {
            toolTip.CGet<TextMeshProUGUI>("Name").text = appearance.Name;
        }

        if (appearance.IconName is not null)
        {
            toolTip.CGet<CImage>("ItemIcon").SetSprite(appearance.IconName);
        }

        if (appearance.Grade is sbyte grade)
        {
            toolTip.CGet<CImage>("GradeBack").SetSprite(ItemView.GetGradeIcon(grade));
            toolTip.CGet<TextMeshProUGUI>("GradeName").text =
                LocalStringManager.Get($"LK_ShortGrade_{grade}");
            toolTip.CGet<TextMeshProUGUI>("Grade").text =
                (LocalStringManager.Get($"LK_Num_{9 - grade}")
                    + LocalStringManager.Get(LanguageKey.LK_Item_Grade))
                .SetColor(Colors.Instance.GradeColors[grade]);
        }

        if (appearance.Description is not null)
        {
            _ = SetItemDescMethod.Invoke(
                toolTip,
                [appearance.Description, itemData.LoveTokenDataItem]);
        }
    }
}

[HarmonyPatch(typeof(ViewCharacterMenuItems), "ShowItemOperateMenu")]
internal static class ViewCharacterMenuItemsShowItemOperateMenuPatch
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");
    private static readonly FieldInfo ItemListScrollField =
        AccessTools.Field(typeof(ViewCharacterMenuItems), "itemListScroll");
    private static bool s_loggedMissingItemListScroll;

    public static bool Prefix(
        ViewCharacterMenuItems __instance,
        ItemDisplayData itemData,
        RowItemLine rowItemLine)
    {
        if (!ItemGraftRuntime.TryGet(itemData, out Graft? graft)
            || graft.MenuMode != GraftMenuMode.Replace)
        {
            return true;
        }

        if (!IsTaiwuInventoryItem(itemData))
        {
            return true;
        }

        if (ItemListScrollField.GetValue(__instance) is not ItemListScroll itemListScroll)
        {
            LogMissingItemListScroll();
            return false;
        }

        List<ViewPopupMenu.BtnData> buttons = CreateButtons(graft);

        if (buttons.Count > 0)
        {
            itemListScroll.SetItemToPopupMenuMode(rowItemLine, buttons);
        }

        return false;
    }

    private static bool IsTaiwuInventoryItem(ItemDisplayData itemData)
    {
        try
        {
            int taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
            return itemData.OwnerCharId == taiwuCharId
                && itemData.ItemSourceTypeEnum == ItemSourceType.Inventory;
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            return false;
        }
    }

    private static List<ViewPopupMenu.BtnData> CreateButtons(Graft graft)
    {
        List<ViewPopupMenu.BtnData> buttons = [];

        for (int i = 0; i < graft.Operations.Count; i++)
        {
            GraftOperation operation = graft.Operations[i];
            ViewPopupMenu.BtnData button = new(
                operation.Label,
                operation.IsEnabled,
                EItemMenuDisplayOrder.Use,
                () => InvokeOperation(graft, operation));

            if (!string.IsNullOrEmpty(operation.DisabledReason))
            {
                button.SetTip(operation.Label, operation.DisabledReason);
            }

            buttons.Add(button);
        }

        return buttons;
    }

    private static void InvokeOperation(
        Graft graft,
        GraftOperation operation)
    {
        try
        {
            operation.Invoke(graft.HostKey);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Error(ex, "failed to invoke Xiangshu item graft operation");
        }
    }

    private static void LogMissingItemListScroll()
    {
        if (s_loggedMissingItemListScroll)
        {
            return;
        }

        s_loggedMissingItemListScroll = true;
        Log.Error("cannot render Xiangshu item graft menu because ViewCharacterMenuItems.itemListScroll was not found");
    }
}

[HarmonyPatch(typeof(ItemDisplayDataHelper), nameof(ItemDisplayDataHelper.GetName))]
internal static class ItemDisplayDataHelperGetNamePatch
{
    public static void Postfix(
        ITradeableContent itemDisplayData,
        bool withGradeColor,
        ref string __result)
    {
        if (ItemGraftRuntime.TryGet(itemDisplayData, out Graft? graft)
            && graft.Appearance.Name is not null)
        {
            __result = ItemGraftVisuals.FormatName(
                itemDisplayData,
                graft.Appearance,
                withGradeColor);
        }
    }
}

[HarmonyPatch(typeof(ItemBack), nameof(ItemBack.Set))]
internal static class ItemBackSetPatch
{
    public static void Postfix(
        ItemBack __instance,
        ITradeableContent data)
    {
        if (ItemGraftRuntime.TryGet(data, out Graft? graft))
        {
            ItemGraftVisuals.ApplyToItemBack(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(CommonItemBack), nameof(CommonItemBack.SetData))]
internal static class CommonItemBackSetDataPatch
{
    public static void Postfix(
        CommonItemBack __instance,
        ItemDisplayData data)
    {
        if (ItemGraftRuntime.TryGet(data, out Graft? graft))
        {
            ItemGraftVisuals.ApplyToCommonItemBack(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(ItemView), nameof(ItemView.SetData))]
internal static class ItemViewSetDataPatch
{
    public static void Postfix(
        ItemView __instance,
        ItemDisplayData data)
    {
        if (ItemGraftRuntime.TryGet(data, out Graft? graft))
        {
            ItemGraftVisuals.ApplyToItemView(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(MouseTipMakingTool), "Init")]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class MouseTipMakingToolInitPatch
{
    public static void Postfix(
        MouseTipMakingTool __instance,
        ArgumentBox argsBox)
    {
        _ = argsBox.Get("ItemData", out ItemDisplayData itemData);

        if (ItemGraftRuntime.TryGet(itemData, out Graft? graft))
        {
            ItemGraftVisuals.ApplyToMakingToolTip(
                __instance,
                itemData,
                graft.Appearance);
        }
    }
}
