namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 表示游戏物品真实 owner 的跨端传输键；无 owner 由可空 owner key 的 null 表达。
/// </summary>
public readonly struct GraftHostOwnerKey : IEquatable<GraftHostOwnerKey>
{
    private const sbyte CharacterInventoryOwnerType = 3;

    /// <summary>
    /// 创建游戏物品真实 owner 的跨端传输键。
    /// </summary>
    /// <param name="ownerType">游戏后端 <c>ItemOwnerType</c> 的数值；必须指向真实 owner。</param>
    /// <param name="ownerId">该 owner type 下的 owner id。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownerType"/> 未指向真实 owner。</exception>
    public GraftHostOwnerKey(sbyte ownerType, int ownerId)
    {
        if (ownerType <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerType),
                ownerType,
                "Owner type must point to an item owner.");
        }

        OwnerType = ownerType;
        OwnerId = ownerId;
    }

    /// <summary>
    /// 获取游戏后端 <c>ItemOwnerType</c> 的数值。
    /// </summary>
    public sbyte OwnerType { get; }

    /// <summary>
    /// 获取该 owner type 下的 owner id。
    /// </summary>
    public int OwnerId { get; }

    /// <summary>
    /// 创建角色行囊 owner key。
    /// </summary>
    /// <param name="characterId">角色 id。</param>
    /// <returns>角色行囊 owner key。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterId"/> 小于 0。</exception>
    public static GraftHostOwnerKey CharacterInventory(int characterId)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Character id must be valid.");
        }

        return new GraftHostOwnerKey(CharacterInventoryOwnerType, characterId);
    }

    /// <summary>
    /// 判断当前 owner key 是否指向指定角色的行囊。
    /// </summary>
    /// <param name="characterId">角色 id。</param>
    /// <returns>当前 owner key 指向指定角色行囊时返回 true。</returns>
    public bool IsCharacterInventory(int characterId)
    {
        return characterId >= 0
            && OwnerType == CharacterInventoryOwnerType
            && OwnerId == characterId;
    }

    /// <inheritdoc />
    public bool Equals(GraftHostOwnerKey other)
    {
        return OwnerType == other.OwnerType && OwnerId == other.OwnerId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is GraftHostOwnerKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (OwnerType * 397) ^ OwnerId;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{{{OwnerType}, {OwnerId}}}";
    }

    /// <summary>
    /// 判断两个 owner key 是否相等。
    /// </summary>
    /// <param name="left">左侧 owner key。</param>
    /// <param name="right">右侧 owner key。</param>
    /// <returns>两个 owner key 相等时返回 true。</returns>
    public static bool operator ==(GraftHostOwnerKey left, GraftHostOwnerKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 判断两个 owner key 是否不相等。
    /// </summary>
    /// <param name="left">左侧 owner key。</param>
    /// <param name="right">右侧 owner key。</param>
    /// <returns>两个 owner key 不相等时返回 true。</returns>
    public static bool operator !=(GraftHostOwnerKey left, GraftHostOwnerKey right)
    {
        return !left.Equals(right);
    }
}
