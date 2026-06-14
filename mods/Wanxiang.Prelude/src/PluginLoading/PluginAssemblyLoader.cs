using System.Reflection;
using System.Text;
using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Prelude.PluginLoading;

internal static class PluginAssemblyLoader
{
    public static TaiwuRemakePlugin LoadPlugin(
        string directoryPath,
        string dllName,
        string modIdStr)
    {
        try
        {
            string pluginRootDirectory = Path.GetFullPath(directoryPath);
            string pluginPath = Path.GetFullPath(
                Path.Combine(pluginRootDirectory, dllName));
            string pluginDirectory = Path.GetDirectoryName(pluginPath)
                ?? pluginRootDirectory;
            string[] searchDirectories = PluginAssemblyResolver.NormalizeSearchDirectories(
                pluginDirectory,
                pluginRootDirectory);

            using (PluginAssemblyResolver.PushSearchDirectories(searchDirectories))
            {
                Assembly assembly = PluginAssemblyResolver.LoadAssemblyGraphFromPath(
                    pluginPath,
                    searchDirectories,
                    usePathCache: false);

                Type entrypointType = GetEntrypointType(assembly)
                    ?? throw new TypeLoadException(
                        $"Plugin entrypoint was not found in \"{pluginPath}\".");
                if (Activator.CreateInstance(entrypointType) is not TaiwuRemakePlugin plugin)
                {
                    throw new InvalidOperationException(
                        $"Failed to create entrypoint instance in plugin \"{pluginPath}\".");
                }

                plugin.ModIdStr = modIdStr;
                plugin.Initialize();
                return plugin;
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            throw CreatePluginLoadException(ex);
        }
    }

    private static Type? GetEntrypointType(Assembly assembly)
    {
        return assembly
            .GetExportedTypes()
            .FirstOrDefault(static type => typeof(TaiwuRemakePlugin).IsAssignableFrom(type));
    }

    private static InvalidOperationException CreatePluginLoadException(
        ReflectionTypeLoadException ex)
    {
        StringBuilder message = new();
        foreach (Exception? loaderException in ex.LoaderExceptions)
        {
            if (loaderException is not null)
            {
                _ = message.AppendLine(loaderException.Message);
            }
        }

        return new InvalidOperationException(message.ToString(), ex);
    }
}
