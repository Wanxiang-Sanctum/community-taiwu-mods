using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

public sealed class AttachOptions
{
    public string? NotificationMessage { get; set; }

    public short NotificationRecordType { get; set; } = GraftNotifications.DefaultNativeRecordType;

    public Action<GraftHostEventArgs>? OnHostEvent { get; set; }
}
