namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Describes the script entrypoint contract owned by a mod-specific adapter.
/// </summary>
public sealed class DynamicScriptEntryContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicScriptEntryContract"/> class.
    /// </summary>
    /// <param name="entryTypeFullName">The required full name of the public static script entry type.</param>
    /// <param name="scriptGlobalsType">The exact globals type required as the sole entry method parameter.</param>
    public DynamicScriptEntryContract(
        string entryTypeFullName,
        Type scriptGlobalsType)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(entryTypeFullName);
        ArgumentNullException.ThrowIfNull(scriptGlobalsType);
#else
        if (string.IsNullOrWhiteSpace(entryTypeFullName))
        {
            throw new ArgumentException("Entry type full name is required.", nameof(entryTypeFullName));
        }

        if (scriptGlobalsType is null)
        {
            throw new ArgumentNullException(nameof(scriptGlobalsType));
        }
#endif

        EntryTypeFullName = entryTypeFullName;
        ScriptGlobalsType = scriptGlobalsType;
    }

    /// <summary>
    /// Gets the required full name of the public static script entry type.
    /// </summary>
    public string EntryTypeFullName { get; }

    /// <summary>
    /// Gets the exact globals type required as the sole entry method parameter.
    /// </summary>
    public Type ScriptGlobalsType { get; }
}
