using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Wanxiang.Taiwu.DynamicScripting;

/// <summary>
/// 将宿主声明的程序集解析为 metadata 引用 DLL 路径。
/// </summary>
public static class DynamicScriptAssemblyReferenceResolver
{
    /// <summary>
    /// 将定义给定标记类型的程序集解析为 metadata 引用路径。
    /// </summary>
    /// <param name="markerType">由待引用程序集定义的类型。</param>
    /// <returns>解析后的程序集引用路径。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="markerType"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">无法将程序集解析为 metadata 引用路径。</exception>
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
    /// 将给定程序集解析为 metadata 引用路径。
    /// </summary>
    /// <param name="assembly">要引用的程序集。</param>
    /// <returns>解析后的程序集引用路径。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">无法将程序集解析为 metadata 引用路径。</exception>
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
    /// 校验引用路径匹配定义标记类型的程序集，并返回规范化后的引用路径。
    /// </summary>
    /// <param name="markerType">由待引用程序集定义的类型。</param>
    /// <param name="referencePath">要引用的确切 DLL 文件。</param>
    /// <returns>规范化后的程序集引用路径。</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> 为 null 或空白字符串。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="markerType"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">引用路径不存在，或与标记程序集不匹配。</exception>
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
    /// 校验引用路径匹配给定程序集，并返回规范化后的引用路径。
    /// </summary>
    /// <param name="assembly">要引用的程序集。</param>
    /// <param name="referencePath">要引用的确切 DLL 文件。</param>
    /// <returns>规范化后的程序集引用路径。</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> 为 null 或空白字符串。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">引用路径不存在，或与程序集不匹配。</exception>
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
    /// 校验引用路径匹配给定程序集身份，并返回规范化后的引用路径。
    /// </summary>
    /// <param name="assemblyName">要引用的确切程序集身份。</param>
    /// <param name="referencePath">要引用的确切 DLL 文件。</param>
    /// <returns>规范化后的程序集引用路径。</returns>
    /// <exception cref="ArgumentException"><paramref name="referencePath"/> 为 null 或空白字符串。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">引用路径不存在，或与程序集身份不匹配。</exception>
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
