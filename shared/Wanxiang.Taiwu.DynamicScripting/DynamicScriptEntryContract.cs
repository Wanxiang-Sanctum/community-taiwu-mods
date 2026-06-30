namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Describes the script entrypoint contract owned by a mod-specific adapter.
/// </summary>
public sealed class DynamicScriptEntryContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicScriptEntryContract"/> class.
    /// </summary>
    /// <param name="entryTypeSimpleName">The required simple name of the public static script entry type.</param>
    /// <param name="scriptGlobalsType">The exact globals type required as the sole entry method parameter.</param>
    /// <param name="scriptGlobalsDisplayName">The display name shown in diagnostics for the globals type.</param>
    /// <param name="scriptAssemblyNamePrefix">The prefix used for generated dynamic script assembly names.</param>
    /// <param name="asyncEntryMethodName">The accepted async entry method name.</param>
    /// <param name="syncEntryMethodName">The accepted sync entry method name.</param>
    public DynamicScriptEntryContract(
        string entryTypeSimpleName,
        Type scriptGlobalsType,
        string scriptGlobalsDisplayName,
        string scriptAssemblyNamePrefix,
        string asyncEntryMethodName = "ExecuteAsync",
        string syncEntryMethodName = "Execute")
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(entryTypeSimpleName);
        ArgumentNullException.ThrowIfNull(scriptGlobalsType);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptGlobalsDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptAssemblyNamePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(asyncEntryMethodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(syncEntryMethodName);
#else
        if (string.IsNullOrWhiteSpace(entryTypeSimpleName))
        {
            throw new ArgumentException("Entry type simple name is required.", nameof(entryTypeSimpleName));
        }

        if (scriptGlobalsType is null)
        {
            throw new ArgumentNullException(nameof(scriptGlobalsType));
        }

        if (string.IsNullOrWhiteSpace(scriptGlobalsDisplayName))
        {
            throw new ArgumentException("Script globals display name is required.", nameof(scriptGlobalsDisplayName));
        }

        if (string.IsNullOrWhiteSpace(scriptAssemblyNamePrefix))
        {
            throw new ArgumentException("Script assembly name prefix is required.", nameof(scriptAssemblyNamePrefix));
        }

        if (string.IsNullOrWhiteSpace(asyncEntryMethodName))
        {
            throw new ArgumentException("Async entry method name is required.", nameof(asyncEntryMethodName));
        }

        if (string.IsNullOrWhiteSpace(syncEntryMethodName))
        {
            throw new ArgumentException("Sync entry method name is required.", nameof(syncEntryMethodName));
        }
#endif

        EntryTypeSimpleName = entryTypeSimpleName;
        ScriptGlobalsType = scriptGlobalsType;
        ScriptGlobalsDisplayName = scriptGlobalsDisplayName;
        ScriptAssemblyNamePrefix = scriptAssemblyNamePrefix;
        AsyncEntryMethodName = asyncEntryMethodName;
        SyncEntryMethodName = syncEntryMethodName;
    }

    /// <summary>
    /// Gets the required simple name of the public static script entry type.
    /// </summary>
    public string EntryTypeSimpleName { get; }

    /// <summary>
    /// Gets the exact globals type required as the sole entry method parameter.
    /// </summary>
    public Type ScriptGlobalsType { get; }

    /// <summary>
    /// Gets the display name shown in diagnostics for the globals type.
    /// </summary>
    public string ScriptGlobalsDisplayName { get; }

    /// <summary>
    /// Gets the prefix used for generated dynamic script assembly names.
    /// </summary>
    public string ScriptAssemblyNamePrefix { get; }

    /// <summary>
    /// Gets the accepted async entry method name.
    /// </summary>
    public string AsyncEntryMethodName { get; }

    /// <summary>
    /// Gets the accepted sync entry method name.
    /// </summary>
    public string SyncEntryMethodName { get; }
}
