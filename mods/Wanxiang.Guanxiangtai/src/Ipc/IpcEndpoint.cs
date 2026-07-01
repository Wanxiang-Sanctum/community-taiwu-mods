namespace Wanxiang.Guanxiangtai.Ipc;

public sealed class IpcEndpoint
{
    public string Role { get; set; } = string.Empty;

    public string Transport { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public DateTimeOffset StartedAt { get; set; }
}
