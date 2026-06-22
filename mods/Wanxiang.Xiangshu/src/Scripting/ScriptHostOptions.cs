namespace Wanxiang.Xiangshu.Scripting;

public sealed class ScriptHostOptions
{
    public ScriptHostOptions(
        string targetSide,
        IEnumerable<string>? referenceDirectories = null,
        IEnumerable<string>? assemblyReferencePaths = null)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSide);
#else
        if (string.IsNullOrWhiteSpace(targetSide))
        {
            throw new ArgumentException("Target side is required.", nameof(targetSide));
        }
#endif

        TargetSide = targetSide;
        ReferenceDirectories = NormalizeReferenceDirectories(referenceDirectories);
        AssemblyReferencePaths = NormalizeAssemblyReferencePaths(assemblyReferencePaths);
    }

    public string TargetSide { get; }

    public IReadOnlyList<string> ReferenceDirectories { get; }

    public IReadOnlyList<string> AssemblyReferencePaths { get; }

    private static List<string> NormalizeReferenceDirectories(
        IEnumerable<string>? referenceDirectories)
    {
        if (referenceDirectories is null)
        {
            return [];
        }

        List<string> normalizedDirectories = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string referenceDirectory in referenceDirectories)
        {
            if (string.IsNullOrWhiteSpace(referenceDirectory))
            {
                continue;
            }

            string normalizedDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(referenceDirectory.Trim()));
            if (seen.Add(normalizedDirectory))
            {
                normalizedDirectories.Add(normalizedDirectory);
            }
        }

        return normalizedDirectories;
    }

    private static List<string> NormalizeAssemblyReferencePaths(
        IEnumerable<string>? assemblyReferencePaths)
    {
        if (assemblyReferencePaths is null)
        {
            return [];
        }

        List<string> normalizedPaths = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string referencePath in assemblyReferencePaths)
        {
            if (string.IsNullOrWhiteSpace(referencePath))
            {
                continue;
            }

            string normalizedPath = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(referencePath.Trim()));
            if (seen.Add(normalizedPath))
            {
                normalizedPaths.Add(normalizedPath);
            }
        }

        return normalizedPaths;
    }
}
