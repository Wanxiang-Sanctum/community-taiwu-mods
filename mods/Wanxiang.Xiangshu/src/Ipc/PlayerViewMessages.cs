using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace Wanxiang.Xiangshu.Ipc;

[MessagePackObject]
public sealed class IpcCapturePlayerViewRequest;

[MessagePackObject]
public sealed class IpcCapturePlayerViewResponse(byte[] pngBytes)
{
    [Key(0)]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "MessagePack serializes PNG binary payloads directly as byte arrays.")]
    public byte[] PngBytes { get; } =
        pngBytes ?? throw new ArgumentNullException(nameof(pngBytes));
}
