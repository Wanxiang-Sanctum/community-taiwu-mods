using Game.Components.Item;
using Game.Views.MouseTips.Item.Common;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.LifeRecord.GeneralRecord;
using TMPro;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

internal static class GraftVisuals
{
    internal static string FormatName(
        ITradeableContent content,
        GraftAppearance appearance,
        bool withGradeColor,
        string renderedName)
    {
        bool usesRenderedName = appearance.Name is null;
        string name = appearance.Name ?? renderedName;

        if (!withGradeColor
            || (appearance.Name is null && appearance.VisualGrade is null))
        {
            return name;
        }

        sbyte nameColorGrade = appearance.VisualGrade ?? content.Grade;
        return nameColorGrade >= 0
            ? (usesRenderedName ? name.RemoveColorTags() : name)
                .SetColor(Colors.Instance.GradeColors[nameColorGrade])
            : name;
    }

    internal static void ApplyRenderedItemKeyNames(
        ArgumentCollection argumentCollection,
        RenderedArgumentCollection renderedArgCollection,
        int firstRenderedIndex)
    {
        for (int i = 0; i < argumentCollection.ItemKeys.Count; i++)
        {
            int renderedIndex = firstRenderedIndex + i;
            ItemKey key = (ItemKey)argumentCollection.ItemKeys[i];
            renderedArgCollection.ItemKeys[renderedIndex] = ApplyRenderedItemKeyName(
                key,
                renderedArgCollection.ItemKeys[renderedIndex]);
        }
    }

    internal static string ApplyRenderedItemKeyName(
        ItemKey key,
        string renderedName)
    {
        if (!GraftVisualState.TryGet(key, out Graft? graft))
        {
            return renderedName;
        }

        GraftAppearance appearance = graft.Appearance;

        if (appearance.Name is null
            || string.IsNullOrEmpty(renderedName))
        {
            return renderedName;
        }

        string templateName = ItemTemplateHelper.GetName(key.ItemType, key.TemplateId)
            .Replace('\n', ' ');

        return string.IsNullOrEmpty(templateName)
            ? renderedName
            : renderedName.Replace(
                templateName,
                appearance.Name.Replace('\n', ' '),
                StringComparison.Ordinal);
    }

    internal static void ApplyToItemBack(
        ItemBack itemBack,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemBack.SetIcon(appearance.IconName);
        }

        if (appearance.VisualGrade is sbyte visualGrade)
        {
            itemBack.SetBack(visualGrade);
        }
    }

    internal static void ApplyToCommonItemBack(
        CommonItemBack itemBack,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemBack.CGet<CImage>("Icon").SetSprite(appearance.IconName);
        }

        if (appearance.VisualGrade is sbyte visualGrade)
        {
            itemBack.CGet<CImage>("GradeBack").SetSprite(CommonItemBack.GetGradeBack(visualGrade));
        }
    }

    internal static void ApplyToItemView(
        ItemView itemView,
        GraftAppearance appearance)
    {
        if (appearance.IconName is not null)
        {
            itemView.SetIcon(appearance.IconName);
        }

        if (appearance.VisualGrade is sbyte visualGrade)
        {
            ApplyItemViewGradeVisuals(itemView, visualGrade);
        }

        if (appearance.Name is not null
            && itemView.Names.Contains("Name"))
        {
            itemView.CGet<TextMeshProUGUI>("Name").text = appearance.Name;
        }
    }

    internal static void ApplyToMakingTooltip(
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

        if (appearance.VisualGrade is sbyte visualGrade)
        {
            toolTip.CGet<CImage>("GradeBack").SetSprite(ItemView.GetGradeIcon(visualGrade));
        }

        if (appearance.Description is not null)
        {
            toolTip.SetItemDesc(
                appearance.Description,
                itemData.LoveTokenDataItem);
        }
    }

    internal static void ApplyToCommonTooltip(
        TooltipItemCommonArea commonArea,
        ItemDisplayData itemData,
        GraftAppearance appearance,
        bool isDetail)
    {
        ItemKey hostKey = itemData.RealKey;
        string name = appearance.Name
            ?? ItemTemplateHelper.GetName(hostKey.ItemType, hostKey.TemplateId);
        string description = (appearance.Description
                ?? ItemTemplateHelper.GetDesc(hostKey.ItemType, hostKey.TemplateId))
            .ColorReplace();
        string functionDescription = (appearance.DetailDescription
                ?? ItemTemplateHelper.GetFunctionDesc(hostKey.ItemType, hostKey.TemplateId))
            .ColorReplace();
        sbyte hostGrade = ItemTemplateHelper.GetGrade(hostKey.ItemType, hostKey.TemplateId);
        string icon = appearance.IconName
            ?? ItemTemplateHelper.GetIcon(hostKey.ItemType, hostKey.TemplateId);
        string itemType = CommonUtils.GetItemTypeName(hostKey.ItemType);
        int baseValue = ItemTemplateHelper.GetBaseValue(hostKey.ItemType, hostKey.TemplateId);
        string value = TooltipItemBase.GetBonusValue(
            baseValue,
            (int)itemData.Value,
            isDetail);

        commonArea.Refresh(
            itemData,
            name,
            description,
            functionDescription,
            hostGrade,
            icon,
            itemType,
            value);

        if (appearance.VisualGrade is sbyte visualGrade)
        {
            ApplyCommonTooltipGradeBackground(commonArea, visualGrade);
        }
    }

    private static void ApplyCommonTooltipGradeBackground(
        TooltipItemCommonArea commonArea,
        sbyte visualGrade)
    {
        // Refresh owns the grade text; visual grade only replaces the background.
        commonArea.imageGradeBack.SetSprite("ui9_mousetip_base_level_" + visualGrade);
    }

    private static void ApplyItemViewGradeVisuals(
        ItemView itemView,
        sbyte visualGrade)
    {
        // ItemView.SetGrade also rewrites the grade label; keep this to visual surfaces.
        itemView.CGet<CImage>("IconBack").SetSprite(ItemView.GetGradeBack(visualGrade));

        CImage gradeBack = itemView.CGet<CImage>("GradeBack");
        gradeBack.gameObject.SetActive(true);
        gradeBack.SetSprite(ItemView.GetGradeIcon(visualGrade));
    }
}
