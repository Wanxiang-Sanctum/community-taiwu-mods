namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Selects the host thread used to invoke the compiled script entry method.
/// </summary>
public enum DynamicScriptEntryThread
{
    /// <summary>
    /// Invoke the entry method on the current caller thread.
    /// </summary>
    Current = 0,

    /// <summary>
    /// Invoke the entry method on the host's main thread.
    /// </summary>
    MainThread = 1,
}
