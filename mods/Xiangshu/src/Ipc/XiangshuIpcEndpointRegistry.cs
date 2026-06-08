using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Xiangshu.Ipc;

public static class XiangshuIpcEndpointRegistry
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    public static string GetManifestPath()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("LocalApplicationData path is not available.");
        }

        return Path.Combine(root, "Taiwu", "Xiangshu", "ipc-endpoints.json");
    }

    public static XiangshuIpcEndpointRegistration Register(XiangshuIpcEndpoint endpoint)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        string manifestPath = GetManifestPath();

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

        return new XiangshuIpcEndpointRegistration(manifestPath, endpoint);
    }

    internal static void Unregister(string manifestPath, XiangshuIpcEndpoint endpoint)
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

    private static void UpdateManifest(string manifestPath, Action<XiangshuIpcManifest> update)
    {
        string directory = Path.GetDirectoryName(manifestPath) ?? ".";
        _ = Directory.CreateDirectory(directory);

        using FileStream lockFile = OpenLockFile(manifestPath + ".lock");
        XiangshuIpcManifest manifest = ReadManifest(manifestPath);

        update(manifest);

        manifest.Version = 1;
        manifest.UpdatedAtUtc = DateTimeOffset.UtcNow;

        string json = JsonConvert.SerializeObject(manifest, Formatting.Indented, JsonSettings);
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

    private static XiangshuIpcManifest ReadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new XiangshuIpcManifest();
        }

        string json = File.ReadAllText(manifestPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new XiangshuIpcManifest();
        }

        return JsonConvert.DeserializeObject<XiangshuIpcManifest>(json, JsonSettings)
            ?? throw new InvalidDataException($"IPC endpoint manifest is not a JSON object: {manifestPath}");
    }

    private static bool IsSameEndpointSlot(XiangshuIpcEndpoint left, XiangshuIpcEndpoint right)
    {
        return left.ProcessId == right.ProcessId
            && string.Equals(left.Side, right.Side, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLiveEndpoint(XiangshuIpcEndpoint endpoint)
    {
        try
        {
            using Process process = Process.GetProcessById(endpoint.ProcessId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

public sealed class XiangshuIpcEndpointRegistration(
    string manifestPath,
    XiangshuIpcEndpoint endpoint) : IDisposable
{
    private readonly string _manifestPath = manifestPath;
    private readonly XiangshuIpcEndpoint _endpoint = endpoint;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        XiangshuIpcEndpointRegistry.Unregister(_manifestPath, _endpoint);
    }
}

internal sealed class XiangshuIpcManifest
{
    public int Version { get; set; } = 1;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<XiangshuIpcEndpoint> Endpoints { get; set; } = [];
}

public sealed class XiangshuIpcEndpoint
{
    public string Side { get; set; } = string.Empty;

    public string Transport { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public int ProcessId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
}
