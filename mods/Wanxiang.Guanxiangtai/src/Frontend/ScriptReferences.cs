using Cysharp.Threading.Tasks;
using FrameWork.ModSystem;
using GameData.Domains.Mod;
using System.Reflection;
using Wanxiang.Guanxiangtai.Scripting;
using Wanxiang.Taiwu.DynamicScripting;

namespace Wanxiang.Guanxiangtai.Frontend;

internal static class ScriptReferences
{
    private const string PluginsDirectoryName = "Plugins";
    private const ulong PreludeFileId = 3747731025;
    private const string PreludeFrontendPluginDirectoryName = "Frontend";
    private const string UniTaskFileName = "UniTask.dll";

    public static DynamicScriptReferenceOptions Create(string pluginDirectory)
    {
        return new DynamicScriptReferenceOptions(
        [
            ScriptReferencePaths.GetContractReferencePath(pluginDirectory),
            GetPreludeFrontendReferencePath(
                typeof(UniTask).Assembly,
                UniTaskFileName),
        ]);
    }

    private static string GetPreludeFrontendReferencePath(
        Assembly assembly,
        string fileName)
    {
        string referencePath = Path.Combine(
            GetPreludeFrontendPluginDirectory(),
            fileName);
        return DynamicScriptAssemblyReferenceResolver.GetVerifiedAssemblyReferencePath(
            assembly,
            referencePath);
    }

    private static string GetPreludeFrontendPluginDirectory()
    {
        ModInfoWithDisplayData modInfo = GetEnabledPreludeModInfo();
        return Path.Combine(
            Path.GetFullPath(modInfo.DirectoryName),
            PluginsDirectoryName,
            PreludeFrontendPluginDirectoryName);
    }

    private static ModInfoWithDisplayData GetEnabledPreludeModInfo()
    {
        foreach (ModId modId in ModManager.EnabledMods)
        {
            if (modId.FileId != PreludeFileId)
            {
                continue;
            }

            ModInfoWithDisplayData? modInfo = ModManager.GetModInfo(modId);
            if (modInfo is null || string.IsNullOrWhiteSpace(modInfo.DirectoryName))
            {
                continue;
            }

            return modInfo;
        }

        throw new InvalidOperationException(
            "Wanxiang.Prelude frontend runtime dependency could not be resolved.");
    }
}
