namespace Wanxiang.Guanxiangtai.McpServer;

internal sealed class PluginRuntimeAvailability(
    PluginSideAvailability frontend,
    PluginSideAvailability backend)
{
    public PluginSideAvailability Frontend { get; } =
        frontend ?? throw new ArgumentNullException(nameof(frontend));

    public PluginSideAvailability Backend { get; } =
        backend ?? throw new ArgumentNullException(nameof(backend));

    public bool IsReady => Frontend.Available && Backend.Available;
}

internal sealed class PluginSideAvailability(
    bool available,
    string? reason)
{
    public bool Available { get; } = available;

    public string? Reason { get; } = reason;
}
