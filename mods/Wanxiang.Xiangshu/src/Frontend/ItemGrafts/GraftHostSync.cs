using Config;
using Cysharp.Threading.Tasks;
using FrameWork;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using Wanxiang.Taiwu.AsyncInterop;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Frontend;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal sealed class GraftHostSync : IDisposable
{
    private const string AttachNotification = "相枢藏进了陶土药钵。";
    private const string CreateNotification = "低语的陶土药钵落入了行囊。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly Action _onHostLeftTaiwuInventory;
    private bool _disposed;
    private bool _syncing;
    private bool _syncRequested;
    private int _syncGeneration;
    private GraftSession? _currentSession;

    public static GraftHostSync Create(Action onHostLeftTaiwuInventory)
    {
        GraftHostSync sync = new(onHostLeftTaiwuInventory);
        GEvent.Add(EEvents.OnGameStateChange, sync.OnGameStateChange);

        sync.RequestSync();

        return sync;
    }

    private GraftHostSync(Action onHostLeftTaiwuInventory)
    {
        _onHostLeftTaiwuInventory = onHostLeftTaiwuInventory
            ?? throw new ArgumentNullException(nameof(onHostLeftTaiwuInventory));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GEvent.Remove(EEvents.OnGameStateChange, OnGameStateChange);
        ClearCurrentSession();
    }

    private void RequestSync()
    {
        if (_disposed
            || !TryGetTaiwuCharId(out int taiwuCharId))
        {
            return;
        }

        if (_syncing)
        {
            _syncRequested = true;
            return;
        }

        _syncRequested = false;
        SyncGraftAsync(taiwuCharId, _syncGeneration).Forget();
    }

    private void ResetState()
    {
        _syncGeneration++;
        _syncRequested = false;
        ClearCurrentSession();
    }

    private void OnGameStateChange(ArgumentBox argBox)
    {
        if (!argBox.Get("newState", out Enum newState))
        {
            return;
        }

        if ((EGameState)(object)newState != EGameState.InGame)
        {
            ResetState();
            return;
        }

        RequestSync();
    }

    private void OnHostEvent(GraftHostEventArgs hostEvent)
    {
        HandleHostEventAsync(hostEvent).Forget();
    }

    private async UniTask HandleHostEventAsync(GraftHostEventArgs hostEvent)
    {
        await UniTask.SwitchToMainThread();

        if (_disposed
            || _currentSession is null
            || _currentSession.Graft.HostId != hostEvent.HostId)
        {
            return;
        }

        if (hostEvent is GraftHostRemovedEventArgs removed)
        {
            NotifyHostRemoved(removed.HostKey);
        }
        else if (hostEvent is GraftHostLocationChangedEventArgs locationChanged)
        {
            NotifyHostLocationChanged(locationChanged);
        }
        else if (hostEvent is GraftHostDataChangedEventArgs)
        {
            RequestSync();
        }
    }

    private void NotifyHostRemoved(ItemKey hostKey)
    {
        if (_disposed)
        {
            return;
        }

        GraftSession? session = _currentSession;

        if (!XiangshuGraftState.ClearIfHost(
                hostKey,
                out bool wasInTaiwuInventory))
        {
            return;
        }

        _currentSession = null;

        if (session?.IsActive == true)
        {
            DisposeSessionSafelyAsync(session).Forget();
        }

        if (wasInTaiwuInventory)
        {
            _onHostLeftTaiwuInventory();
        }

        RequestSync();
    }

    private void NotifyHostLocationChanged(GraftHostLocationChangedEventArgs hostEvent)
    {
        if (_disposed || !TryGetTaiwuCharId(out int taiwuCharId))
        {
            return;
        }

        bool fromTaiwuInventory = hostEvent.FromCharacterId == taiwuCharId;
        bool toTaiwuInventory = hostEvent.ToCharacterId == taiwuCharId;

        if (fromTaiwuInventory == toTaiwuInventory
            || !XiangshuGraftState.SetHostInTaiwuInventory(
                hostEvent.HostKey,
                isInTaiwuInventory: toTaiwuInventory))
        {
            return;
        }

        if (!toTaiwuInventory)
        {
            _onHostLeftTaiwuInventory();
        }

        RequestSync();
    }

    private async UniTask SyncGraftAsync(
        int taiwuCharId,
        int syncGeneration)
    {
        _syncing = true;

        try
        {
            GraftHostTemplate hostTemplate = CreateBowlHostTemplate();

            if (XiangshuGraftState.TryGetHost(out ItemKey currentHost))
            {
                bool currentInInventory = await InventoryContainsItemAsync(
                    taiwuCharId,
                    currentHost);

                if (!IsCurrentSync(taiwuCharId, syncGeneration))
                {
                    return;
                }

                if (currentInInventory)
                {
                    _ = XiangshuGraftState.SetHostInTaiwuInventory(
                        currentHost,
                        isInTaiwuInventory: true);
                    return;
                }

                if (XiangshuGraftState.SetHostInTaiwuInventory(
                        currentHost,
                        isInTaiwuInventory: false))
                {
                    _onHostLeftTaiwuInventory();
                }

                bool currentExists = await ItemExistsAsync(currentHost);

                if (!IsCurrentSync(taiwuCharId, syncGeneration))
                {
                    return;
                }

                if (currentExists)
                {
                    return;
                }

                ClearCurrentSession();
            }

            IReadOnlyList<ItemKey> medicineBowls = await GetMedicineBowlsAsync(taiwuCharId);

            if (!IsCurrentSync(taiwuCharId, syncGeneration))
            {
                return;
            }

            ItemKey existingHost = SelectOldestHost(medicineBowls);
            GraftDefinition definition = XiangshuGraftState.CreateDefinition();

            GraftSession session = existingHost.IsValid()
                ? await InventoryGrafts.AttachAsync(
                    existingHost,
                    definition,
                    new AttachmentOptions
                    {
                        NotificationMessage = AttachNotification,
                        OnHostEvent = OnHostEvent,
                    })
                : await InventoryGrafts.CreateAsync(
                    taiwuCharId,
                    hostTemplate,
                    definition,
                    new CreationOptions
                    {
                        NotificationMessage = CreateNotification,
                        OnHostEvent = OnHostEvent,
                    });

            if (!IsCurrentSync(taiwuCharId, syncGeneration))
            {
                await DisposeSessionSafelyAsync(session);
                return;
            }

            SetCurrentSession(
                session,
                isInTaiwuInventory: true);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Error(ex, "failed to sync Xiangshu item graft");
        }
        finally
        {
            _syncing = false;

            if (_syncRequested)
            {
                RequestSync();
            }
        }
    }

    private void SetCurrentSession(
        GraftSession session,
        bool isInTaiwuInventory)
    {
        GraftSession? previousSession = _currentSession;
        _currentSession = session ?? throw new ArgumentNullException(nameof(session));
        XiangshuGraftState.SetCurrent(
            session.Graft,
            isInTaiwuInventory);

        if (previousSession is not null
            && !ReferenceEquals(previousSession, session))
        {
            DisposeSessionSafelyAsync(previousSession).Forget();
        }
    }

    private void ClearCurrentSession()
    {
        GraftSession? session = _currentSession;
        _currentSession = null;
        XiangshuGraftState.ClearCurrent();

        if (session?.IsActive == true)
        {
            DisposeSessionSafelyAsync(session).Forget();
        }
    }

    private static async UniTask DisposeSessionSafelyAsync(GraftSession session)
    {
        try
        {
            await session.DisposeAsync();
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Warning(
                "failed to dispose Xiangshu item graft session",
                new
                {
                    exceptionType = ex.GetType().FullName,
                    exceptionMessage = ex.Message,
                    exception = ex.ToString(),
                });
        }
    }

    private bool IsCurrentSync(
        int taiwuCharId,
        int syncGeneration)
    {
        return !_disposed
            && _syncGeneration == syncGeneration
            && TryGetTaiwuCharId(out int currentTaiwuCharId)
            && currentTaiwuCharId == taiwuCharId;
    }

    private static UniTask<bool> InventoryContainsItemAsync(
        int taiwuCharId,
        ItemKey hostKey)
    {
        return TaiwuAsyncCall.InvokeAsync<bool>(
            callback => CharacterDomainMethod.AsyncCall.InventoryContainsItem(
                null,
                taiwuCharId,
                hostKey,
                callback.Invoke));
    }

    private static async UniTask<bool> ItemExistsAsync(ItemKey hostKey)
    {
        GraftHostId hostId = new(hostKey);
        ItemDisplayData? item = await TaiwuAsyncCall.InvokeAsync<ItemDisplayData?>(
            callback => ItemDomainMethod.AsyncCall.GetItemDisplayData(
                null,
                hostKey,
                callback.Invoke));

        return hostId.Matches(item?.RealKey ?? ItemKey.Invalid);
    }

    private static async UniTask<IReadOnlyList<ItemKey>> GetMedicineBowlsAsync(int taiwuCharId)
    {
        short itemSubType = ItemTemplateHelper.GetItemSubType(
            GameData.Domains.Item.ItemType.CraftTool,
            CraftTool.DefKey.Medicine0);
        List<ItemDisplayData> inventoryItems = await TaiwuAsyncCall.InvokeAsync<List<ItemDisplayData>>(
            callback => CharacterDomainMethod.AsyncCall.GetInventoryItems(
                null,
                taiwuCharId,
                itemSubType,
                callback.Invoke));
        List<ItemKey> medicineBowls = [];

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            ItemKey key = inventoryItems[i].RealKey;

            if (IsMedicineBowl(key))
            {
                medicineBowls.Add(key);
            }
        }

        return medicineBowls;
    }

    private static ItemKey SelectOldestHost(IReadOnlyList<ItemKey> medicineBowls)
    {
        ItemKey selected = ItemKey.Invalid;

        for (int i = 0; i < medicineBowls.Count; i++)
        {
            ItemKey key = medicineBowls[i];

            if (!selected.IsValid() || key.Id < selected.Id)
            {
                selected = key;
            }
        }

        return selected;
    }

    private static bool IsMedicineBowl(ItemKey key)
    {
        return key.IsValid()
            && key.ItemType == GameData.Domains.Item.ItemType.CraftTool
            && key.TemplateId == CraftTool.DefKey.Medicine0;
    }

    private static GraftHostTemplate CreateBowlHostTemplate()
    {
        return new GraftHostTemplate(
            GameData.Domains.Item.ItemType.CraftTool,
            CraftTool.DefKey.Medicine0);
    }

    private static bool TryGetTaiwuCharId(out int taiwuCharId)
    {
        taiwuCharId = -1;

        if (GameApp.Instance is null
            || GameApp.Instance.GetCurrentGameStateName() != EGameState.InGame)
        {
            return false;
        }

        taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
        return taiwuCharId >= 0;
    }
}
