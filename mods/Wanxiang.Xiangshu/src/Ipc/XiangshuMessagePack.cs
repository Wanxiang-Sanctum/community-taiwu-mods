using MessagePack;
using MessagePack.Resolvers;

namespace Wanxiang.Xiangshu.Ipc;

public static class XiangshuMessagePack
{
    public static MessagePackSerializerOptions Options { get; } =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                XiangshuIpcMessagePackResolver.Instance,
                BuiltinResolver.Instance,
                DynamicGenericResolver.Instance));
}

[GeneratedMessagePackResolver]
internal sealed partial class XiangshuIpcMessagePackResolver;
