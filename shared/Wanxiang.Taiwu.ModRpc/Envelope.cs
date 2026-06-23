namespace Wanxiang.Taiwu.ModRpc;

internal sealed class Envelope
{
    public string Protocol { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public bool ExpectsResponse { get; set; }

    public string? Error { get; set; }
}
