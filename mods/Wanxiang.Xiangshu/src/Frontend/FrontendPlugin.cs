using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;
using Wanxiang.Xiangshu.Ipc;

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
        try
        {
            CurrentAgentSettings = AgentSettings.Load(ModIdStr);
            IpcEndpointRegistry.ConfigureForModDirectory(CurrentAgentSettings.ModDirectory);
            _ipcServer = new FrontendIpcServer();
            IpcEndpoint endpoint = _ipcServer.Start();
            LogInfo(
                $"frontend IPC listening at {IpcRuntime.FormatEndpointAddress(endpoint)}; pid={endpoint.ProcessId}; manifest={IpcEndpointRegistry.ManifestPath}.");
            _agentCliLauncher = new AgentCliLauncher();
            InstallAgentDiagnosticHotkey();

            StartMcpServer(CurrentAgentSettings);
            LogInfo(
                $"frontend plugin initialized; adapter={CurrentAgentSettings.Adapter}; workingDirectory={CurrentAgentSettings.WorkingDirectory}; debugMode={CurrentAgentSettings.DebugModeEnabled}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Wanxiang.Xiangshu frontend plugin initialization failed: {ex}");
            throw;
        }
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
        _mcpServerProcess = new McpSidecarProcess(
            settings.ModDirectory,
            settings.WorkingDirectory,
            IpcEndpointRegistry.ManifestPath);

        try
        {
            McpSidecarStartResult result = _mcpServerProcess.Start(settings.DebugModeEnabled);
            LogInfo(
                $"MCP sidecar started; pid={result.ProcessId}; logs={result.LogDirectory}; stdout={result.StdoutPath}; stderr={result.StderrPath}; events={result.EventLogPath}.");
        }
        catch (Exception ex) when (ex is FileNotFoundException
            or Win32Exception
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            _mcpServerProcess.Dispose();
            _mcpServerProcess = null;
            Debug.LogError($"Wanxiang.Xiangshu MCP sidecar failed to start: {ex}");
        }
    }

    private void InstallAgentDiagnosticHotkey()
    {
        XiangshuHotKeys.RegisterWithGameCommandKit();
        FrontendHotkeyBridge.Attach(this);
        _harmony = new Harmony("Wanxiang.Xiangshu.Frontend");
        XiangshuHotKeys.PatchViewBottomUpdate(_harmony);
        LogInfo("frontend ViewBottom hotkey patch installed.");
    }

    internal void LaunchAgentDiagnostic()
    {
        AgentSettings? settings = CurrentAgentSettings;

        if (settings is null || _agentCliLauncher is null)
        {
            throw new InvalidOperationException("Wanxiang.Xiangshu frontend plugin is not initialized.");
        }

        (bool _, string message) = _agentCliLauncher.TryStartDiagnostic(settings);
        LogInfo(message);
    }

    private static void LogInfo(string message)
    {
        Debug.Log("Wanxiang.Xiangshu " + message);
    }
}
