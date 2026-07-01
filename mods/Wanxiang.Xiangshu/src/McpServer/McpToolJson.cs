using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;

namespace Wanxiang.Xiangshu.McpServer;

internal static class McpToolJson
{
    public static JsonSerializerOptions SerializerOptions { get; } =
        CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(McpJsonUtilities.DefaultOptions);
        options.Converters.Insert(
            0,
            new JsonStringEnumConverter<McpScriptEntryThread>(allowIntegerValues: false));
        options.Converters.Insert(
            0,
            new JsonStringEnumConverter<McpPluginSide>(allowIntegerValues: false));
        return options;
    }
}
