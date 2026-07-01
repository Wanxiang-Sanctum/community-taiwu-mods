namespace Wanxiang.Guanxiangtai.McpServerRuntime;

internal sealed class McpServerEndpointFile
{
    public string ModId { get; set; } = GuanxiangtaiMcp.ModId;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<McpServerEndpoint> Servers { get; set; } = [];
}
