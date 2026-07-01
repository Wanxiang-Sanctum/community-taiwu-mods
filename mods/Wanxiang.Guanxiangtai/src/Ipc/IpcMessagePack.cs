using MessagePack;
using MessagePack.Resolvers;

namespace Wanxiang.Guanxiangtai.Ipc;

public static class IpcMessagePack
{
    public static MessagePackSerializerOptions Options { get; } =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                IpcMessagePackResolver.Instance,
                // MessagePipe exposes its IPC frame formatters through formatter attributes.
                AttributeFormatterResolver.Instance,
                BuiltinResolver.Instance));
}

[GeneratedMessagePackResolver]
internal sealed partial class IpcMessagePackResolver;
