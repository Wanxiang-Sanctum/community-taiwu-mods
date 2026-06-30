using FrameWork.ModSystem;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.DynamicScripting.Frontend;

/// <summary>
/// Creates explicit assembly reference options for frontend dynamic scripts.
/// </summary>
public static class FrontendScriptReferences
{
    private const string PluginsDirectoryName = "Plugins";

    /// <summary>
    /// Creates reference options for frontend scripts.
    /// </summary>
    /// <param name="pluginDirectory">The current frontend plugin deployment directory.</param>
    /// <param name="scriptContractMarkerType">A type defined by the script contract assembly.</param>
    /// <param name="facadeMarkerTypes">Optional marker types for facade assemblies exposed to scripts.</param>
    /// <returns>Reference options that include the script contract, facade assemblies, and UniTask metadata reference.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pluginDirectory"/> is null or whitespace, or
    /// <paramref name="facadeMarkerTypes"/> contains a null entry.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="scriptContractMarkerType"/> or <paramref name="facadeMarkerTypes"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">A required assembly cannot be resolved.</exception>
    public static DynamicScriptReferenceOptions CreateOptions(
        string pluginDirectory,
        Type scriptContractMarkerType,
        params Type[] facadeMarkerTypes)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            throw new ArgumentException("Plugin directory is required.", nameof(pluginDirectory));
        }

        if (scriptContractMarkerType is null)
        {
            throw new ArgumentNullException(nameof(scriptContractMarkerType));
        }

        if (facadeMarkerTypes is null)
        {
            throw new ArgumentNullException(nameof(facadeMarkerTypes));
        }

        string normalizedPluginDirectory = Path.GetFullPath(pluginDirectory);
        string[] pluginSearchDirectories = [normalizedPluginDirectory];
        string[] frontendDependencySearchDirectories =
            GetFrontendDependencySearchDirectories(normalizedPluginDirectory);
        List<string> assemblyReferencePaths = new(facadeMarkerTypes.Length + 2)
        {
            DynamicScriptAssemblyReferenceResolver.ResolveRequiredAssemblyReferencePath(
                scriptContractMarkerType,
                pluginSearchDirectories),
        };

        foreach (Type? markerType in facadeMarkerTypes)
        {
            if (markerType is null)
            {
                throw new ArgumentException(
                    "Facade marker types cannot contain null entries.",
                    nameof(facadeMarkerTypes));
            }

            assemblyReferencePaths.Add(
                DynamicScriptAssemblyReferenceResolver.ResolveRequiredAssemblyReferencePath(
                    markerType,
                    pluginSearchDirectories));
        }

        assemblyReferencePaths.Add(
            DynamicScriptAssemblyReferenceResolver.ResolveRequiredAssemblyReferencePath(
                typeof(Cysharp.Threading.Tasks.UniTask).Assembly,
                frontendDependencySearchDirectories));

        return new DynamicScriptReferenceOptions(assemblyReferencePaths);
    }

    private static string[] GetFrontendDependencySearchDirectories(string pluginDirectory)
    {
        List<string> resolvedSearchDirectories = [];
        AddSearchDirectory(resolvedSearchDirectories, pluginDirectory);

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
                    resolvedSearchDirectories,
                    Path.GetDirectoryName(Path.Combine(modPluginDirectory, frontendPlugin)));
                AddSearchDirectory(resolvedSearchDirectories, modPluginDirectory);
            }
        }

        return [.. resolvedSearchDirectories];
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
}
