using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class Graft(
    ItemKey hostKey,
    GraftAppearance appearance,
    GraftMenuMode menuMode,
    IReadOnlyList<GraftOperation> operations)
{
    public ItemKey HostKey { get; } = ValidateHostKey(hostKey, nameof(hostKey));

    public GraftAppearance Appearance { get; } =
        appearance ?? throw new ArgumentNullException(nameof(appearance));

    public GraftMenuMode MenuMode { get; } = ValidateMenuMode(menuMode, nameof(menuMode));

    public IReadOnlyList<GraftOperation> Operations { get; } = CopyOperations(operations);

    public bool HasOperations => Operations.Count > 0;

    public Graft WithAppearance(GraftAppearance appearance)
    {
        return new Graft(HostKey, appearance, MenuMode, Operations);
    }

    public Graft WithOperations(
        GraftMenuMode menuMode,
        IReadOnlyList<GraftOperation> operations)
    {
        return new Graft(HostKey, Appearance, menuMode, operations);
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

    internal static ItemKey ValidateHostKey(ItemKey hostKey, string parameterName)
    {
        if (!hostKey.IsValid())
        {
            throw new ArgumentException("Host item key must be valid.", parameterName);
        }

        if (!hostKey.HasTemplate)
        {
            throw new ArgumentException("Host item key must have a template.", parameterName);
        }

        if (!ItemTemplateHelper.CheckTemplateValid(hostKey.ItemType, hostKey.TemplateId))
        {
            throw new ArgumentException("Host item template must be valid.", parameterName);
        }

        if (ItemTemplateHelper.IsStackable(hostKey.ItemType, hostKey.TemplateId))
        {
            throw new ArgumentException("Host item must not be stackable.", parameterName);
        }

        return hostKey;
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
