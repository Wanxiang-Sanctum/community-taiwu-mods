using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 用物品类型、模板 ID 和物品实例 ID 标识一个真实嫁接宿主物品。
/// </summary>
public readonly struct GraftHostId : IEquatable<GraftHostId>
{
    /// <summary>
    /// 从有效的非堆叠太吾物品 key 创建宿主身份。
    /// </summary>
    /// <param name="hostKey">真实宿主物品 key。</param>
    public GraftHostId(ItemKey hostKey)
    {
        ItemKey validatedHostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));

        ItemType = validatedHostKey.ItemType;
        TemplateId = validatedHostKey.TemplateId;
        ItemId = validatedHostKey.Id;
        IsValid = true;
    }

    /// <summary>
    /// 从太吾物品身份字段创建宿主身份。
    /// </summary>
    /// <param name="itemType">太吾物品类型。</param>
    /// <param name="templateId">太吾物品模板 ID。</param>
    /// <param name="itemId">太吾物品实例 ID。</param>
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

    /// <summary>
    /// 获取太吾物品类型。
    /// </summary>
    public sbyte ItemType { get; }

    /// <summary>
    /// 获取太吾物品模板 ID。
    /// </summary>
    public short TemplateId { get; }

    /// <summary>
    /// 获取太吾物品实例 ID。
    /// </summary>
    public int ItemId { get; }

    /// <summary>
    /// 获取该身份是否由有效宿主字段创建。
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 判断给定的完整物品 key 是否属于这个稳定宿主身份。
    /// </summary>
    /// <param name="hostKey">要比较的当前太吾物品 key。</param>
    /// <returns>当物品 key 具有相同类型、模板 ID 和实例 ID 时返回 true。</returns>
    public bool Matches(ItemKey hostKey)
    {
        return IsValid
            && hostKey.IsValid()
            && hostKey.ItemType == ItemType
            && hostKey.TemplateId == TemplateId
            && hostKey.Id == ItemId;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is GraftHostId other && Equals(other);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid
            ? $"{ItemType}:{TemplateId}:{ItemId}"
            : "Invalid";
    }

    /// <summary>
    /// 比较两个宿主身份是否相等。
    /// </summary>
    /// <param name="left">左侧身份。</param>
    /// <param name="right">右侧身份。</param>
    /// <returns>当两个身份指向同一个宿主物品时返回 true。</returns>
    public static bool operator ==(GraftHostId left, GraftHostId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 比较两个宿主身份是否不相等。
    /// </summary>
    /// <param name="left">左侧身份。</param>
    /// <param name="right">右侧身份。</param>
    /// <returns>当两个身份不指向同一个宿主物品时返回 true。</returns>
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
