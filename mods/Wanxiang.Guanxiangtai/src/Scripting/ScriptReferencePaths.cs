using Wanxiang.Taiwu.DynamicScripting;

namespace Wanxiang.Guanxiangtai.Scripting;

public static class ScriptReferencePaths
{
    public static string GetContractReferencePath(string pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            throw new ArgumentException("Plugin directory is required.", nameof(pluginDirectory));
        }

        string referencePath = Path.Combine(
            pluginDirectory,
            typeof(GuanxiangtaiScriptGlobals).Assembly.GetName().Name + ".dll");
        return DynamicScriptAssemblyReferenceResolver.GetVerifiedAssemblyReferencePath(
            typeof(GuanxiangtaiScriptGlobals),
            referencePath);
    }
}
