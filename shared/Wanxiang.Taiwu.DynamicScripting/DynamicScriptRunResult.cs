namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Represents the outcome of compiling and trying to invoke a dynamic script.
/// </summary>
public abstract class DynamicScriptRunResult
{
    /// <summary>
    /// Creates an invoked response with a JSON-encoded return value.
    /// </summary>
    /// <param name="returnValueJson">The JSON-encoded return value.</param>
    /// <returns>An invoked result.</returns>
    public static DynamicScriptRunResult InvokedWithReturnValue(string returnValueJson)
    {
        return new DynamicScriptInvokedResult(
            new DynamicScriptReturnValueOutcome(returnValueJson));
    }

    /// <summary>
    /// Creates a response for a script whose entry method was not invoked.
    /// </summary>
    /// <param name="reason">The reason invocation did not occur.</param>
    /// <param name="details">Optional diagnostics for the failure.</param>
    /// <returns>A not-invoked result.</returns>
    public static DynamicScriptRunResult NotInvoked(
        string reason,
        DynamicScriptNotInvokedDetails? details = null)
    {
        return new DynamicScriptNotInvokedResult(reason, details);
    }

    /// <summary>
    /// Creates an invoked response whose entry method threw or was canceled.
    /// </summary>
    /// <param name="message">The exception or cancellation message.</param>
    /// <returns>An invoked result.</returns>
    public static DynamicScriptRunResult InvokedWithException(string message)
    {
        return new DynamicScriptInvokedResult(
            new DynamicScriptExceptionOutcome(message));
    }

    private protected DynamicScriptRunResult()
    {
    }
}
