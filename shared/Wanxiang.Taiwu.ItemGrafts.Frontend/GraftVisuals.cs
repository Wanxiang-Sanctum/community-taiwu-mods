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
            || (appearance.Name is null && appearance.Grade is null))
        {
            return name;
        }

        sbyte grade = appearance.Grade ?? content.Grade;
        return grade >= 0
            ? (usesRenderedName ? name.RemoveColorTags() : name)
                .SetColor(Colors.Instance.GradeColors[grade])
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

        if (appearance.Grade is sbyte grade)
        {
            itemBack.SetBack(grade);
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

        if (appearance.Grade is sbyte grade)
        {
            itemBack.CGet<CImage>("GradeBack").SetSprite(CommonItemBack.GetGradeBack(grade));
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

        if (appearance.Grade is sbyte grade)
        {
            toolTip.CGet<CImage>("GradeBack").SetSprite(ItemView.GetGradeIcon(grade));
            toolTip.CGet<TextMeshProUGUI>("GradeName").text = ItemView.GetGradeText(grade);
            toolTip.CGet<TextMeshProUGUI>("Grade").text = ItemView.GetLastGradeText(grade);
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
        ItemKey key = itemData.RealKey;
        string name = appearance.Name
            ?? ItemTemplateHelper.GetName(key.ItemType, key.TemplateId);
        string description = (appearance.Description
                ?? ItemTemplateHelper.GetDesc(key.ItemType, key.TemplateId))
            .ColorReplace();
        string functionDescription = ItemTemplateHelper.GetFunctionDesc(
                key.ItemType,
                key.TemplateId)
            .ColorReplace();
        sbyte grade = appearance.Grade
            ?? ItemTemplateHelper.GetGrade(key.ItemType, key.TemplateId);
        string icon = appearance.IconName
            ?? ItemTemplateHelper.GetIcon(key.ItemType, key.TemplateId);
        string itemType = CommonUtils.GetItemTypeName(key.ItemType);
        int baseValue = ItemTemplateHelper.GetBaseValue(key.ItemType, key.TemplateId);
        string value = TooltipItemBase.GetBonusValue(
            baseValue,
            (int)itemData.Value,
            isDetail);

        commonArea.Refresh(
            itemData,
            name,
            description,
            functionDescription,
            grade,
            icon,
            itemType,
            value);
    }

}
