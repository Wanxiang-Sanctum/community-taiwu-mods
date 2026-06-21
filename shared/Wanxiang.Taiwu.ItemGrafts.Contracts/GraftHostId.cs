using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

public readonly struct GraftHostId : IEquatable<GraftHostId>
{
    public GraftHostId(ItemKey hostKey)
    {
        ItemKey validatedHostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));

        ItemType = validatedHostKey.ItemType;
        TemplateId = validatedHostKey.TemplateId;
        ItemId = validatedHostKey.Id;
        IsValid = true;
    }

    public GraftHostId(sbyte itemType, short templateId, int itemId)
    {
        ValidateTemplate(itemType, templateId);

        if (itemId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemId),
                itemId,
                "Host item id must be valid.");
        }

        ItemType = itemType;
        TemplateId = templateId;
        ItemId = itemId;
        IsValid = true;
    }

    public sbyte ItemType { get; }

    public short TemplateId { get; }

    public int ItemId { get; }

    public bool IsValid { get; }

    public bool Matches(ItemKey hostKey)
    {
        return IsValid
            && hostKey.IsValid()
            && hostKey.ItemType == ItemType
            && hostKey.TemplateId == TemplateId
            && hostKey.Id == ItemId;
    }

    public bool Equals(GraftHostId other)
    {
        if (!IsValid || !other.IsValid)
        {
            return IsValid == other.IsValid;
        }

        return ItemType == other.ItemType
            && TemplateId == other.TemplateId
            && ItemId == other.ItemId;
    }

    public override bool Equals(object? obj)
    {
        return obj is GraftHostId other && Equals(other);
    }

    public override int GetHashCode()
    {
        if (!IsValid)
        {
            return 0;
        }

        unchecked
        {
            int hash = ItemType.GetHashCode();
            hash = (hash * 397) ^ TemplateId.GetHashCode();
            hash = (hash * 397) ^ ItemId;
            return hash;
        }
    }

    public override string ToString()
    {
        return IsValid
            ? $"{ItemType}:{TemplateId}:{ItemId}"
            : "Invalid";
    }

    public static bool operator ==(GraftHostId left, GraftHostId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GraftHostId left, GraftHostId right)
    {
        return !left.Equals(right);
    }

    private static void ValidateTemplate(sbyte itemType, short templateId)
    {
        if (!GraftHostValidation.IsValidTemplate(itemType, templateId))
        {
            throw new ArgumentException("Host item template must be valid and not stackable.");
        }
    }
}
