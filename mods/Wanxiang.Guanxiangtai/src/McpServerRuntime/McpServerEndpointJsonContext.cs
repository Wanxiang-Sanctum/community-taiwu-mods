#if NET6_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wanxiang.Guanxiangtai.McpServerRuntime;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(McpServerEndpointFile))]
internal sealed partial class McpServerEndpointJsonContext : JsonSerializerContext;
#endif
