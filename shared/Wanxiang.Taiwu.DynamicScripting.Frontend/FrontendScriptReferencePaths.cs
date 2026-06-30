using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FrameWork.ModSystem;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.DynamicScripting.Frontend;

/// <summary>
/// Resolves frontend-only assembly reference paths for dynamic script compilation.
/// </summary>
public static class FrontendScriptReferencePaths
{
    private const string PluginsDirectoryName = "Plugins";

    /// <summary>
    /// Gets additional frontend assembly reference paths needed by common script entry code.
    /// </summary>
    /// <param name="pluginDirectory">The current frontend plugin deployment directory.</param>
    /// <returns>Additional assembly reference paths.</returns>
    public static IReadOnlyList<string> GetAdditionalAssemblyReferencePaths(string pluginDirectory)
    {
        return TryResolveAssemblyReferencePath(
            typeof(Cysharp.Threading.Tasks.UniTask).Assembly,
            GetAssemblySearchDirectories(pluginDirectory),
            out string? uniTaskPath)
            ? [uniTaskPath]
            : [];
    }

    private static string[] GetAssemblySearchDirectories(string pluginDirectory)
    {
        List<string> searchDirectories = [];
        AddSearchDirectory(searchDirectories, pluginDirectory);

        foreach (ModId modId in global::ModManager.EnabledMods)
        {
            ModInfoWithDisplayData? modInfo = global::ModManager.GetModInfo(modId);
            if (modInfo is null || string.IsNullOrWhiteSpace(modInfo.DirectoryName))
            {
                continue;
            }

            string modPluginDirectory = Path.Combine(
                modInfo.DirectoryName,
                PluginsDirectoryName);
            foreach (string frontendPlugin in modInfo.FrontendPlugins)
            {
                if (string.IsNullOrWhiteSpace(frontendPlugin))
                {
                    continue;
                }

                AddSearchDirectory(
                    searchDirectories,
                    Path.GetDirectoryName(Path.Combine(modPluginDirectory, frontendPlugin)));
                AddSearchDirectory(searchDirectories, modPluginDirectory);
            }
        }

        return [.. searchDirectories];
    }

    private static void AddSearchDirectory(
        List<string> searchDirectories,
        string? searchDirectory)
    {
        if (string.IsNullOrWhiteSpace(searchDirectory))
        {
            return;
        }

        string normalizedDirectory = Path.GetFullPath(searchDirectory);
        if (!searchDirectories.Contains(normalizedDirectory, StringComparer.OrdinalIgnoreCase))
        {
            searchDirectories.Add(normalizedDirectory);
        }
    }

    private static bool TryResolveAssemblyReferencePath(
        Assembly assembly,
        IReadOnlyList<string> searchDirectories,
        [NotNullWhen(true)] out string? referencePath)
    {
        if (TryGetAssemblyLocation(assembly, out referencePath))
        {
            return true;
        }

        return TryFindAssemblyReferencePath(
            assembly.GetName(),
            searchDirectories,
            out referencePath);
    }

    private static bool TryGetAssemblyLocation(
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

    private static bool TryFindAssemblyReferencePath(
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

    private static bool AssemblyIdentityMatches(string path, AssemblyName expectedName)
    {
        try
        {
            AssemblyName candidateName = AssemblyName.GetAssemblyName(path);
            return string.Equals(
                candidateName.FullName,
                expectedName.FullName,
                StringComparison.Ordinal);
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
