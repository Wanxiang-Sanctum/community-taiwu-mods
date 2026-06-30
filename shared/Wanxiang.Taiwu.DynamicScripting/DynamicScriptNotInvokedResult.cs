namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Represents a script that did not reach its entry method.
/// </summary>
/// <param name="reason">The reason invocation did not occur.</param>
/// <param name="details">Optional diagnostics for the failure.</param>
public sealed class DynamicScriptNotInvokedResult(
    string reason,
    DynamicScriptNotInvokedDetails? details = null) : DynamicScriptRunResult
{
    /// <summary>
    /// Gets the reason invocation did not occur.
    /// </summary>
    public string Reason { get; } =
        reason ?? throw new ArgumentNullException(nameof(reason));

    /// <summary>
    /// Gets optional diagnostics for the failure.
    /// </summary>
    public DynamicScriptNotInvokedDetails? Details { get; } = details;
}
