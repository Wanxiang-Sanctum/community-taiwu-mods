using System.Reflection;
using HarmonyLib;

namespace Wanxiang.Prelude.PluginLoading;

public static class PluginLoadBridge
{
    private static readonly object HarmonySync = new();

    private static Harmony? s_harmony;

    public static void Apply(
        string harmonyId,
        Assembly currentPluginAssembly,
        params string[] currentPluginSearchDirectories)
    {
        PluginAssemblyResolver.EnsureInstalled();
        PluginAssemblyResolver.RegisterAssembly(
            currentPluginAssembly,
            currentPluginSearchDirectories);

        lock (HarmonySync)
        {
            if (s_harmony is not null)
            {
                return;
            }

            Harmony harmony = new(harmonyId);
            harmony.PatchAll(typeof(PluginLoadBridge).Assembly);
            s_harmony = harmony;
        }
    }

    public static void Unpatch()
    {
        lock (HarmonySync)
        {
            s_harmony?.UnpatchSelf();
            s_harmony = null;
        }
    }
}
