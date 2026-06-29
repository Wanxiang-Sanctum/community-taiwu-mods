namespace Wanxiang.Guanxiangtai.McpServerRuntime;

public sealed class McpServerEndpoint
{
    public string Host { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int Port { get; set; }

    public int ProcessId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public string ExecutablePath { get; set; } = string.Empty;
}
