using MessagePack;
using MessagePack.Resolvers;

namespace Wanxiang.Xiangshu.Ipc;

public static class XiangshuMessagePack
{
    public static MessagePackSerializerOptions Options { get; } =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                XiangshuIpcMessagePackResolver.Instance,
                // MessagePipe exposes its IPC frame formatters through formatter attributes.
                AttributeFormatterResolver.Instance,
                BuiltinResolver.Instance));
}

[GeneratedMessagePackResolver]
internal sealed partial class XiangshuIpcMessagePackResolver;
