using Cysharp.Threading.Tasks;
using FrameWork.ModSystem;
using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Xiangshu.Scripting;

namespace Wanxiang.Xiangshu.Frontend.Ipc;

internal static class ScriptReferences
{
    private const string PluginsDirectoryName = "Plugins";
    private const string PreludeModId = "WanxiangPrelude";
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
        System.Reflection.Assembly assembly,
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
        ModInfoWithDisplayData? modInfo = global::ModManager.GetModInfo(PreludeModId);
        if (modInfo is null || string.IsNullOrWhiteSpace(modInfo.DirectoryName))
        {
            throw new InvalidOperationException(
                "Wanxiang.Prelude frontend runtime dependency could not be resolved.");
        }

        return Path.Combine(
            Path.GetFullPath(modInfo.DirectoryName),
            PluginsDirectoryName,
            PreludeFrontendPluginDirectoryName);
    }
}
