using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Wanxiang.Xiangshu.Ipc;

[DataContract]
public sealed class IpcExecuteScriptRequest
{
    [DataMember(Order = 0)]
    public string Script { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public Dictionary<string, string> Arguments { get; set; } = [];
}

[DataContract]
public sealed class IpcExecuteScriptResponse
{
    [DataMember(Order = 0)]
    public string ReturnValueJson { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public string Error { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public Collection<string> Diagnostics { get; set; } = [];
}
