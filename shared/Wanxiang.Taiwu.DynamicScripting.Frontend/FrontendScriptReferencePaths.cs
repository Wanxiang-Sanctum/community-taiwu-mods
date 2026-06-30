using FrameWork.ModSystem;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.DynamicScripting.Frontend;

/// <summary>
/// Resolves frontend-only assembly reference paths for dynamic script compilation.
/// </summary>
public static class FrontendScriptReferencePaths
{
    private const string PluginsDirectoryName = "Plugins";
    private const FrontendScriptReferenceFeatures SupportedFeatures =
        FrontendScriptReferenceFeatures.UniTask;

    /// <summary>
    /// Gets frontend assembly reference paths for explicitly enabled script features.
    /// </summary>
    /// <param name="pluginDirectory">The current frontend plugin deployment directory.</param>
    /// <param name="features">The frontend script reference features to expose.</param>
    /// <returns>Explicit assembly reference paths for the requested features.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="features"/> contains unsupported flags.</exception>
    /// <exception cref="InvalidOperationException">A requested feature assembly cannot be resolved.</exception>
    public static IReadOnlyList<string> GetAssemblyReferencePaths(
        string pluginDirectory,
        FrontendScriptReferenceFeatures features)
    {
        ThrowIfUnsupportedFeatures(features);

        List<string> referencePaths = [];
        if ((features & FrontendScriptReferenceFeatures.UniTask) != FrontendScriptReferenceFeatures.None)
        {
            referencePaths.Add(
                DynamicScriptAssemblyReferenceResolver.ResolveRequiredAssemblyReferencePath(
                    typeof(Cysharp.Threading.Tasks.UniTask).Assembly,
                    GetAssemblySearchDirectories(pluginDirectory)));
        }

        return referencePaths;
    }

    private static void ThrowIfUnsupportedFeatures(FrontendScriptReferenceFeatures features)
    {
        if ((features & SupportedFeatures) != features)
        {
            throw new ArgumentOutOfRangeException(
                nameof(features),
                features,
                "Unsupported frontend script reference features.");
        }
    }

    private static string[] GetAssemblySearchDirectories(string pluginDirectory)
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
