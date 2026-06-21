using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

internal static class GraftHostValidation
{
    public static ItemKey ValidateKey(ItemKey hostKey, string parameterName)
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

    public static GraftHostTemplate ValidateTemplate(
        GraftHostTemplate hostTemplate,
        string parameterName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(hostTemplate, parameterName);
#else
        if (hostTemplate is null)
        {
            throw new ArgumentNullException(parameterName);
        }
#endif

        ValidateTemplate(
            hostTemplate.ItemType,
            hostTemplate.TemplateId,
            parameterName);

        return hostTemplate;
    }

    public static bool IsValidKey(ItemKey hostKey)
    {
        return hostKey.IsValid()
            && hostKey.HasTemplate
            && IsValidTemplate(hostKey.ItemType, hostKey.TemplateId);
    }

    public static bool IsValidTemplate(sbyte itemType, short templateId)
    {
        return ItemTemplateHelper.CheckTemplateValid(itemType, templateId)
            && !ItemTemplateHelper.IsStackable(itemType, templateId);
    }

    public static bool MatchesTemplate(ItemKey hostKey, GraftHostTemplate hostTemplate)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(hostTemplate);
#else
        if (hostTemplate is null)
        {
            throw new ArgumentNullException(nameof(hostTemplate));
        }
#endif

        return hostKey.IsValid()
            && hostKey.ItemType == hostTemplate.ItemType
            && hostKey.TemplateId == hostTemplate.TemplateId;
    }

    private static void ValidateTemplate(
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
