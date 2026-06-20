using GameData.Domains.Item;
using GameData.Domains.Item.Display;

namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class CreateOptions
{
    public Func<IReadOnlyList<ItemDisplayData>, IReadOnlyList<ItemDisplayData>, ItemKey>? SelectHost { get; set; }

    public string? NotificationMessage { get; set; }

    public short NotificationRecordType { get; set; } = GraftNotifications.DefaultNativeRecordType;
}
