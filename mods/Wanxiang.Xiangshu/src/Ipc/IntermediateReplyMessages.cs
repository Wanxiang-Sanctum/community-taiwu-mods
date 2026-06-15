using MessagePack;

namespace Wanxiang.Xiangshu.Ipc;

[MessagePackObject]
public sealed class IpcIntermediateReplyRequest(string content)
{
    [Key(0)]
    public string Content { get; } =
        content ?? throw new ArgumentNullException(nameof(content));
}

[MessagePackObject]
public sealed class IpcIntermediateReplyResponse(bool delivered = true)
{
    [Key(0)]
    public bool Delivered { get; } = delivered;
}
