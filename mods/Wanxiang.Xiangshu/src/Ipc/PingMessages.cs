using System.Runtime.Serialization;

namespace Wanxiang.Xiangshu.Ipc;

[DataContract]
public sealed class IpcPingRequest
{
    [DataMember(Order = 0)]
    public string Message { get; set; } = string.Empty;
}

[DataContract]
public sealed class IpcPingResponse
{
    [DataMember(Order = 0)]
    public string Side { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public string Message { get; set; } = string.Empty;
}

[DataContract]
public sealed class IpcIntermediateReplyRequest
{
    [DataMember(Order = 0)]
    public string Content { get; set; } = string.Empty;
}

[DataContract]
public sealed class IpcIntermediateReplyResponse;
