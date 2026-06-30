namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Represents a script entry method that threw or was canceled.
/// </summary>
/// <param name="message">The exception or cancellation message.</param>
public sealed class DynamicScriptExceptionOutcome(
    string message) : DynamicScriptInvocationOutcome
{
    /// <summary>
    /// Gets the exception or cancellation message.
    /// </summary>
    public string Message { get; } =
        message ?? throw new ArgumentNullException(nameof(message));
}
