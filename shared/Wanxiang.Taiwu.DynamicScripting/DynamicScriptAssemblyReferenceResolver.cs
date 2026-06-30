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
    /// <returns>The resolved assembly reference path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="markerType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The assembly cannot be resolved to a metadata reference path.</exception>
    public static string ResolveRequiredAssemblyReferencePath(Type markerType)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(markerType);
#else
        if (markerType is null)
        {
            throw new ArgumentNullException(nameof(markerType));
        }
#endif

        return ResolveRequiredAssemblyReferencePath(markerType.Assembly);
    }

    /// <summary>
    /// Resolves the supplied assembly to a metadata reference path.
    /// </summary>
    /// <param name="assembly">The assembly to reference.</param>
    /// <returns>The resolved assembly reference path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The assembly cannot be resolved to a metadata reference path.</exception>
    public static string ResolveRequiredAssemblyReferencePath(Assembly assembly)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assembly);
#else
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }
#endif

        if (TryGetAssemblyLocation(assembly, out string? referencePath))
        {
            return referencePath;
        }

        throw new InvalidOperationException(
            "Could not resolve a metadata reference path for assembly "
            + $"'{assembly.GetName().FullName}'. Pass the exact DLL path through "
            + "DynamicScriptReferenceOptions.");
    }

    /// <summary>
    /// Gets a normalized reference path after verifying that it matches the assembly that defines a marker type.
    /// </summary>
    /// <param name="markerType">A type defined by the assembly to reference.</param>
    /// <param name="referencePath">The exact DLL file to reference.</param>
    /// <returns>The normalized assembly reference path.</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="markerType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The reference path is missing or does not match the marker assembly.</exception>
    public static string GetVerifiedAssemblyReferencePath(
        Type markerType,
        string referencePath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(markerType);
#else
        if (markerType is null)
        {
            throw new ArgumentNullException(nameof(markerType));
        }
#endif

        return GetVerifiedAssemblyReferencePath(markerType.Assembly, referencePath);
    }

    /// <summary>
    /// Gets a normalized reference path after verifying that it matches the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly to reference.</param>
    /// <param name="referencePath">The exact DLL file to reference.</param>
    /// <returns>The normalized assembly reference path.</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The reference path is missing or does not match the assembly.</exception>
    public static string GetVerifiedAssemblyReferencePath(
        Assembly assembly,
        string referencePath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assembly);
#else
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }
#endif

        return GetVerifiedAssemblyReferencePath(assembly.GetName(), referencePath);
    }

    /// <summary>
    /// Gets a normalized reference path after verifying that it matches the supplied assembly identity.
    /// </summary>
    /// <param name="assemblyName">The exact assembly identity to reference.</param>
    /// <param name="referencePath">The exact DLL file to reference.</param>
    /// <returns>The normalized assembly reference path.</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The reference path is missing or does not match the assembly identity.</exception>
    public static string GetVerifiedAssemblyReferencePath(
        AssemblyName assemblyName,
        string referencePath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencePath);
#else
        if (assemblyName is null)
        {
            throw new ArgumentNullException(nameof(assemblyName));
        }

        if (string.IsNullOrWhiteSpace(referencePath))
        {
            throw new ArgumentException("Assembly reference path is required.", nameof(referencePath));
        }
#endif

        string normalizedReferencePath = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(referencePath.Trim()));
        if (!File.Exists(normalizedReferencePath))
        {
            throw new InvalidOperationException(
                $"Assembly reference path does not exist: '{normalizedReferencePath}'.");
        }

        if (!AssemblyIdentityMatches(normalizedReferencePath, assemblyName))
        {
            throw new InvalidOperationException(
                "Assembly reference path does not match expected assembly identity "
                + $"'{assemblyName.FullName}': '{normalizedReferencePath}'.");
        }

        return normalizedReferencePath;
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

}
