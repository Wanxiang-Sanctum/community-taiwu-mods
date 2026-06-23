using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FrameWork;
using Wanxiang.Taiwu.Logging;
using Wanxiang.Xiangshu.Frontend.Agent;
using Wanxiang.Xiangshu.Frontend.Agent.Cli;

namespace Wanxiang.Xiangshu.Frontend.Chat;

[SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "AgentCliLauncher is owned and disposed by FrontendPlugin; this runtime only borrows it.")]
internal sealed class CurrentWorldChatSession(
    AgentCliLauncher agentCliLauncher,
    Func<AgentSettings?> settingsProvider,
    ChatParticipantIdentity participants) : IDisposable
{
    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly AgentCliLauncher _agentCliLauncher = agentCliLauncher
        ?? throw new ArgumentNullException(nameof(agentCliLauncher));
    private readonly Func<AgentSettings?> _settingsProvider = settingsProvider
        ?? throw new ArgumentNullException(nameof(settingsProvider));
    private readonly XiangshuChatWindow _window = XiangshuChatWindow.Create(participants
        ?? throw new ArgumentNullException(nameof(participants)));
    private AgentChatSession? _session;
    private uint? _worldId;
    private bool _disposed;

    public CurrentChatSessionBinding IpcBinding { get; } = new();

    public bool IsWindowVisible => _window.IsVisible;

    public void ToggleWindow()
    {
        ThrowIfDisposed();

        if (_window.IsVisible)
        {
            _window.SetVisible(visible: false);
            return;
        }

        if (EnsureBoundToActiveWorld())
        {
            _window.SetVisible(visible: true);
        }
    }

    public void OpenWindow()
    {
        ThrowIfDisposed();

        if (EnsureBoundToActiveWorld())
        {
            _window.SetVisible(visible: true);
        }
    }

    public bool RequestRuntimeInterrupt(string content)
    {
        ThrowIfDisposed();

        return GetBoundSession()?.RequestRuntimeInterrupt(content) == true;
    }

    public void Clear(bool hideWindow)
    {
        ThrowIfDisposed();
        ClearCore(hideWindow);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearCore(hideWindow: true);
        _window.DestroyWindow();
        _disposed = true;
    }

    private bool EnsureBoundToActiveWorld()
    {
        if (!TryGetActiveWorldId(out uint worldId))
        {
            Log.Warning("chat session unavailable because no active game world is loaded");
            return false;
        }

        if (_session is not null && _worldId == worldId)
        {
            return true;
        }

        AgentSettings settings =
            _settingsProvider()
            ?? throw new InvalidOperationException("Agent settings are not initialized.");

        ClearCore(hideWindow: false);

        AgentChatSession session = new(
            _agentCliLauncher,
            _settingsProvider,
            new AgentChatSessionStore(settings.WorkingDirectory, worldId),
            settings.Adapter,
            ChatParticipantIdentity.AssistantName);

        _session = session;
        _worldId = worldId;
        IpcBinding.Bind(session);
        _window.BindSession(session);

        Log.Info(
            "chat session bound to active world",
            new
            {
                worldId = FormatWorldId(worldId),
            });

        return true;
    }

    private void ClearCore(bool hideWindow)
    {
        if (hideWindow)
        {
            _window.SetVisible(visible: false);
        }

        _window.BindSession(null);
        IpcBinding.Clear();

        AgentChatSession? session = _session;
        _session = null;
        _worldId = null;
        session?.Dispose();
    }

    private AgentChatSession? GetBoundSession()
    {
        if (_session is null
            || _worldId is not { } boundWorldId
            || !TryGetActiveWorldId(out uint activeWorldId)
            || boundWorldId != activeWorldId)
        {
            return null;
        }

        return _session;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CurrentWorldChatSession));
        }
    }

    private static bool TryGetActiveWorldId(out uint worldId)
    {
        worldId = default;

        if (GameApp.Instance is null
            || GameApp.Instance.GetCurrentGameStateName() != EGameState.InGame)
        {
            return false;
        }

        worldId = SingletonObject.getInstance<BasicGameData>().WorldId;
        return true;
    }

    private static string FormatWorldId(uint worldId)
    {
        return "0x" + worldId.ToString("x8", CultureInfo.InvariantCulture);
    }
}
