using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Wanxiang.Prelude.PluginLoading;

internal static class PluginAssemblyResolver
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, Assembly> LoadedAssembliesByPath =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Assembly, string[]> SearchDirectoriesByAssembly = [];

    private static bool s_resolverInstalled;

    [ThreadStatic]
    private static string[]? s_activeSearchDirectories;

    public static void EnsureInstalled()
    {
        lock (Sync)
        {
            if (s_resolverInstalled)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            s_resolverInstalled = true;
        }
    }

    public static string[] NormalizeSearchDirectories(params string?[] searchDirectories)
    {
        List<string> normalizedSearchDirectories = [];
        foreach (string? directory in searchDirectories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string normalizedDirectory = Path
                .GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!normalizedSearchDirectories.Contains(
                normalizedDirectory,
                StringComparer.OrdinalIgnoreCase))
            {
                normalizedSearchDirectories.Add(normalizedDirectory);
            }
        }

        return [.. normalizedSearchDirectories];
    }

    public static IDisposable PushSearchDirectories(
        IReadOnlyList<string> searchDirectories)
    {
        return new ResolverScope(searchDirectories);
    }

    public static void RegisterAssembly(
        Assembly assembly,
        params string[] searchDirectories)
    {
        string[] normalizedSearchDirectories = NormalizeSearchDirectories(searchDirectories);
        lock (Sync)
        {
            SearchDirectoriesByAssembly[assembly] = normalizedSearchDirectories;
        }
    }

    public static Assembly LoadAssemblyFromPath(
        string path,
        IReadOnlyList<string> searchDirectories,
        bool usePathCache)
    {
        string fullPath = Path.GetFullPath(path);
        lock (Sync)
        {
            if (usePathCache
                && LoadedAssembliesByPath.TryGetValue(fullPath, out Assembly? cachedAssembly))
            {
                return cachedAssembly;
            }

            byte[] rawAssembly = File.ReadAllBytes(fullPath);
            string symbolPath = Path.ChangeExtension(fullPath, "pdb");
            Assembly assembly = File.Exists(symbolPath)
                ? Assembly.Load(rawAssembly, File.ReadAllBytes(symbolPath))
                : Assembly.Load(rawAssembly);
            string assemblyDirectory = Path.GetDirectoryName(fullPath)
                ?? Path.GetDirectoryName(Path.GetFullPath(path))
                ?? string.Empty;

            RegisterAssembly(assembly, [assemblyDirectory, .. searchDirectories]);
            if (usePathCache)
            {
                LoadedAssembliesByPath[fullPath] = assembly;
            }

            return assembly;
        }
    }

    public static bool TryResolve(
        AssemblyName assemblyName,
        IReadOnlyList<string> searchDirectories,
        [NotNullWhen(true)] out Assembly? assembly)
    {
        if (TryLoadFromSearchDirectories(
            assemblyName,
            searchDirectories,
            out assembly))
        {
            return true;
        }

        return TryFindLoadedAssembly(assemblyName, out assembly);
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        AssemblyName assemblyName = new(args.Name);
        string[] searchDirectories = GetSearchDirectories(args.RequestingAssembly);
        return TryResolve(assemblyName, searchDirectories, out Assembly? assembly)
            ? assembly
            : null;
    }

    private static string[] GetSearchDirectories(Assembly? requestingAssembly)
    {
        List<string> searchDirectories = [];
        if (requestingAssembly is not null)
        {
            lock (Sync)
            {
                if (SearchDirectoriesByAssembly.TryGetValue(
                    requestingAssembly,
                    out string[]? registeredDirectories))
                {
                    searchDirectories.AddRange(registeredDirectories);
                }
            }
        }

        if (s_activeSearchDirectories is not null)
        {
            searchDirectories.AddRange(s_activeSearchDirectories);
        }

        return NormalizeSearchDirectories([.. searchDirectories]);
    }

    private static bool TryFindLoadedAssembly(
        AssemblyName assemblyName,
        [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(
                candidate.GetName().FullName,
                assemblyName.FullName,
                StringComparison.Ordinal));
        return assembly is not null;
    }

    private static bool TryLoadFromSearchDirectories(
        AssemblyName assemblyName,
        IReadOnlyList<string> searchDirectories,
        [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return false;
        }

        foreach (string searchDirectory in searchDirectories)
        {
            string path = Path.Combine(searchDirectory, assemblyName.Name + ".dll");
            if (!File.Exists(path))
            {
                continue;
            }

            assembly = LoadAssemblyFromPath(
                path,
                searchDirectories,
                usePathCache: true);
            AssemblyName loadedName = assembly.GetName();
            if (!string.Equals(
                loadedName.Name,
                assemblyName.Name,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Unexpected assembly name info: looking for {assemblyName.Name} => {loadedName.Name} loaded.");
            }

            return true;
        }

        return false;
    }

    private sealed class ResolverScope : IDisposable
    {
        private readonly string[]? _previousSearchDirectories;
        private bool _disposed;

        public ResolverScope(IReadOnlyList<string> searchDirectories)
        {
            _previousSearchDirectories = s_activeSearchDirectories;
            s_activeSearchDirectories = NormalizeSearchDirectories([.. searchDirectories]);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            s_activeSearchDirectories = _previousSearchDirectories;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
