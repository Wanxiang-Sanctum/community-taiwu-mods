using System.Reflection;

namespace Wanxiang.Taiwu.DynamicScripting;

internal sealed class AssemblyResolutionScope : IDisposable
{
    private readonly List<string> _assemblyReferencePaths;
    private readonly ResolveEventHandler _handler;
    private bool _disposed;

    public AssemblyResolutionScope(IEnumerable<string> assemblyReferencePaths)
    {
        _assemblyReferencePaths = [.. assemblyReferencePaths];
        _handler = ResolveAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += _handler;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AppDomain.CurrentDomain.AssemblyResolve -= _handler;
    }

    private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        AssemblyName requestedName = new(args.Name);
        Assembly? loadedAssembly = FindLoadedAssembly(requestedName);
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        string? referencePath = FindExplicitReferenceAssemblyPath(requestedName);
        if (referencePath is null)
        {
            return null;
        }

        return Assembly.Load(File.ReadAllBytes(referencePath));
    }

    private static Assembly? FindLoadedAssembly(AssemblyName requestedName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            AssemblyName candidateName = assembly.GetName();
            if (string.Equals(
                    candidateName.FullName,
                    requestedName.FullName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return assembly;
            }
        }

        return null;
    }

    private string? FindExplicitReferenceAssemblyPath(AssemblyName requestedName)
    {
        foreach (string referencePath in _assemblyReferencePaths)
        {
            if (File.Exists(referencePath)
                && DynamicScriptAssemblyReferenceResolver.AssemblyIdentityMatches(referencePath, requestedName))
            {
                return referencePath;
            }
        }

        return null;
    }
}
