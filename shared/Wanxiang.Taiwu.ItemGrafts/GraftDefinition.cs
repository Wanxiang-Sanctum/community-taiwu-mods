namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class GraftDefinition(
    GraftAppearance appearance,
    GraftMenuMode menuMode,
    IReadOnlyList<GraftOperation> operations)
{
    public GraftAppearance Appearance { get; } =
        appearance ?? throw new ArgumentNullException(nameof(appearance));

    public GraftMenuMode MenuMode { get; } = Graft.ValidateMenuMode(menuMode, nameof(menuMode));

    public IReadOnlyList<GraftOperation> Operations { get; } = Graft.CopyOperations(operations);

    public bool HasOperations => Operations.Count > 0;
}
