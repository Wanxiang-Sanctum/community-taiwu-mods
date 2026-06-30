#if NET10_0_OR_GREATER
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif

namespace Wanxiang.Guanxiangtai.Ipc;

public static class IpcEndpointRegistry
{
    private const string RuntimeDirectoryName = ".guanxiangtai-runtime";

    private const string ManifestFileName = "ipc-endpoints.json";

#if NET10_0_OR_GREATER
    private static readonly Lock ManifestPathSyncRoot = new();
#else
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };

    private static readonly object ManifestPathSyncRoot = new();
#endif

    private static string? ConfiguredManifestPath { get; set; }

    public static void ConfigureForModDirectory(string modDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(modDirectory);
#else
        if (modDirectory is null)
        {
            throw new ArgumentNullException(nameof(modDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(modDirectory))
        {
            throw new ArgumentException("Mod directory is required.", nameof(modDirectory));
        }

        ConfigureForRuntimeDirectory(
            Path.Combine(
                Path.GetFullPath(modDirectory),
                RuntimeDirectoryName));
    }

    public static void ConfigureForRuntimeDirectory(string runtimeDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(runtimeDirectory);
#else
        if (runtimeDirectory is null)
        {
            throw new ArgumentNullException(nameof(runtimeDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(runtimeDirectory))
        {
            throw new ArgumentException("Runtime directory is required.", nameof(runtimeDirectory));
        }

        ConfigureManifestPath(
            Path.Combine(
                Path.GetFullPath(runtimeDirectory),
                ManifestFileName));
    }

    public static void ConfigureManifestPath(string manifestPath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(manifestPath);
#else
        if (manifestPath is null)
        {
            throw new ArgumentNullException(nameof(manifestPath));
        }
#endif

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("IPC endpoint manifest path is required.", nameof(manifestPath));
        }

        string fullPath = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(manifestPath));

        lock (ManifestPathSyncRoot)
        {
            ConfiguredManifestPath = fullPath;
        }
    }

    public static string ManifestPath
    {
        get
        {
            lock (ManifestPathSyncRoot)
            {
                return ConfiguredManifestPath
                    ?? throw new InvalidOperationException(
                        "Wanxiang.Guanxiangtai IPC endpoint manifest path has not been configured.");
            }
        }
    }

    public static IpcEndpoint? GetRegisteredEndpoint(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("IPC endpoint role is required.", nameof(role));
        }

        string manifestPath = ManifestPath;

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        using FileStream lockFile = OpenLockFile(manifestPath + ".lock");
        IpcEndpointManifest manifest = ReadManifest(manifestPath);

        return manifest.Endpoints
            .Where(endpoint => string.Equals(endpoint.Role, role, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(endpoint => endpoint.StartedAt)
            .Select(CloneEndpoint)
            .FirstOrDefault();
    }

    public static IpcEndpointRegistration Register(IpcEndpoint endpoint)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(endpoint);
#else
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }
#endif

        ValidateEndpoint(endpoint);
        string manifestPath = ManifestPath;

        UpdateManifest(
            manifestPath,
            manifest =>
            {
                manifest.Endpoints =
                [
                    .. manifest.Endpoints
                        .Where(existing => !string.Equals(
                            existing.Role,
                            endpoint.Role,
                            StringComparison.OrdinalIgnoreCase)),
                ];
                manifest.Endpoints.Add(CloneEndpoint(endpoint));
            });

        return new IpcEndpointRegistration(manifestPath, endpoint);
    }

    internal static void Unregister(
        string manifestPath,
        IpcEndpoint endpoint)
    {
        UpdateManifest(
            manifestPath,
            manifest =>
            {
                manifest.Endpoints =
                [
                    .. manifest.Endpoints
                        .Where(existing => !IsSameEndpoint(existing, endpoint)),
                ];
            });
    }

    private static void ValidateEndpoint(IpcEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Role))
        {
            throw new ArgumentException("IPC endpoint role is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(endpoint.Transport))
        {
            throw new ArgumentException("IPC endpoint transport is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new ArgumentException("IPC endpoint host is required.", nameof(endpoint));
        }

        if (endpoint.Port is <= 0 or > 65535)
        {
            throw new ArgumentException("IPC endpoint port must be between 1 and 65535.", nameof(endpoint));
        }
    }

    private static void UpdateManifest(
        string manifestPath,
        Action<IpcEndpointManifest> update)
    {
#if NET6_0_OR_GREATER
        string directory = Path.GetDirectoryName(manifestPath)!;
#else
        string directory = Path.GetDirectoryName(manifestPath);
#endif
        _ = Directory.CreateDirectory(directory);

        using FileStream lockFile = OpenLockFile(manifestPath + ".lock");
        IpcEndpointManifest manifest = ReadManifest(manifestPath);

        update(manifest);

        manifest.UpdatedAt = DateTimeOffset.UtcNow;

        string json = SerializeManifest(manifest);
        File.WriteAllText(manifestPath, json + Environment.NewLine);
    }

    private static FileStream OpenLockFile(string path)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 19)
            {
                Thread.Sleep(25);
            }
        }

        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private static IpcEndpointManifest ReadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new IpcEndpointManifest();
        }

        string json = File.ReadAllText(manifestPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new IpcEndpointManifest();
        }

        return DeserializeManifest(json)
            ?? throw new InvalidDataException($"IPC endpoint manifest is not a JSON object: {manifestPath}");
    }

    private static string SerializeManifest(IpcEndpointManifest manifest)
    {
#if NET10_0_OR_GREATER
        return JsonSerializer.Serialize(
            manifest,
            IpcEndpointJsonContext.Default.IpcEndpointManifest);
#else
        return JsonConvert.SerializeObject(manifest, Formatting.Indented, JsonSettings);
#endif
    }

    private static IpcEndpointManifest? DeserializeManifest(string json)
    {
#if NET10_0_OR_GREATER
        return JsonSerializer.Deserialize(
            json,
            IpcEndpointJsonContext.Default.IpcEndpointManifest);
#else
        return JsonConvert.DeserializeObject<IpcEndpointManifest>(json, JsonSettings);
#endif
    }

    private static IpcEndpoint CloneEndpoint(IpcEndpoint endpoint)
    {
        return new IpcEndpoint
        {
            Role = endpoint.Role,
            Transport = endpoint.Transport,
            Host = endpoint.Host,
            Port = endpoint.Port,
            StartedAt = endpoint.StartedAt,
        };
    }

    private static bool IsSameEndpoint(
        IpcEndpoint left,
        IpcEndpoint right)
    {
        return string.Equals(left.Role, right.Role, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Transport, right.Transport, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
            && left.Port == right.Port
            && left.StartedAt == right.StartedAt;
    }
}
