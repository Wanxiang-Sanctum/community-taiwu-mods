using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

public abstract class GraftHostEventArgs : EventArgs
{
    private protected GraftHostEventArgs(ItemKey hostKey)
    {
        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
        HostId = new GraftHostId(HostKey);
    }

    public abstract GraftHostEventKind Kind { get; }

    public ItemKey HostKey { get; }

    public GraftHostId HostId { get; }

    public static GraftHostEventArgs Removed(ItemKey hostKey)
    {
        return new GraftHostRemovedEventArgs(hostKey);
    }

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

public sealed class GraftHostRemovedEventArgs : GraftHostEventArgs
{
    internal GraftHostRemovedEventArgs(ItemKey hostKey)
        : base(hostKey)
    {
    }

    public override GraftHostEventKind Kind => GraftHostEventKind.Removed;
}

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

    public override GraftHostEventKind Kind => GraftHostEventKind.LocationChanged;

    public int? FromCharacterId { get; }

    public int? ToCharacterId { get; }

    private static int? ValidateCharacterId(int? characterId, string parameterName)
    {
        return characterId.HasValue
            ? GraftHostEventArgs.ValidateCharacterId(characterId.Value, parameterName)
            : null;
    }
}

public sealed class GraftHostDataChangedEventArgs : GraftHostEventArgs
{
    internal GraftHostDataChangedEventArgs(ItemKey hostKey)
        : base(hostKey)
    {
    }

    public override GraftHostEventKind Kind => GraftHostEventKind.DataChanged;
}
