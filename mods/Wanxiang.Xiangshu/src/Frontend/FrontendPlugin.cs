using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using TaiwuModdingLib.Core.Plugin;
using SharedInventoryGrafts = Wanxiang.Taiwu.ItemGrafts.Frontend.InventoryGrafts;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Agent;
using Wanxiang.Xiangshu.Frontend.Agent.Cli;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Frontend.HotKeys;
using Wanxiang.Xiangshu.Frontend.Ipc;
using Wanxiang.Xiangshu.Frontend.ItemGrafts;
using Wanxiang.Xiangshu.Frontend.Mcp;
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
    private const string PluginDirectoryName = "Frontend";
    private const string HostLeftInventoryInterruptMessage = "药钵离囊，声息骤断。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private FrontendIpcServer? _ipcServer;
    private McpSidecar? _mcpSidecar;
    private AgentCliLauncher? _agentCliLauncher;
    private McpBearerToken? _mcpBearerToken;
    private ChatParticipantIdentity? _chatParticipantIdentity;
    private AgentChatSession? _chatSession;
    private XiangshuChatWindow? _chatWindow;
    private FrontendHotkeyDriver? _hotkeyDriver;
    private GraftHostSync? _graftHostSync;

    internal AgentSettings? CurrentAgentSettings { get; private set; }

    internal bool IsChatWindowVisible => _chatWindow?.IsVisible == true;

    public override void Initialize()
    {
        try
        {
            CurrentAgentSettings = AgentSettings.Load(ModIdStr);
            IpcEndpointRegistry.ConfigureForWorkingDirectory(CurrentAgentSettings.WorkingDirectory);
            _mcpBearerToken = McpBearerToken.Create();
            _agentCliLauncher = new AgentCliLauncher(_mcpBearerToken);
            _chatParticipantIdentity = new ChatParticipantIdentity();
            _chatSession = new AgentChatSession(
                _agentCliLauncher,
                () => CurrentAgentSettings,
                new AgentChatSessionStore(CurrentAgentSettings.WorkingDirectory),
                CurrentAgentSettings.Adapter,
                ChatParticipantIdentity.AssistantName);
            _chatWindow = XiangshuChatWindow.Create(_chatSession, _chatParticipantIdentity);
            InstallChatHotkey();
            InstallItemGraft();
            StartFrontendIpcServer(_chatSession, CurrentAgentSettings.ModDirectory);

            StartMcpSidecar(CurrentAgentSettings);
            Log.Info(
                "frontend plugin initialized",
                new
                {
                    adapter = CurrentAgentSettings.Adapter,
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
        _graftHostSync?.Dispose();
        _graftHostSync = null;
        XiangshuGraftState.Reset();
        _ = SharedInventoryGrafts.Uninstall();
        _hotkeyDriver?.Dispose();
        _hotkeyDriver = null;
        _chatWindow?.DestroyWindow();
        _chatWindow = null;
        _chatSession?.Dispose();
        _chatSession = null;
        _chatParticipantIdentity?.Dispose();
        _chatParticipantIdentity = null;
        _agentCliLauncher?.Dispose();
        _agentCliLauncher = null;
        _mcpBearerToken = null;
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
            IpcEndpointRegistry.ManifestPath,
            _mcpBearerToken ?? throw new InvalidOperationException("MCP bearer token is not initialized."));

        try
        {
            _mcpSidecar.Start();
            Log.Info("MCP sidecar started");
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

    private void StartFrontendIpcServer(
        AgentChatSession chatSession,
        string modDirectory)
    {
        _ipcServer?.Dispose();
        _ipcServer = new FrontendIpcServer(
            chatSession,
            XiangshuRuntimePaths.GetPluginDirectory(modDirectory, PluginDirectoryName));
        _ = _ipcServer.Start();
        Log.Info("frontend IPC ready");
    }

    private void InstallChatHotkey()
    {
        XiangshuHotKeys.RegisterWithGameCommandKit();
        _hotkeyDriver?.Dispose();
        _hotkeyDriver = FrontendHotkeyDriver.Create(this);
    }

    private void InstallItemGraft()
    {
        SharedInventoryGrafts.Install(this);
        XiangshuGraftState.Configure(OpenChatWindow);
        _graftHostSync?.Dispose();
        _graftHostSync = GraftHostSync.Create(OnHostLeftTaiwuInventory);
    }

    internal void ToggleChatWindow()
    {
        if (_chatWindow is null)
        {
            throw new InvalidOperationException("Wanxiang.Xiangshu chat window is not initialized.");
        }

        _chatWindow.Toggle();
    }

    internal void OpenChatWindow()
    {
        if (_chatWindow is null)
        {
            throw new InvalidOperationException("Wanxiang.Xiangshu chat window is not initialized.");
        }

        _chatWindow.SetVisible(visible: true);
    }

    private void OnHostLeftTaiwuInventory()
    {
        _ = _chatSession?.RequestRuntimeInterrupt(HostLeftInventoryInterruptMessage);
    }
}
