namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class HostTemplate(
    sbyte itemType,
    short templateId)
{
    public sbyte ItemType { get; } = itemType;

    public short TemplateId { get; } = templateId >= 0
        ? templateId
        : throw new ArgumentOutOfRangeException(nameof(templateId), templateId, "Host item template id must be valid.");
}
