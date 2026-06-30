namespace Wanxiang.Guanxiangtai.Ipc;

internal sealed class IpcEndpointManifest
{
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<IpcEndpoint> Endpoints { get; set; } = [];
}
