namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Describes one dynamic script execution request.
/// </summary>
/// <param name="script">The complete C# compilation unit to compile and execute.</param>
/// <param name="entryThread">The host thread used to invoke the entry method.</param>
public sealed class DynamicScriptRunRequest(
    string script,
    DynamicScriptEntryThread entryThread = DynamicScriptEntryThread.Current)
{
    /// <summary>
    /// Gets the complete C# compilation unit to compile and execute.
    /// </summary>
    public string Script { get; } = script ?? throw new ArgumentNullException(nameof(script));

    /// <summary>
    /// Gets the host thread used to invoke the entry method.
    /// </summary>
    public DynamicScriptEntryThread EntryThread { get; } =
        ValidateEntryThread(entryThread);

    private static DynamicScriptEntryThread ValidateEntryThread(
        DynamicScriptEntryThread entryThread)
    {
        return entryThread is DynamicScriptEntryThread.Current
                or DynamicScriptEntryThread.MainThread
            ? entryThread
            : throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread.");
    }
}
