using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Wanxiang.Taiwu.DynamicScripting;

internal sealed class ScriptReferenceResolver
{
    private const string TrustedPlatformAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";

    private readonly List<string> _assemblyReferencePaths;

    public ScriptReferenceResolver(DynamicScriptReferenceOptions referenceOptions)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(referenceOptions);
#else
        if (referenceOptions is null)
        {
            throw new ArgumentNullException(nameof(referenceOptions));
        }
#endif

        _assemblyReferencePaths = [.. referenceOptions.AssemblyReferencePaths];
    }

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
        List<string> referenceDiagnostics = [];
        HashSet<string> referencePaths = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> referenceAssemblyIdentities = new(StringComparer.OrdinalIgnoreCase);

        AddTrustedPlatformAssemblyReferences(
            references,
            referencePaths,
            referenceAssemblyIdentities);

        AddExplicitAssemblyReferences(
            references,
            referencePaths,
            referenceAssemblyIdentities,
            referenceDiagnostics);

        bool hasRequiredReferences = TryAddRequiredAssemblyReference(
            requiredAssembly,
            requiredAssemblyDisplayName,
            references,
            referencePaths,
            referenceAssemblyIdentities,
            referenceDiagnostics);

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
            referenceDiagnostics);
    }

    public AssemblyResolutionScope CreateAssemblyResolutionScope()
    {
        return new AssemblyResolutionScope(_assemblyReferencePaths);
    }

    private void AddExplicitAssemblyReferences(
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities,
        List<string> referenceDiagnostics)
    {
        foreach (string referencePath in _assemblyReferencePaths)
        {
            if (!File.Exists(referencePath))
            {
                referenceDiagnostics.Add(
                    $"Script assembly reference path does not exist: '{referencePath}'.");
                continue;
            }

            if (!TryAddMetadataReference(
                    referencePath,
                    references,
                    referencePaths,
                    referenceAssemblyIdentities))
            {
                referenceDiagnostics.Add(
                    $"Script assembly reference path could not be loaded as metadata: '{referencePath}'.");
            }
        }
    }

    private bool TryAddRequiredAssemblyReference(
        Assembly assembly,
        string assemblyDisplayName,
        List<MetadataReference> references,
        HashSet<string> referencePaths,
        HashSet<string> referenceAssemblyIdentities,
        List<string> referenceDiagnostics)
    {
        if (!DynamicScriptAssemblyReferenceResolver.TryGetAssemblyLocation(assembly, out string? referencePath)
            && !DynamicScriptAssemblyReferenceResolver.TryFindAssemblyReferencePath(
                assembly.GetName(),
                _assemblyReferencePaths,
                out referencePath))
        {
            referenceDiagnostics.Add(
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

        referenceDiagnostics.Add(
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
        if (DynamicScriptAssemblyReferenceResolver.TryGetAssemblyLocation(assembly, out string? referencePath))
        {
            _ = TryAddMetadataReference(
                referencePath,
                references,
                referencePaths,
                referenceAssemblyIdentities);
        }
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
    IReadOnlyList<string> referenceDiagnostics)
{
    public bool HasRequiredReferences { get; } = hasRequiredReferences;

    public IReadOnlyList<MetadataReference> References { get; } = references;

    public IReadOnlyList<string> ReferenceDiagnostics { get; } = referenceDiagnostics;
}
