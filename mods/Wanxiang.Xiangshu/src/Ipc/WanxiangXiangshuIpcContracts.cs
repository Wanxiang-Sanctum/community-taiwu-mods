namespace Wanxiang.Xiangshu.Ipc;

public sealed class WanxiangXiangshuIpcPingRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class WanxiangXiangshuIpcPingResponse
{
    public string Side { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
