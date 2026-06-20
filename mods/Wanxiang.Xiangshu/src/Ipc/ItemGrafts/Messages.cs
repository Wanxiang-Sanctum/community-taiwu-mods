using MessagePack;

namespace Wanxiang.Xiangshu.Ipc.ItemGrafts;

[MessagePackObject]
public sealed class HostRemovedRequest(ulong hostKey)
{
    [Key(0)]
    public ulong HostKey { get; } = hostKey;
}

[MessagePackObject]
public sealed class InventoryTransferRequest(
    ulong hostKey,
    int fromCharacterId,
    int toCharacterId,
    int amount)
{
    public const int NoCharacterInventoryId = -1;

    [Key(0)]
    public ulong HostKey { get; } = hostKey;

    [Key(1)]
    public int FromCharacterId { get; } = fromCharacterId;

    [Key(2)]
    public int ToCharacterId { get; } = toCharacterId;

    [Key(3)]
    public int Amount { get; } = amount;
}

[MessagePackObject]
public sealed class TaiwuInventorySnapshotChangedRequest;

[MessagePackObject]
public sealed class RegisterHostRequest(ulong hostKey)
{
    [Key(0)]
    public ulong HostKey { get; } = hostKey;
}

[MessagePackObject]
public sealed class UnregisterHostRequest;
