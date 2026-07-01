using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class McpToolJson
{
    public static JsonSerializerOptions SerializerOptions { get; } =
        CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(McpJsonUtilities.DefaultOptions);
        options.TypeInfoResolverChain.Insert(0, McpToolJsonContext.Default);
        options.Converters.Insert(
            0,
            new JsonStringEnumConverter<McpScriptEntryThread>(allowIntegerValues: false));
        options.Converters.Insert(
            0,
            new JsonStringEnumConverter<McpPluginSide>(allowIntegerValues: false));
        options.Converters.Insert(
            0,
            new JsonStringEnumConverter<McpTaiwuStopMethod>(allowIntegerValues: false));
        return options;
    }
}

[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(McpPluginSide))]
[JsonSerializable(typeof(McpScriptEntryThread))]
[JsonSerializable(typeof(McpTaiwuStopMethod))]
internal sealed partial class McpToolJsonContext : JsonSerializerContext;
