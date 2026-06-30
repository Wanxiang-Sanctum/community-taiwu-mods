#if NET10_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wanxiang.Guanxiangtai.Ipc;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(IpcEndpointManifest))]
internal sealed partial class IpcEndpointJsonContext : JsonSerializerContext;
#endif
