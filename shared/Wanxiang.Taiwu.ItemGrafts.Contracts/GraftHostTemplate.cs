namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

public sealed class GraftHostTemplate(sbyte itemType, short templateId)
{
    public sbyte ItemType { get; } = itemType;

    public short TemplateId { get; } = templateId;
}
