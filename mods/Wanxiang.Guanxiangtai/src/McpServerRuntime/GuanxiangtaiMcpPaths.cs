namespace Wanxiang.Guanxiangtai.McpServerRuntime;

public static class GuanxiangtaiMcpPaths
{
    public const string RuntimeDirectoryName = ".guanxiangtai-runtime";

    public const string EndpointFileName = "mcp-server-endpoints.json";

    public static string GetRuntimeDirectory(string ownerDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(ownerDirectory);
#else
        if (ownerDirectory is null)
        {
            throw new ArgumentNullException(nameof(ownerDirectory));
        }
#endif

        if (string.IsNullOrWhiteSpace(ownerDirectory))
        {
            throw new ArgumentException("Runtime owner directory is required.", nameof(ownerDirectory));
        }

        return Path.Combine(
            Path.GetFullPath(ownerDirectory),
            RuntimeDirectoryName);
    }

    public static string GetEndpointFilePath(string runtimeDirectory)
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

        return Path.Combine(
            Path.GetFullPath(runtimeDirectory),
            EndpointFileName);
    }
}
