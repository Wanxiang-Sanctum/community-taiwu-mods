using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Agent;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Frontend.HotKeys;
using Wanxiang.Xiangshu.Frontend.Ipc;
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
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private FrontendIpcServer? _ipcServer;
    private McpSidecar? _mcpSidecar;
    private AgentCliLauncher? _agentCliLauncher;
    private ChatParticipantIdentity? _chatParticipantIdentity;
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
            IpcEndpointRegistry.ConfigureForWorkingDirectory(CurrentAgentSettings.WorkingDirectory);
            StartFrontendIpcServer();
            _agentCliLauncher = new AgentCliLauncher();
            _chatParticipantIdentity = new ChatParticipantIdentity();
            _chatSession = new AgentChatSession(
                _agentCliLauncher,
                () => CurrentAgentSettings,
                ChatParticipantIdentity.AssistantName);
            _chatWindow = XiangshuChatWindow.Create(_chatSession, _chatParticipantIdentity);
            InstallChatHotkey();

            StartMcpSidecar(CurrentAgentSettings);
            Log.Info(
                "frontend plugin initialized",
                new
                {
                    adapter = CurrentAgentSettings.Adapter,
                    workingDirectory = CurrentAgentSettings.WorkingDirectory,
                });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "frontend plugin initialization failed");
            throw;
        }
    }

    public override void OnModSettingUpdate()
    {
        Log.Info("frontend settings updated; restart the game to apply Wanxiang.Xiangshu runtime settings.");
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
        _chatParticipantIdentity?.Dispose();
        _chatParticipantIdentity = null;
        _agentCliLauncher?.Dispose();
        _agentCliLauncher = null;
        _mcpSidecar?.Dispose();
        _mcpSidecar = null;
        _ipcServer?.Dispose();
        _ipcServer = null;
        CurrentAgentSettings = null;
    }

    private void StartMcpSidecar(AgentSettings settings)
    {
        _mcpSidecar?.Dispose();
        _mcpSidecar = new McpSidecar(
            settings.ModDirectory,
            settings.WorkingDirectory,
            IpcEndpointRegistry.ManifestPath);

        try
        {
            McpSidecarLaunch launch = _mcpSidecar.Start();
            Log.Info(
                "MCP sidecar started",
                new
                {
                    processId = launch.ProcessId,
                    logDirectory = launch.LogDirectory,
                    eventLog = launch.EventLogPath,
                });
        }
        catch (Exception ex) when (ex is FileNotFoundException
            or Win32Exception
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            _mcpSidecar.Dispose();
            _mcpSidecar = null;
            Log.Error(ex, "MCP sidecar failed to start");
        }
    }

    private void StartFrontendIpcServer()
    {
        _ipcServer?.Dispose();
        _ipcServer = new FrontendIpcServer();
        IpcEndpoint endpoint = _ipcServer.Start();
        Log.Info(
            "frontend IPC listening",
            new
            {
                endpoint = IpcRuntime.FormatEndpointAddress(endpoint),
                processId = endpoint.ProcessId,
                manifest = IpcEndpointRegistry.ManifestPath,
            });
    }

    private void InstallChatHotkey()
    {
        XiangshuHotKeys.RegisterWithGameCommandKit();
        FrontendHotkeyBridge.Attach(this);
        _harmony = new Harmony("Wanxiang.Xiangshu.Frontend");
        XiangshuHotKeys.PatchViewBottomUpdate(_harmony);
        Log.Info("frontend ViewBottom chat hotkey patch installed");
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
        Log.Info(message);
    }
}
