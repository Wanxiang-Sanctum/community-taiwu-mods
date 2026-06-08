namespace Xiangshu.Ipc;

public sealed class XiangshuIpcPingRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class XiangshuIpcPingResponse
{
    public string Side { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
