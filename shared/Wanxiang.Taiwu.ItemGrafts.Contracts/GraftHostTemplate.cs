using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 表示请求创建真实嫁接宿主时使用的非堆叠物品模板。
/// </summary>
public sealed class GraftHostTemplate
{
    /// <summary>
    /// 初始化 <see cref="GraftHostTemplate"/> 类的新实例。
    /// </summary>
    /// <param name="itemType">太吾物品类型。</param>
    /// <param name="templateId">太吾物品模板 ID。</param>
    /// <exception cref="ArgumentException">模板不存在，或模板对应物品可堆叠。</exception>
    public GraftHostTemplate(sbyte itemType, short templateId)
    {
        GraftHostValidation.ValidateTemplate(itemType, templateId, nameof(templateId));

        ItemType = itemType;
        TemplateId = templateId;
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
    /// 判断给定物品 key 是否使用本宿主模板。
    /// </summary>
    /// <param name="hostKey">要比较的物品 key。</param>
    /// <returns>当物品 key 有效且与本宿主模板的物品类型和模板 ID 相同时返回 true。</returns>
    public bool Matches(ItemKey hostKey)
    {
        return hostKey.IsValid()
            && hostKey.HasTemplate
            && hostKey.ItemType == ItemType
            && hostKey.TemplateId == TemplateId;
    }
}
