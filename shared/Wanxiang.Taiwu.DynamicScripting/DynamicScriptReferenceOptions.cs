namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Describes explicit assembly reference inputs for dynamic script compilation.
/// </summary>
/// <param name="assemblyReferencePaths">Specific assembly files to add as compilation and runtime references.</param>
/// <exception cref="ArgumentException"><paramref name="assemblyReferencePaths"/> contains a null or whitespace path.</exception>
public sealed class DynamicScriptReferenceOptions(
    IEnumerable<string>? assemblyReferencePaths = null)
{
    /// <summary>
    /// Gets specific assembly files to add as compilation and runtime references.
    /// </summary>
    public IReadOnlyList<string> AssemblyReferencePaths { get; } =
        NormalizePaths(
            assemblyReferencePaths,
            nameof(assemblyReferencePaths));

    private static List<string> NormalizePaths(
        IEnumerable<string>? paths,
        string parameterName)
    {
        if (paths is null)
        {
            return [];
        }

        List<string> normalizedPaths = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string? path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Assembly reference paths cannot contain null or whitespace entries.",
                    parameterName);
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
