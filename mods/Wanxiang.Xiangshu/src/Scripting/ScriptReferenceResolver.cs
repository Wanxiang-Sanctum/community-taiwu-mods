using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Wanxiang.Xiangshu.Scripting;

internal sealed class ScriptReferenceResolver(IEnumerable<string>? referenceDirectories)
{
    private const string TrustedPlatformAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";

    private readonly List<string> _referenceDirectories = NormalizeReferenceDirectories(referenceDirectories);

    public CompilationReferences CollectReferences(
        Assembly requiredAssembly,
        string requiredAssemblyDisplayName)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(requiredAssembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredAssemblyDisplayName);
#else
        if (requiredAssembly is null)
        {
            throw new ArgumentNullException(nameof(requiredAssembly));
        }

        if (string.IsNullOrWhiteSpace(requiredAssemblyDisplayName))
        {
            throw new ArgumentException("Required assembly display name is required.", nameof(requiredAssemblyDisplayName));
        }
#endif

        List<MetadataReference> references = [];
        List<string> issues = [];
        HashSet<string> referencePaths = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> referenceAssemblyIdentities = new(StringComparer.OrdinalIgnoreCase);

        AddTrustedPlatformAssemblyReferences(
            references,
            referencePaths,
            referenceAssemblyIdentities);

        AddReferenceDirectoryReferences(
            references,
            referencePaths,
            referenceAssemblyIdentities,
            issues);

        bool hasRequiredReferences = TryAddRequiredAssemblyReference(
            requiredAssembly,
            requiredAssemblyDisplayName,
            references,
            referencePaths,
            referenceAssemblyIdentities,
            issues);

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryAddLoadedAssemblyReference(
                assembly,
                references,
                referencePaths,
                referenceAssemblyIdentities);
        }

        return new CompilationReferences(
            hasRequiredReferences,
            references,
            issues);
    }

    public AssemblyResolutionScope CreateAssemblyResolutionScope()
    {
        return new AssemblyResolutionScope(_referenceDirectories);
    }

    private bool TryAddRequiredAssemblyReference(
        Assembly assembly,
        string assemblyDisplayName,
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities,
        List<string> issues)
    {
        if (!TryGetAssemblyReferencePath(assembly, out string? referencePath)
            && !TryFindReferenceDirectoryAssembly(assembly.GetName(), out referencePath))
        {
            issues.Add(
                $"The assembly that defines {assemblyDisplayName} is not available "
                + "as a metadata reference for dynamic compilation.");
            return false;
        }

        if (TryAddMetadataReference(
            referencePath,
            references,
            referencePaths,
            referenceAssemblyIdentities))
        {
            return true;
        }

        issues.Add(
            $"The assembly that defines {assemblyDisplayName} could not be loaded "
            + $"as a metadata reference from '{referencePath}'.");
        return false;
    }

    private static void TryAddLoadedAssemblyReference(
        Assembly assembly,
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities)
    {
        if (TryGetAssemblyReferencePath(assembly, out string? referencePath))
        {
            _ = TryAddMetadataReference(
                referencePath,
                references,
                referencePaths,
                referenceAssemblyIdentities);
        }
    }

    private static bool TryGetAssemblyReferencePath(
        Assembly assembly,
        [NotNullWhen(true)] out string? referencePath)
    {
        referencePath = null;

        if (assembly.IsDynamic)
        {
            return false;
        }

        try
        {
            referencePath = assembly.Location;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(referencePath) && File.Exists(referencePath);
    }

    private static void AddTrustedPlatformAssemblyReferences(
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities)
    {
        if (AppContext.GetData(TrustedPlatformAssembliesKey) is not string trustedPlatformAssemblies)
        {
            return;
        }

        foreach (string referencePath in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(referencePath))
            {
                _ = TryAddMetadataReference(
                    referencePath,
                    references,
                    referencePaths,
                    referenceAssemblyIdentities);
            }
        }
    }

    private void AddReferenceDirectoryReferences(
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities,
        List<string> issues)
    {
        foreach (string referenceDirectory in _referenceDirectories)
        {
            if (!Directory.Exists(referenceDirectory))
            {
                issues.Add(
                    $"Script reference directory does not exist: '{referenceDirectory}'.");
                continue;
            }

            foreach (string referencePath in EnumerateReferenceDirectoryAssemblies(referenceDirectory))
            {
                _ = TryAddMetadataReference(
                    referencePath,
                    references,
                    referencePaths,
                    referenceAssemblyIdentities);
            }
        }
    }

    private bool TryFindReferenceDirectoryAssembly(
        AssemblyName assemblyName,
        [NotNullWhen(true)] out string? referencePath)
    {
        referencePath = null;
        if (string.IsNullOrWhiteSpace(assemblyName.Name))
        {
            return false;
        }

        string fileName = assemblyName.Name + ".dll";
        foreach (string referenceDirectory in _referenceDirectories)
        {
            string candidatePath = Path.Combine(referenceDirectory, fileName);
            if (File.Exists(candidatePath))
            {
                referencePath = candidatePath;
                return true;
            }
        }

        return false;
    }

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

    private static string[] EnumerateReferenceDirectoryAssemblies(string referenceDirectory)
    {
        return
        [
            .. Directory.EnumerateFiles(referenceDirectory, "*.dll", SearchOption.TopDirectoryOnly),
        ];
    }

    private static bool TryAddMetadataReference(
        string referencePath,
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities)
    {
        string normalizedPath = Path.GetFullPath(referencePath);
        if (!referencePaths.Add(normalizedPath))
        {
            return true;
        }

        string? assemblyIdentity = null;
        try
        {
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(normalizedPath);
            assemblyIdentity = assemblyName.FullName;
            if (!referenceAssemblyIdentities.Add(assemblyIdentity))
            {
                _ = referencePaths.Remove(normalizedPath);
                return true;
            }

            references.Add(MetadataReference.CreateFromImage(
                File.ReadAllBytes(normalizedPath),
                filePath: normalizedPath));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException
            or BadImageFormatException
            or IOException
            or UnauthorizedAccessException)
        {
            _ = referencePaths.Remove(normalizedPath);
            if (assemblyIdentity is not null)
            {
                _ = referenceAssemblyIdentities.Remove(assemblyIdentity);
            }

            return false;
        }
    }

}

internal sealed class CompilationReferences(
    bool hasRequiredReferences,
    IReadOnlyList<MetadataReference> references,
    IReadOnlyList<string> issues)
{
    public bool HasRequiredReferences { get; } = hasRequiredReferences;

    public IReadOnlyList<MetadataReference> References { get; } = references;

    public IReadOnlyList<string> Issues { get; } = issues;
}
