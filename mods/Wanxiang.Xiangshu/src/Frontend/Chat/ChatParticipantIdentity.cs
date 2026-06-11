using CharacterDataMonitor;

namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class ChatParticipantIdentity : IDisposable
{
    public const string AssistantName = "相枢";

    private BasicInfoMonitor? _playerMonitor;
    private int _playerCharacterId = -1;
    private bool _disposed;

    public event Action? PlayerNameChanged;

    public bool IsPlayerNameReady => !string.IsNullOrWhiteSpace(PlayerName);

    public string? PlayerName { get; private set; }

    public void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        int characterId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;

        if (_playerMonitor is null || _playerCharacterId != characterId)
        {
            BindPlayerMonitor(characterId);
        }

        UpdatePlayerNameFromMonitor();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UnbindPlayerMonitor();
        PlayerNameChanged = null;
    }

    private void BindPlayerMonitor(int characterId)
    {
        UnbindPlayerMonitor();
        _playerCharacterId = characterId;

        _playerMonitor = SingletonObject.getInstance<CharacterMonitorModel>()
            .GetMonitorItem<BasicInfoMonitor>(characterId);
        _playerMonitor.AddNameDataListener(UpdatePlayerNameFromMonitor);
    }

    private void UnbindPlayerMonitor()
    {
        _playerMonitor?.RemoveNameDataListener(UpdatePlayerNameFromMonitor);

        _playerMonitor = null;
        _playerCharacterId = -1;
    }

    private void UpdatePlayerNameFromMonitor()
    {
        BasicInfoMonitor? monitor = _playerMonitor;

        if (monitor?.Init != true)
        {
            SetPlayerName(null);
            return;
        }

        string realName = NameCenter.GetRealName(ref monitor.NameRelatedData);
        SetPlayerName(realName);
    }

    private void SetPlayerName(string? value)
    {
        string? normalized = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

        if (string.Equals(PlayerName, normalized, StringComparison.Ordinal))
        {
            return;
        }

        PlayerName = normalized;
        PlayerNameChanged?.Invoke();
    }
}
