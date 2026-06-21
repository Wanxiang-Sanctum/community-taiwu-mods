using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 后端观察器向前端嫁接会话报告宿主事实事件的基类。
/// </summary>
public abstract class GraftHostEventArgs : EventArgs
{
    private protected GraftHostEventArgs(ItemKey hostKey)
    {
        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
        HostId = new GraftHostId(HostKey);
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
    /// 创建表示真实宿主物品角色行囊位置发生变化的事件。
    /// </summary>
    /// <param name="hostKey">当前宿主物品 key。</param>
    /// <param name="fromCharacterId">变化前的角色行囊端点；不在角色行囊中时为 null。</param>
    /// <param name="toCharacterId">变化后的角色行囊端点；不在角色行囊中时为 null。</param>
    /// <returns>宿主位置变化事件。</returns>
    public static GraftHostEventArgs LocationChanged(
        ItemKey hostKey,
        int? fromCharacterId,
        int? toCharacterId)
    {
        return new GraftHostLocationChangedEventArgs(
            hostKey,
            fromCharacterId,
            toCharacterId);
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

    internal static int ValidateCharacterId(int characterId, string parameterName)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                characterId,
                "Character id must be valid.");
        }

        return characterId;
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
/// 表示真实宿主物品进入、离开或转移于角色行囊之间。
/// </summary>
public sealed class GraftHostLocationChangedEventArgs : GraftHostEventArgs
{
    internal GraftHostLocationChangedEventArgs(
        ItemKey hostKey,
        int? fromCharacterId,
        int? toCharacterId)
        : base(hostKey)
    {
        FromCharacterId = ValidateCharacterId(fromCharacterId, nameof(fromCharacterId));
        ToCharacterId = ValidateCharacterId(toCharacterId, nameof(toCharacterId));

        if (FromCharacterId is null && ToCharacterId is null)
        {
            throw new ArgumentException("Host location event must have at least one character endpoint.");
        }
    }

    /// <inheritdoc />
    public override GraftHostEventKind Kind => GraftHostEventKind.LocationChanged;

    /// <summary>
    /// 获取变化前的角色行囊端点；变化前不在角色行囊中时为 null。
    /// </summary>
    public int? FromCharacterId { get; }

    /// <summary>
    /// 获取变化后的角色行囊端点；变化后不在角色行囊中时为 null。
    /// </summary>
    public int? ToCharacterId { get; }

    private static int? ValidateCharacterId(int? characterId, string parameterName)
    {
        return characterId.HasValue
            ? GraftHostEventArgs.ValidateCharacterId(characterId.Value, parameterName)
            : null;
    }
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
