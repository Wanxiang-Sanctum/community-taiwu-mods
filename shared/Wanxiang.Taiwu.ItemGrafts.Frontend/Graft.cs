using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

public sealed class Graft
{
    internal Graft(
        ItemKey hostKey,
        GraftAppearance appearance,
        GraftMenuMode menuMode,
        IReadOnlyList<GraftOperation> operations)
    {
        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
        HostId = new GraftHostId(HostKey);
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        MenuMode = ValidateMenuMode(menuMode, nameof(menuMode));
        Operations = CopyOperations(operations);
    }

    public ItemKey HostKey { get; private set; }

    public GraftHostId HostId { get; }

    public GraftAppearance Appearance { get; }

    public GraftMenuMode MenuMode { get; }

    public IReadOnlyList<GraftOperation> Operations { get; }

    public bool HasOperations => Operations.Count > 0;

    internal void UpdateHostKey(ItemKey hostKey)
    {
        if (!HostId.Matches(hostKey))
        {
            throw new ArgumentException("Host key does not belong to this graft.", nameof(hostKey));
        }

        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
    }

    internal static GraftOperation[] CopyOperations(
        IReadOnlyList<GraftOperation> operations)
    {
        if (operations is null)
        {
            throw new ArgumentNullException(nameof(operations));
        }

        GraftOperation[] copy = [.. operations];

        if (Array.Exists(copy, static operation => operation is null))
        {
            throw new ArgumentException("Graft operations must not contain null.", nameof(operations));
        }

        return copy;
    }

    internal static GraftMenuMode ValidateMenuMode(
        GraftMenuMode menuMode,
        string parameterName)
    {
        return menuMode switch
        {
            GraftMenuMode.Append => menuMode,
            GraftMenuMode.Replace => menuMode,
            _ => throw new ArgumentOutOfRangeException(parameterName, menuMode, "Unsupported graft menu mode."),
        };
    }
}
