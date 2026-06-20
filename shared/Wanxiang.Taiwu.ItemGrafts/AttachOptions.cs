namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class AttachOptions
{
    public string? NotificationMessage { get; set; }

    public short NotificationRecordType { get; set; } = GraftNotifications.DefaultNativeRecordType;
}
