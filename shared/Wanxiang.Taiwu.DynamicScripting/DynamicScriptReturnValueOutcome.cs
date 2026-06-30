namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Represents a script entry method that returned successfully.
/// </summary>
/// <param name="returnValueJson">The JSON-encoded return value.</param>
public sealed class DynamicScriptReturnValueOutcome(
    string returnValueJson) : DynamicScriptInvocationOutcome
{
    /// <summary>
    /// Gets the JSON-encoded return value.
    /// </summary>
    public string ReturnValueJson { get; } =
        returnValueJson ?? throw new ArgumentNullException(nameof(returnValueJson));
}
