using System.Diagnostics.CodeAnalysis;
using FrameWork;
using Game.Components.Item;
using Game.Components.ListStyleGeneralScroll.Item;
using Game.Views;
using Game.Views.CharacterMenu;
using Game.Views.MouseTips.Item.Common;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.LifeRecord;
using GameData.Domains.LifeRecord.GeneralRecord;
using GameData.Domains.Taiwu;
using GameDataExtensions;
using HarmonyLib;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

internal static class GraftVisualLayer
{
    private const string HarmonyOwnerSuffix = "Wanxiang.Taiwu.ItemGrafts.Frontend";

    private static readonly object SyncRoot = new();

    private static Harmony? s_harmony;

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
            s_harmony.PatchAll(typeof(GraftVisualLayer).Assembly);
        }
    }

    internal static void Uninstall()
    {
        lock (SyncRoot)
        {
            s_harmony?.UnpatchSelf();
            s_harmony = null;
        }
    }
}

[HarmonyPatch(typeof(ViewCharacterMenuItems), nameof(ViewCharacterMenuItems.ShowItemOperateMenu))]
internal static class InventoryItemMenuPatch
{
    public static bool Prefix(
        ViewCharacterMenuItems __instance,
        ItemDisplayData itemData,
        RowItemLine rowItemLine)
    {
        if (!GraftVisualState.TryGet(itemData, out Graft? graft)
            || graft.MenuMode != GraftMenuMode.Replace)
        {
            return true;
        }

        if (!IsTaiwuInventoryItem(itemData))
        {
            return true;
        }

        ItemListScroll itemListScroll = __instance.itemListScroll;

        List<ViewPopupMenu.BtnData> buttons = CreateButtons(graft);

        if (buttons.Count > 0)
        {
            itemListScroll.SetItemToPopupMenuMode(rowItemLine, buttons);
        }

        return false;
    }

    private static bool IsTaiwuInventoryItem(ItemDisplayData itemData)
    {
        int taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
        return itemData.OwnerCharId == taiwuCharId
            && itemData.ItemSourceTypeEnum == ItemSourceType.Inventory;
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
            GLog.TagError("InventoryGrafts", ex);
        }
    }
}

[HarmonyPatch(typeof(ItemDisplayDataHelper), nameof(ItemDisplayDataHelper.GetName))]
internal static class ItemNamePatch
{
    public static void Postfix(
        ITradeableContent itemDisplayData,
        bool withGradeColor,
        ref string __result)
    {
        if (GraftVisualState.TryGet(itemDisplayData, out Graft? graft)
            && graft.Appearance.Name is not null)
        {
            __result = GraftVisuals.FormatName(
                itemDisplayData,
                graft.Appearance,
                withGradeColor);
        }
    }
}

[HarmonyPatch(typeof(ItemBack), nameof(ItemBack.Set))]
internal static class ItemBackPatch
{
    public static void Postfix(
        ItemBack __instance,
        ITradeableContent data)
    {
        if (GraftVisualState.TryGet(data, out Graft? graft))
        {
            GraftVisuals.ApplyToItemBack(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(CommonItemBack), nameof(CommonItemBack.SetData))]
internal static class CommonItemBackPatch
{
    public static void Postfix(
        CommonItemBack __instance,
        ItemDisplayData data)
    {
        if (GraftVisualState.TryGet(data, out Graft? graft))
        {
            GraftVisuals.ApplyToCommonItemBack(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(ItemView), nameof(ItemView.SetData))]
internal static class ItemViewPatch
{
    public static void Postfix(
        ItemView __instance,
        ItemDisplayData data)
    {
        if (GraftVisualState.TryGet(data, out Graft? graft))
        {
            GraftVisuals.ApplyToItemView(__instance, graft.Appearance);
        }
    }
}

[HarmonyPatch(
    typeof(TooltipItemCommonArea),
    nameof(TooltipItemCommonArea.Refresh),
    typeof(ItemDisplayData),
    typeof(bool))]
internal static class CommonTooltipPatch
{
    public static void Postfix(
        TooltipItemCommonArea __instance,
        ItemDisplayData itemData,
        bool isDetail)
    {
        if (GraftVisualState.TryGet(itemData, out Graft? graft))
        {
            GraftVisuals.ApplyToCommonTooltip(
                __instance,
                itemData,
                graft.Appearance,
                isDetail);
        }
    }
}

[HarmonyPatch(typeof(MouseTipMakingTool), nameof(MouseTipMakingTool.Init))]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Harmony constructs this patch by reflection.")]
internal static class MakingTooltipPatch
{
    public static void Postfix(
        MouseTipMakingTool __instance,
        ArgumentBox argsBox)
    {
        _ = argsBox.Get("ItemData", out ItemDisplayData itemData);

        if (GraftVisualState.TryGet(itemData, out Graft? graft))
        {
            GraftVisuals.ApplyToMakingTooltip(
                __instance,
                itemData,
                graft.Appearance);
        }
    }
}

[HarmonyPatch(typeof(global::GameMessageUtils), nameof(global::GameMessageUtils.RenderItemKeys))]
internal static class MessageItemKeysPatch
{
    public static void Prefix(
        RenderedArgumentCollection renderedArgCollection,
        out int __state)
    {
        __state = renderedArgCollection.ItemKeys.Count;
    }

    public static void Postfix(
        ArgumentCollection argumentCollection,
        RenderedArgumentCollection renderedArgCollection,
        int __state)
    {
        for (int i = 0; i < argumentCollection.ItemKeys.Count; i++)
        {
            ItemKey key = (ItemKey)argumentCollection.ItemKeys[i];

            if (GraftVisualState.TryGet(key, out Graft? graft)
                && graft.Appearance.Name is not null)
            {
                int renderedIndex = __state + i;
                string renderedName = renderedArgCollection.ItemKeys[renderedIndex];
                renderedArgCollection.ItemKeys[renderedIndex] =
                    GraftVisuals.ApplyRenderedItemName(
                        renderedName,
                        key,
                        graft.Appearance);
            }
        }
    }
}

[HarmonyPatch(
    typeof(global::GameMessageUtils),
    nameof(global::GameMessageUtils.RenderItemKey),
    typeof(int),
    typeof(TransferableRecordDataBase),
    typeof(bool))]
internal static class MessageItemKeyPatch
{
    public static void Postfix(
        int index,
        TransferableRecordDataBase data,
        ref string __result)
    {
        ItemKey key = (ItemKey)data.ArgumentCollection.ItemKeys[index];

        if (GraftVisualState.TryGet(key, out Graft? graft)
            && graft.Appearance.Name is not null)
        {
            __result = GraftVisuals.ApplyRenderedItemName(
                __result,
                key,
                graft.Appearance);
        }
    }
}
