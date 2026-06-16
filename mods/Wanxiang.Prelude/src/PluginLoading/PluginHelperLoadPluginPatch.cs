using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Prelude.PluginLoading;

[HarmonyPatch(typeof(PluginHelper), nameof(PluginHelper.LoadPlugin))]
internal static class PluginHelperLoadPluginPatch
{
    [SuppressMessage(
        "Roslynator",
        "RCS1213:Remove unused method declaration",
        Justification = "Harmony invokes patch methods by signature.")]
    private static bool Prefix(
        string directoryPath,
        string dllName,
        string modIdStr,
        ref TaiwuRemakePlugin __result)
    {
        __result = PluginAssemblyLoader.LoadPlugin(directoryPath, dllName, modIdStr);
        return false;
    }
}
