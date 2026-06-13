using System.Runtime.Serialization;

namespace Wanxiang.Xiangshu.Ipc;

[DataContract]
public sealed class IpcIntermediateReplyRequest
{
    [DataMember(Order = 0)]
    public string Content { get; set; } = string.Empty;
}

[DataContract]
public sealed class IpcIntermediateReplyResponse;
