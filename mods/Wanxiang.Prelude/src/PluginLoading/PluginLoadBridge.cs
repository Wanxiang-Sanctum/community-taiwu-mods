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
        ThrowIfNull(currentPluginAssembly, nameof(currentPluginAssembly));
        ThrowIfNull(
            currentPluginSearchDirectories,
            nameof(currentPluginSearchDirectories));

        string[] searchDirectories = PluginAssemblyResolver.NormalizeSearchDirectories(
            currentPluginSearchDirectories);
        PluginAssemblyResolver.EnsureInstalled();
        PluginAssemblyResolver.RegisterAssemblyGraph(
            currentPluginAssembly,
            searchDirectories);

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

    private static void ThrowIfNull<T>(T? value, string paramName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
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
