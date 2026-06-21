namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 携带请求创建真实嫁接宿主时使用的物品模板字段。
/// </summary>
/// <param name="itemType">太吾物品类型。</param>
/// <param name="templateId">太吾物品模板 ID。</param>
public sealed class GraftHostTemplate(sbyte itemType, short templateId)
{
    /// <summary>
    /// 获取太吾物品类型。
    /// </summary>
    public sbyte ItemType { get; } = itemType;

    /// <summary>
    /// 获取太吾物品模板 ID。
    /// </summary>
    public short TemplateId { get; } = templateId;
}
