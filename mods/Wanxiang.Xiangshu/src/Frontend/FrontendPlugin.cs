using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

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
    private AgentCliLauncher? _agentCliLauncher;
    private Harmony? _harmony;

    internal AgentSettings? CurrentAgentSettings { get; private set; }

    public override void Initialize()
    {
        CurrentAgentSettings = AgentSettings.Load(ModIdStr);
        _ipcServer = new FrontendIpcServer();
        _ipcServer.Start();
        _agentCliLauncher = new AgentCliLauncher();
        InstallAgentDiagnosticHotkey();

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
        _harmony?.UnpatchSelf();
        _harmony = null;
        FrontendHotkeyBridge.Detach(this);
        _agentCliLauncher?.Dispose();
        _agentCliLauncher = null;
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

    private void InstallAgentDiagnosticHotkey()
    {
        XiangshuHotKeys.RegisterWithGameCommandKit();
        FrontendHotkeyBridge.Attach(this);
        _harmony = new Harmony("Wanxiang.Xiangshu.Frontend");
        _harmony.PatchAll(typeof(FrontendPlugin).Assembly);
    }

    internal void LaunchAgentDiagnostic()
    {
        AgentSettings? settings = CurrentAgentSettings;

        if (settings is null || _agentCliLauncher is null)
        {
            throw new InvalidOperationException("Wanxiang.Xiangshu frontend plugin is not initialized.");
        }

        (bool _, string message) = _agentCliLauncher.TryStartDiagnostic(settings);
        Debug.Log(message);
    }
}
