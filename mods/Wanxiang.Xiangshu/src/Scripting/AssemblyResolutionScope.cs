using System.Reflection;

namespace Wanxiang.Xiangshu.Scripting;

internal sealed class AssemblyResolutionScope : IDisposable
{
    private readonly List<string> _referenceDirectories;
    private readonly ResolveEventHandler _handler;
    private bool _disposed;

    public AssemblyResolutionScope(List<string> referenceDirectories)
    {
        _referenceDirectories = referenceDirectories;
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

        string? referencePath = FindReferenceAssemblyPath(requestedName);
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
                    StringComparison.OrdinalIgnoreCase)
                || AssemblyName.ReferenceMatchesDefinition(requestedName, candidateName))
            {
                return assembly;
            }
        }

        return null;
    }

    private string? FindReferenceAssemblyPath(AssemblyName requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName.Name))
        {
            return null;
        }

        string fileName = requestedName.Name + ".dll";
        foreach (string referenceDirectory in _referenceDirectories)
        {
            string candidatePath = Path.Combine(referenceDirectory, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return null;
    }
}
