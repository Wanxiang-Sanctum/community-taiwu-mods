using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 表示后端观察服务报告给前端嫁接会话的宿主事实事件。
/// </summary>
public abstract class GraftHostEventArgs : EventArgs
{
    private protected GraftHostEventArgs(ItemKey hostKey)
    {
        if (!GraftHostId.TryCreate(hostKey, out GraftHostId hostId))
        {
            throw new ArgumentException("Host item key must be valid.", nameof(hostKey));
        }

        HostKey = hostKey;
        HostId = hostId;
    }

    /// <summary>
    /// 获取事件种类判别值。
    /// </summary>
    public abstract GraftHostEventKind Kind { get; }

    /// <summary>
    /// 获取宿主当前可传给太吾游戏 API 的完整物品 key。
    /// </summary>
    public ItemKey HostKey { get; }

    /// <summary>
    /// 获取不包含可变物品状态的稳定宿主身份。
    /// </summary>
    public GraftHostId HostId { get; }

    /// <summary>
    /// 创建表示真实宿主物品已被移除的事件。
    /// </summary>
    /// <param name="hostKey">移除前观察到的宿主物品 key。</param>
    /// <returns>宿主移除事件。</returns>
    public static GraftHostEventArgs Removed(ItemKey hostKey)
    {
        return new GraftHostRemovedEventArgs(hostKey);
    }

    /// <summary>
    /// 创建表示真实宿主物品 owner 发生变化的事件。
    /// </summary>
    /// <param name="hostKey">当前宿主物品 key。</param>
    /// <param name="fromOwner">变化前的物品 owner；变化前无 owner 时为 null。</param>
    /// <param name="toOwner">变化后的物品 owner；变化后无 owner 时为 null。</param>
    /// <returns>宿主 owner 变化事件。</returns>
    public static GraftHostEventArgs OwnerChanged(
        ItemKey hostKey,
        GraftHostOwnerKey? fromOwner,
        GraftHostOwnerKey? toOwner)
    {
        return new GraftHostOwnerChangedEventArgs(
            hostKey,
            fromOwner,
            toOwner);
    }

    /// <summary>
    /// 创建表示真实宿主物品数据已变化的事件。
    /// </summary>
    /// <param name="hostKey">当前宿主物品 key。</param>
    /// <returns>宿主数据变化事件。</returns>
    public static GraftHostEventArgs DataChanged(ItemKey hostKey)
    {
        return new GraftHostDataChangedEventArgs(hostKey);
    }
}

/// <summary>
/// 表示真实宿主物品已从游戏状态中移除。
/// </summary>
public sealed class GraftHostRemovedEventArgs : GraftHostEventArgs
{
    internal GraftHostRemovedEventArgs(ItemKey hostKey)
        : base(hostKey)
    {
    }

    /// <inheritdoc />
    public override GraftHostEventKind Kind => GraftHostEventKind.Removed;
}

/// <summary>
/// 表示真实宿主物品的游戏 owner 已变化。
/// </summary>
public sealed class GraftHostOwnerChangedEventArgs : GraftHostEventArgs
{
    internal GraftHostOwnerChangedEventArgs(
        ItemKey hostKey,
        GraftHostOwnerKey? fromOwner,
        GraftHostOwnerKey? toOwner)
        : base(hostKey)
    {
        FromOwner = fromOwner;
        ToOwner = toOwner;

        if (FromOwner is null && ToOwner is null)
        {
            throw new ArgumentException("Host owner change event must have at least one endpoint.");
        }
    }

    /// <inheritdoc />
    public override GraftHostEventKind Kind => GraftHostEventKind.OwnerChanged;

    /// <summary>
    /// 获取变化前的物品 owner；变化前无 owner 时为 null。
    /// </summary>
    public GraftHostOwnerKey? FromOwner { get; }

    /// <summary>
    /// 获取变化后的物品 owner；变化后无 owner 时为 null。
    /// </summary>
    public GraftHostOwnerKey? ToOwner { get; }
}

/// <summary>
/// 表示真实宿主物品数据已变化，调用方应按需重新查询。
/// </summary>
public sealed class GraftHostDataChangedEventArgs : GraftHostEventArgs
{
    internal GraftHostDataChangedEventArgs(ItemKey hostKey)
        : base(hostKey)
    {
    }

    /// <inheritdoc />
    public override GraftHostEventKind Kind => GraftHostEventKind.DataChanged;
}
