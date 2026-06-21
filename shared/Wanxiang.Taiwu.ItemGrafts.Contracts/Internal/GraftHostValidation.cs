using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

internal static class GraftHostValidation
{
    internal static ItemKey ValidateKey(ItemKey hostKey, string parameterName)
    {
        if (!hostKey.IsValid())
        {
            throw new ArgumentException("Host item key must be valid.", parameterName);
        }

        if (!hostKey.HasTemplate)
        {
            throw new ArgumentException("Host item key must have a template.", parameterName);
        }

        ValidateTemplate(hostKey.ItemType, hostKey.TemplateId, parameterName);
        return hostKey;
    }

    internal static bool IsValidKey(ItemKey hostKey)
    {
        return hostKey.IsValid()
            && hostKey.HasTemplate
            && IsValidTemplate(hostKey.ItemType, hostKey.TemplateId);
    }

    internal static bool IsValidTemplate(sbyte itemType, short templateId)
    {
        return ItemTemplateHelper.CheckTemplateValid(itemType, templateId)
            && !ItemTemplateHelper.IsStackable(itemType, templateId);
    }

    internal static void ValidateTemplate(
        sbyte itemType,
        short templateId,
        string parameterName)
    {
        if (!ItemTemplateHelper.CheckTemplateValid(itemType, templateId))
        {
            throw new ArgumentException(
                "Host item template must be valid.",
                parameterName);
        }

        if (ItemTemplateHelper.IsStackable(itemType, templateId))
        {
            throw new ArgumentException(
                "Host item must not be stackable.",
                parameterName);
        }
    }
}
