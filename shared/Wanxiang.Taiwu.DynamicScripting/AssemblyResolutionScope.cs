using System.Reflection;

namespace Wanxiang.Taiwu.DynamicScripting;

internal sealed class AssemblyResolutionScope : IDisposable
{
    private readonly List<string> _referenceDirectories;
    private readonly List<string> _assemblyReferencePaths;
    private readonly ResolveEventHandler _handler;
    private bool _disposed;

    public AssemblyResolutionScope(
        IEnumerable<string> referenceDirectories,
        IEnumerable<string> assemblyReferencePaths)
    {
        _referenceDirectories = [.. referenceDirectories];
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

        string? explicitReferencePath = FindExplicitReferenceAssemblyPath(requestedName);
        if (explicitReferencePath is not null)
        {
            return explicitReferencePath;
        }

        string fileName = requestedName.Name + ".dll";
        foreach (string referenceDirectory in _referenceDirectories)
        {
            string candidatePath = Path.Combine(referenceDirectory, fileName);
            if (File.Exists(candidatePath) && AssemblyIdentityMatches(candidatePath, requestedName))
            {
                return candidatePath;
            }
        }

        return null;
    }

    private string? FindExplicitReferenceAssemblyPath(AssemblyName requestedName)
    {
        foreach (string referencePath in _assemblyReferencePaths)
        {
            if (File.Exists(referencePath) && AssemblyIdentityMatches(referencePath, requestedName))
            {
                return referencePath;
            }
        }

        return null;
    }

    private static bool AssemblyIdentityMatches(string path, AssemblyName expectedName)
    {
        try
        {
            AssemblyName candidateName = AssemblyName.GetAssemblyName(path);
            return string.Equals(
                    candidateName.FullName,
                    expectedName.FullName,
                    StringComparison.OrdinalIgnoreCase)
                || AssemblyName.ReferenceMatchesDefinition(expectedName, candidateName);
        }
        catch (Exception ex) when (ex is ArgumentException
            or BadImageFormatException
            or IOException
            or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
