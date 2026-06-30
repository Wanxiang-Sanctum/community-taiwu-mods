using System.Collections.ObjectModel;

namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Contains diagnostics for a script that could not be compiled or referenced.
/// </summary>
/// <param name="referenceDiagnostics">Reference discovery diagnostics.</param>
/// <param name="compilationDiagnostics">Compilation diagnostics.</param>
public sealed class DynamicScriptNotInvokedDetails(
    IReadOnlyList<string>? referenceDiagnostics,
    IReadOnlyList<string>? compilationDiagnostics)
{
    private static readonly IReadOnlyList<string> EmptyList =
        new ReadOnlyCollection<string>([]);

    /// <summary>
    /// Gets reference discovery diagnostics.
    /// </summary>
    public IReadOnlyList<string> ReferenceDiagnostics { get; } =
        NormalizeList(referenceDiagnostics);

    /// <summary>
    /// Gets compilation diagnostics.
    /// </summary>
    public IReadOnlyList<string> CompilationDiagnostics { get; } =
        NormalizeList(compilationDiagnostics);

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        return values is { Count: > 0 }
            ? new ReadOnlyCollection<string>([.. values])
            : EmptyList;
    }
}
