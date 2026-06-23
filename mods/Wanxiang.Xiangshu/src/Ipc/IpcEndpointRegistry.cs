using System.Diagnostics;
#if NET10_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif

namespace Wanxiang.Xiangshu.Ipc;

public static class IpcEndpointRegistry
{
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

        ConfigureForWorkingDirectory(
            XiangshuRuntimePaths.ResolveAgentWorkingDirectory(
                Path.GetFullPath(modDirectory),
                configuredValue: null));
    }

    public static void ConfigureForWorkingDirectory(string workingDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(workingDirectory);
#else
        if (workingDirectory is null)
        {
            throw new ArgumentNullException(nameof(workingDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException("Agent working directory is required.", nameof(workingDirectory));
        }

        ConfigureManifestPath(
            XiangshuRuntimePaths.GetIpcManifestPath(workingDirectory));
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
                        "Wanxiang.Xiangshu IPC endpoint manifest path has not been configured.");
            }
        }
    }

    public static IReadOnlyList<IpcEndpoint> GetLiveEndpoints()
    {
        string manifestPath = ManifestPath;

        if (!File.Exists(manifestPath))
        {
            return [];
        }

        using FileStream lockFile = OpenLockFile(manifestPath + ".lock");
        IpcEndpointManifest manifest = ReadManifest(manifestPath);

        return
        [
            .. manifest.Endpoints
                .Where(IsLiveEndpoint)
                .Select(CloneEndpoint),
        ];
    }

    public static IpcEndpoint? TryGetLiveEndpoint(string role)
    {
        return GetLiveEndpoints()
            .Where(endpoint => string.Equals(endpoint.Role, role, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(endpoint => endpoint.StartedAtUtc)
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

        string manifestPath = ManifestPath;

        UpdateManifest(
            manifestPath,
            manifest =>
            {
                manifest.Endpoints =
                [
                    .. manifest.Endpoints
                        .Where(existing => IsLiveEndpoint(existing)
                            && !IsSameEndpointSlot(existing, endpoint)),
                ];
                manifest.Endpoints.Add(endpoint);
            });

        return new IpcEndpointRegistration(manifestPath, endpoint);
    }

    internal static void Unregister(string manifestPath, IpcEndpoint endpoint)
    {
        UpdateManifest(
            manifestPath,
            manifest =>
            {
                manifest.Endpoints =
                [
                    .. manifest.Endpoints
                        .Where(existing => IsLiveEndpoint(existing)
                            && !IsSameEndpointSlot(existing, endpoint)),
                ];
            });
    }

    private static void UpdateManifest(string manifestPath, Action<IpcEndpointManifest> update)
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

        manifest.UpdatedAtUtc = DateTimeOffset.UtcNow;

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

    private static bool IsSameEndpointSlot(IpcEndpoint left, IpcEndpoint right)
    {
        return left.ProcessId == right.ProcessId
            && string.Equals(left.Role, right.Role, StringComparison.OrdinalIgnoreCase);
    }

    private static IpcEndpoint CloneEndpoint(IpcEndpoint endpoint)
    {
        return new IpcEndpoint
        {
            Role = endpoint.Role,
            Transport = endpoint.Transport,
            Host = endpoint.Host,
            Path = endpoint.Path,
            Port = endpoint.Port,
            ProcessId = endpoint.ProcessId,
            StartedAtUtc = endpoint.StartedAtUtc,
        };
    }

    private static bool IsLiveEndpoint(IpcEndpoint endpoint)
    {
        try
        {
            using Process process = Process.GetProcessById(endpoint.ProcessId);
            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }
}

public sealed class IpcEndpointRegistration(
    string manifestPath,
    IpcEndpoint endpoint) : IDisposable
{
    private readonly string _manifestPath = manifestPath;
    private readonly IpcEndpoint _endpoint = endpoint;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IpcEndpointRegistry.Unregister(_manifestPath, _endpoint);
    }
}

internal sealed class IpcEndpointManifest
{
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<IpcEndpoint> Endpoints { get; set; } = [];
}

public sealed class IpcEndpoint
{
    public string Role { get; set; } = string.Empty;

    public string Transport { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int Port { get; set; }

    public int ProcessId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
}

#if NET10_0_OR_GREATER
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(IpcEndpointManifest))]
internal sealed partial class IpcEndpointJsonContext : JsonSerializerContext;
#endif
