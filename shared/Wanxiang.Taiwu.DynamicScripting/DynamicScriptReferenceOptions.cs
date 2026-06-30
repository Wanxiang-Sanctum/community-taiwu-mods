namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Describes reference discovery inputs for dynamic script compilation.
/// </summary>
/// <param name="referenceDirectories">Directories whose top-level DLLs are available as compilation and runtime references.</param>
/// <param name="assemblyReferencePaths">Specific assembly files to add as compilation references.</param>
public sealed class DynamicScriptReferenceOptions(
    IEnumerable<string>? referenceDirectories = null,
    IEnumerable<string>? assemblyReferencePaths = null)
{
    /// <summary>
    /// Gets directories whose top-level DLLs are available as compilation and runtime references.
    /// </summary>
    public IReadOnlyList<string> ReferenceDirectories { get; } =
        NormalizePaths(referenceDirectories);

    /// <summary>
    /// Gets specific assembly files to add as compilation references.
    /// </summary>
    public IReadOnlyList<string> AssemblyReferencePaths { get; } =
        NormalizePaths(assemblyReferencePaths);

    private static List<string> NormalizePaths(IEnumerable<string>? paths)
    {
        if (paths is null)
        {
            return [];
        }

        List<string> normalizedPaths = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            string normalizedPath = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(path.Trim()));
            if (seen.Add(normalizedPath))
            {
                normalizedPaths.Add(normalizedPath);
            }
        }

        return normalizedPaths;
    }
}
