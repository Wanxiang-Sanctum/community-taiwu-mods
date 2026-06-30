namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Dispatches a dynamic script entry invocation to a host-owned thread policy.
/// </summary>
public interface IDynamicScriptEntryDispatcher
{
    /// <summary>
    /// Invokes the supplied entry delegate according to the requested thread policy.
    /// </summary>
    /// <param name="invokeEntry">The delegate that calls the script entry method.</param>
    /// <param name="entryThread">The requested entry invocation thread.</param>
    /// <param name="cancellationToken">The cancellation token for the dispatch operation.</param>
    /// <returns>The raw return value produced by the script entry method.</returns>
    Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken);
}
