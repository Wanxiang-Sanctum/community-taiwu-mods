using MessagePack;
using MessagePack.Resolvers;

namespace Wanxiang.Xiangshu.Ipc;

public static class XiangshuMessagePack
{
    public static MessagePackSerializerOptions Options { get; } =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                XiangshuIpcMessagePackResolver.Instance,
                SourceGeneratedFormatterResolver.Instance,
                BuiltinResolver.Instance,
                DynamicGenericResolver.Instance));
}

[global::MessagePack.GeneratedMessagePackResolverAttribute]
internal sealed partial class XiangshuIpcMessagePackResolver;
