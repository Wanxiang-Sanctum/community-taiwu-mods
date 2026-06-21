using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

public sealed class CreateOptions
{
    public string? NotificationMessage { get; set; }

    public short NotificationRecordType { get; set; } = GraftNotifications.DefaultNativeRecordType;

    public Func<IReadOnlyList<ItemDisplayData>, IReadOnlyList<ItemDisplayData>, ItemKey>? SelectCreatedHost { get; set; }

    public Action<GraftHostEventArgs>? OnHostEvent { get; set; }
}
