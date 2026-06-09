using System.Diagnostics.CodeAnalysis;
using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Xiangshu.Frontend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Wanxiang.Xiangshu.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private FrontendIpcServer? _ipcServer;
    private McpSidecarProcess? _mcpServerProcess;

    internal AgentSettings? CurrentAgentSettings { get; private set; }

    public override void Initialize()
    {
        CurrentAgentSettings = AgentSettings.Load(ModIdStr);
        _ipcServer = new FrontendIpcServer();
        _ipcServer.Start();

        StartMcpServer(CurrentAgentSettings);
    }

    public override void OnModSettingUpdate()
    {
        AgentSettings nextSettings = AgentSettings.Load(ModIdStr);
        bool shouldRestartMcpServer =
            CurrentAgentSettings?.DebugModeEnabled != nextSettings.DebugModeEnabled;

        CurrentAgentSettings = nextSettings;

        if (shouldRestartMcpServer && _mcpServerProcess is not null)
        {
            StartMcpServer(nextSettings);
        }
    }

    public override void Dispose()
    {
        _mcpServerProcess?.Dispose();
        _mcpServerProcess = null;
        _ipcServer?.Dispose();
        _ipcServer = null;
        CurrentAgentSettings = null;
    }

    private void StartMcpServer(AgentSettings settings)
    {
        _mcpServerProcess?.Dispose();
        _mcpServerProcess = new McpSidecarProcess();
        _mcpServerProcess.Start(settings.DebugModeEnabled);
    }
}
