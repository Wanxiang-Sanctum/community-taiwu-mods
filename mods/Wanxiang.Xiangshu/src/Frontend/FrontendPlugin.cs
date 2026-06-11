using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Xiangshu.Frontend.Agent;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Frontend.HotKeys;
using Wanxiang.Xiangshu.Frontend.Ipc;
using Wanxiang.Xiangshu.Frontend.Logging;
using Wanxiang.Xiangshu.Frontend.Sidecar;
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
    private AgentChatSession? _chatSession;
    private XiangshuChatWindow? _chatWindow;
    private Harmony? _harmony;

    internal AgentSettings? CurrentAgentSettings { get; private set; }

    internal bool IsChatWindowVisible => _chatWindow?.IsVisible == true;

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
            _chatSession = new AgentChatSession(_agentCliLauncher, () => CurrentAgentSettings);
            _chatWindow = XiangshuChatWindow.Create(_chatSession);
            InstallChatHotkey();

            StartMcpServer(CurrentAgentSettings);
            LogInfo(
                $"frontend plugin initialized; adapter={CurrentAgentSettings.Adapter}; workingDirectory={CurrentAgentSettings.WorkingDirectory}.");
        }
        catch (Exception ex)
        {
            XiangshuFrontendLog.Error("frontend plugin initialization failed: " + ex);
            throw;
        }
    }

    public override void OnModSettingUpdate()
    {
        CurrentAgentSettings = AgentSettings.Load(ModIdStr);
    }

    public override void Dispose()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        FrontendHotkeyBridge.Detach(this);
        _chatWindow?.DestroyWindow();
        _chatWindow = null;
        _chatSession?.Dispose();
        _chatSession = null;
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
            McpSidecarStartResult result = _mcpServerProcess.Start();
            LogInfo(
                $"MCP sidecar started; pid={result.ProcessId}; logs={result.LogDirectory}; eventLog={result.EventLogPath}.");
        }
        catch (Exception ex) when (ex is FileNotFoundException
            or Win32Exception
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            _mcpServerProcess.Dispose();
            _mcpServerProcess = null;
            XiangshuFrontendLog.Error("MCP sidecar failed to start: " + ex);
        }
    }

    private void InstallChatHotkey()
    {
        XiangshuHotKeys.RegisterWithGameCommandKit();
        FrontendHotkeyBridge.Attach(this);
        _harmony = new Harmony("Wanxiang.Xiangshu.Frontend");
        XiangshuHotKeys.PatchViewBottomUpdate(_harmony);
        LogInfo("frontend ViewBottom chat hotkey patch installed.");
    }

    internal void ToggleChatWindow()
    {
        if (_chatWindow is null)
        {
            throw new InvalidOperationException("Wanxiang.Xiangshu chat window is not initialized.");
        }

        _chatWindow.Toggle();
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
        XiangshuFrontendLog.Info(message);
    }
}
