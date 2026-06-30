namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Represents a script whose entry method was invoked.
/// </summary>
/// <param name="outcome">The entry method outcome.</param>
public sealed class DynamicScriptInvokedResult(
    DynamicScriptInvocationOutcome outcome) : DynamicScriptRunResult
{
    /// <summary>
    /// Gets the entry method outcome.
    /// </summary>
    public DynamicScriptInvocationOutcome Outcome { get; } =
        outcome ?? throw new ArgumentNullException(nameof(outcome));
}
