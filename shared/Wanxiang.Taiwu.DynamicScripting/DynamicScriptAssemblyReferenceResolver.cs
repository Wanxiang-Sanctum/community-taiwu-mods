using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// Resolves host-declared assemblies to metadata reference DLL paths.
/// </summary>
public static class DynamicScriptAssemblyReferenceResolver
{
    /// <summary>
    /// Resolves the assembly that defines the supplied marker type to a metadata reference path.
    /// </summary>
    /// <param name="markerType">A type defined by the assembly to reference.</param>
    /// <param name="searchDirectories">Explicit directories to search when the loaded assembly has no usable location.</param>
    /// <returns>The resolved assembly reference path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="markerType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The assembly cannot be resolved to a metadata reference path.</exception>
    public static string ResolveRequiredAssemblyReferencePath(
        Type markerType,
        IEnumerable<string>? searchDirectories = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(markerType);
#else
        if (markerType is null)
        {
            throw new ArgumentNullException(nameof(markerType));
        }
#endif

        return ResolveRequiredAssemblyReferencePath(markerType.Assembly, searchDirectories);
    }

    /// <summary>
    /// Resolves the supplied assembly to a metadata reference path.
    /// </summary>
    /// <param name="assembly">The assembly to reference.</param>
    /// <param name="searchDirectories">Explicit directories to search when the loaded assembly has no usable location.</param>
    /// <returns>The resolved assembly reference path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The assembly cannot be resolved to a metadata reference path.</exception>
    public static string ResolveRequiredAssemblyReferencePath(
        Assembly assembly,
        IEnumerable<string>? searchDirectories = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assembly);
#else
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }
#endif

        if (TryResolveAssemblyReferencePath(
            assembly,
            searchDirectories,
            out string? referencePath))
        {
            return referencePath;
        }

        throw new InvalidOperationException(
            "Could not resolve a metadata reference path for assembly "
            + $"'{assembly.GetName().FullName}'. Pass an explicit DLL path through "
            + "DynamicScriptReferenceOptions, or provide a search directory that contains "
            + "the matching assembly.");
    }

    private static bool TryResolveAssemblyReferencePath(
        Assembly assembly,
        IEnumerable<string>? searchDirectories,
        [NotNullWhen(true)] out string? referencePath)
    {
        return TryGetAssemblyLocation(assembly, out referencePath)
            || TryFindAssemblyInSearchDirectories(
                assembly.GetName(),
                NormalizeSearchDirectories(searchDirectories),
                out referencePath);
    }

    internal static bool TryGetAssemblyLocation(
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
        }

        return !string.IsNullOrWhiteSpace(referencePath)
            && File.Exists(referencePath);
    }

    internal static bool TryFindAssemblyReferencePath(
        AssemblyName assemblyName,
        IEnumerable<string> referencePaths,
        [NotNullWhen(true)] out string? referencePath)
    {
        referencePath = null;
        foreach (string candidatePath in referencePaths)
        {
            if (File.Exists(candidatePath) && AssemblyIdentityMatches(candidatePath, assemblyName))
            {
                referencePath = candidatePath;
                return true;
            }
        }

        return false;
    }

    internal static bool AssemblyIdentityMatches(string path, AssemblyName expectedName)
    {
        try
        {
            AssemblyName candidateName = AssemblyName.GetAssemblyName(path);
            return string.Equals(
                candidateName.FullName,
                expectedName.FullName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException
            or BadImageFormatException
            or IOException
            or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryFindAssemblyInSearchDirectories(
        AssemblyName assemblyName,
        IReadOnlyList<string> searchDirectories,
        [NotNullWhen(true)] out string? referencePath)
    {
        referencePath = null;
        if (string.IsNullOrWhiteSpace(assemblyName.Name))
        {
            return false;
        }

        string fileName = assemblyName.Name + ".dll";
        foreach (string searchDirectory in searchDirectories)
        {
            string candidatePath = Path.Combine(searchDirectory, fileName);
            if (File.Exists(candidatePath) && AssemblyIdentityMatches(candidatePath, assemblyName))
            {
                referencePath = candidatePath;
                return true;
            }
        }

        return false;
    }

    private static string[] NormalizeSearchDirectories(IEnumerable<string>? searchDirectories)
    {
        if (searchDirectories is null)
        {
            return [];
        }

        List<string> normalizedDirectories = [];
        foreach (string searchDirectory in searchDirectories)
        {
            if (string.IsNullOrWhiteSpace(searchDirectory))
            {
                continue;
            }

            string normalizedDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(searchDirectory.Trim()));
            if (!normalizedDirectories.Contains(normalizedDirectory, StringComparer.OrdinalIgnoreCase))
            {
                normalizedDirectories.Add(normalizedDirectory);
            }
        }

        return [.. normalizedDirectories];
    }
}
