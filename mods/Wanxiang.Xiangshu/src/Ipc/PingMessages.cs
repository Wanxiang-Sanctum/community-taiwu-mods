namespace Wanxiang.Xiangshu.Ipc;

public sealed class IpcPingRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class IpcPingResponse
{
    public string Side { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
