using MessagePack;

namespace Wanxiang.Xiangshu.Ipc;

[MessagePackObject]
public sealed class IpcIntermediateReplyRequest(string content)
{
    [Key(0)]
    public string Content { get; } =
        content ?? throw new ArgumentNullException(nameof(content));
}
