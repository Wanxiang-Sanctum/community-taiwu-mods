using System.ComponentModel;
using System.Diagnostics;
#if NET6_0_OR_GREATER
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif

namespace Wanxiang.Guanxiangtai.McpServerRuntime;

public static class McpServerEndpointRegistry
{
#if NET10_0_OR_GREATER
    private static readonly Lock EndpointFilePathSyncRoot = new();
#else
    private static readonly object EndpointFilePathSyncRoot = new();
#endif

#if !NET6_0_OR_GREATER
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };
#endif

    private static string? ConfiguredEndpointFilePath { get; set; }

    public static void ConfigureRuntimeDirectory(string runtimeDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(runtimeDirectory);
#else
        if (runtimeDirectory is null)
        {
            throw new ArgumentNullException(nameof(runtimeDirectory));
        }
#endif

        ConfigureEndpointFilePath(
            GuanxiangtaiMcpPaths.GetEndpointFilePath(runtimeDirectory));
    }

    public static void ConfigureEndpointFilePath(string endpointFilePath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(endpointFilePath);
#else
        if (endpointFilePath is null)
        {
            throw new ArgumentNullException(nameof(endpointFilePath));
        }
#endif

        if (string.IsNullOrWhiteSpace(endpointFilePath))
        {
            throw new ArgumentException("MCP server runtime endpoint file path is required.", nameof(endpointFilePath));
        }

        string fullPath = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(endpointFilePath));

        lock (EndpointFilePathSyncRoot)
        {
            ConfiguredEndpointFilePath = fullPath;
        }
    }

    public static string EndpointFilePath
    {
        get
        {
            lock (EndpointFilePathSyncRoot)
            {
                return ConfiguredEndpointFilePath
                    ?? throw new InvalidOperationException(
                        "Wanxiang.Guanxiangtai MCP server runtime endpoint file path has not been configured.");
            }
        }
    }

    public static IReadOnlyList<McpServerEndpoint> GetLiveEndpoints()
    {
        string endpointFilePath = EndpointFilePath;

        if (!File.Exists(endpointFilePath))
        {
            return [];
        }

        using IDisposable endpointFileLock = GuanxiangtaiMcpLocks.AcquireEndpointFile(endpointFilePath);
        McpServerEndpointFile endpointFile = ReadEndpointFile(endpointFilePath);

        return
        [
            .. endpointFile.Servers
                .Where(IsLiveEndpoint)
                .Select(CloneEndpoint),
        ];
    }

    public static McpServerEndpoint? TryGetLiveEndpoint()
    {
        return GetLiveEndpoints()
            .OrderByDescending(endpoint => endpoint.StartedAt)
            .FirstOrDefault();
    }

    public static McpServerEndpointRegistration Register(
        McpServerEndpoint endpoint)
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
        string endpointFilePath = EndpointFilePath;

        UpdateEndpointFile(
            endpointFilePath,
            endpointFile =>
            {
                endpointFile.Servers =
                [
                    .. endpointFile.Servers
                        .Where(existing => IsLiveEndpoint(existing)
                            && !IsSameEndpointProcess(existing, endpoint)),
                ];
                endpointFile.Servers.Add(CloneEndpoint(endpoint));
            });

        return new McpServerEndpointRegistration(endpointFilePath, endpoint);
    }

    internal static void Unregister(
        string endpointFilePath,
        McpServerEndpoint endpoint)
    {
        UpdateEndpointFile(
            endpointFilePath,
            endpointFile =>
            {
                endpointFile.Servers =
                [
                    .. endpointFile.Servers
                        .Where(existing => IsLiveEndpoint(existing)
                            && !IsSameEndpointProcess(existing, endpoint)),
                ];
            });
    }

    private static void ValidateEndpoint(McpServerEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new ArgumentException("MCP server host is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(endpoint.Path))
        {
            throw new ArgumentException("MCP server path is required.", nameof(endpoint));
        }

        if (endpoint.Port is <= 0 or > 65535)
        {
            throw new ArgumentException("MCP server port must be between 1 and 65535.", nameof(endpoint));
        }

        if (endpoint.ProcessId <= 0)
        {
            throw new ArgumentException("MCP server process id is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(endpoint.ExecutablePath))
        {
            throw new ArgumentException("MCP server executable path is required.", nameof(endpoint));
        }
    }

    private static void UpdateEndpointFile(
        string endpointFilePath,
        Action<McpServerEndpointFile> update)
    {
#if NET6_0_OR_GREATER
        string directory = Path.GetDirectoryName(endpointFilePath)!;
#else
        string directory = Path.GetDirectoryName(endpointFilePath);
#endif
        _ = Directory.CreateDirectory(directory);

        using IDisposable endpointFileLock = GuanxiangtaiMcpLocks.AcquireEndpointFile(endpointFilePath);
        McpServerEndpointFile endpointFile = ReadEndpointFile(endpointFilePath);

        update(endpointFile);

        endpointFile.ModId = GuanxiangtaiMcp.ModId;
        endpointFile.UpdatedAt = DateTimeOffset.UtcNow;

        string json = SerializeEndpointFile(endpointFile);
        File.WriteAllText(endpointFilePath, json + Environment.NewLine);
    }

    private static McpServerEndpointFile ReadEndpointFile(string endpointFilePath)
    {
        if (!File.Exists(endpointFilePath))
        {
            return new McpServerEndpointFile();
        }

        string json = File.ReadAllText(endpointFilePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new McpServerEndpointFile();
        }

        return DeserializeEndpointFile(json)
            ?? throw new InvalidDataException($"MCP server runtime endpoint file is not a JSON object: {endpointFilePath}");
    }

    private static string SerializeEndpointFile(McpServerEndpointFile endpointFile)
    {
#if NET6_0_OR_GREATER
        return JsonSerializer.Serialize(
            endpointFile,
            McpServerEndpointJsonContext.Default.McpServerEndpointFile);
#else
        return JsonConvert.SerializeObject(endpointFile, Formatting.Indented, JsonSettings);
#endif
    }

    private static McpServerEndpointFile? DeserializeEndpointFile(string json)
    {
#if NET6_0_OR_GREATER
        return JsonSerializer.Deserialize(
            json,
            McpServerEndpointJsonContext.Default.McpServerEndpointFile);
#else
        return JsonConvert.DeserializeObject<McpServerEndpointFile>(json, JsonSettings);
#endif
    }

    private static bool IsSameEndpointProcess(
        McpServerEndpoint left,
        McpServerEndpoint right)
    {
        return left.ProcessId == right.ProcessId;
    }

    private static McpServerEndpoint CloneEndpoint(
        McpServerEndpoint endpoint)
    {
        return new McpServerEndpoint
        {
            Host = endpoint.Host,
            Path = endpoint.Path,
            Port = endpoint.Port,
            ProcessId = endpoint.ProcessId,
            StartedAt = endpoint.StartedAt,
            ExecutablePath = endpoint.ExecutablePath,
        };
    }

    private static bool IsLiveEndpoint(McpServerEndpoint endpoint)
    {
        try
        {
            using Process process = Process.GetProcessById(endpoint.ProcessId);
            return !process.HasExited
                && IsSameProcessImage(process, endpoint);
        }
        catch (Exception ex) when (ex is ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or Win32Exception)
        {
            return false;
        }
    }

    private static bool IsSameProcessImage(
        Process process,
        McpServerEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.ExecutablePath))
        {
            return false;
        }

        try
        {
            string? actualPath = process.MainModule?.FileName;

            return !string.IsNullOrWhiteSpace(actualPath)
                && string.Equals(
                    Path.GetFullPath(actualPath),
                    Path.GetFullPath(endpoint.ExecutablePath),
                    GetPathComparison());
        }
        catch (Exception ex) when (ex is ArgumentException
            or InvalidOperationException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException
            or Win32Exception)
        {
            return false;
        }
    }

    private static StringComparison GetPathComparison()
    {
        return Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
