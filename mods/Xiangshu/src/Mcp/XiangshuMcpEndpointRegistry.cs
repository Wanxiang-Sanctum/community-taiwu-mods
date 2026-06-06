using System.Diagnostics;
using System.Text.Json;

namespace Xiangshu.Mcp;

internal static class XiangshuMcpEndpointRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static XiangshuMcpEndpointRegistration Register(XiangshuMcpEndpoint endpoint)
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

        return new XiangshuMcpEndpointRegistration(manifestPath, endpoint);
    }

    internal static void Unregister(string manifestPath, XiangshuMcpEndpoint endpoint)
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

    private static string GetManifestPath()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("LocalApplicationData path is not available.");
        }

        return Path.Combine(root, "Taiwu", "Xiangshu", "mcp-endpoints.json");
    }

    private static void UpdateManifest(string manifestPath, Action<XiangshuMcpManifest> update)
    {
        string directory = Path.GetDirectoryName(manifestPath) ?? ".";
        _ = Directory.CreateDirectory(directory);

        using FileStream lockFile = OpenLockFile(manifestPath + ".lock");
        XiangshuMcpManifest manifest = ReadManifest(manifestPath);

        update(manifest);

        manifest.Version = 1;
        manifest.UpdatedAtUtc = DateTimeOffset.UtcNow;

        string json = JsonSerializer.Serialize(manifest, JsonOptions);
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

    private static XiangshuMcpManifest ReadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new XiangshuMcpManifest();
        }

        string json = File.ReadAllText(manifestPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new XiangshuMcpManifest();
        }

        return JsonSerializer.Deserialize<XiangshuMcpManifest>(json, JsonOptions)
            ?? throw new InvalidDataException($"MCP endpoint manifest is not a JSON object: {manifestPath}");
    }

    private static bool IsSameEndpointSlot(XiangshuMcpEndpoint left, XiangshuMcpEndpoint right)
    {
        return left.ProcessId == right.ProcessId
            && string.Equals(left.Side, right.Side, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLiveEndpoint(XiangshuMcpEndpoint endpoint)
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

internal sealed class XiangshuMcpEndpointRegistration(
    string manifestPath,
    XiangshuMcpEndpoint endpoint) : IDisposable
{
    private readonly string _manifestPath = manifestPath;
    private readonly XiangshuMcpEndpoint _endpoint = endpoint;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        XiangshuMcpEndpointRegistry.Unregister(_manifestPath, _endpoint);
    }
}

internal sealed class XiangshuMcpManifest
{
    public int Version { get; set; } = 1;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<XiangshuMcpEndpoint> Endpoints { get; set; } = [];
}

internal sealed class XiangshuMcpEndpoint
{
    public string Side { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;

    public string ServerTitle { get; set; } = string.Empty;

    public string ServerVersion { get; set; } = string.Empty;

    public string Transport { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string AuthorizationHeader { get; set; } = string.Empty;

    public int ProcessId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
}
