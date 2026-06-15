using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Wanxiang.Xiangshu.Ipc;

[DataContract]
public sealed class IpcRunScriptRequest
{
    [DataMember(Order = 0)]
    public string Script { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public Dictionary<string, string> Arguments { get; set; } = [];
}

[DataContract]
public sealed class IpcRunScriptResponse
{
    [DataMember(Order = 0)]
    public string ReturnValueJson { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public string Error { get; set; } = string.Empty;

    [SuppressMessage(
        "Design",
        "CA1002:Do not expose generic lists",
        Justification = "List<T> avoids MessagePack's trimmed dynamic Collection<T> formatter in the MCP sidecar.")]
    [DataMember(Order = 2)]
    public List<string> Diagnostics { get; set; } = [];
}
