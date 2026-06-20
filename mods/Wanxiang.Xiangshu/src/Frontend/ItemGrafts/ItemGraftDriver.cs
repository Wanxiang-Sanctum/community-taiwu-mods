using Config;
using Cysharp.Threading.Tasks;
using FrameWork;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.GameDataBridge;
using GameData.Serializer;
using Wanxiang.Taiwu.ItemGrafts;
using Wanxiang.Taiwu.Logging;

namespace Wanxiang.Xiangshu.Frontend.ItemGrafts;

internal sealed class ItemGraftDriver : IDisposable
{
    private const string AttachNotification = "相枢藏进了陶土药钵。";
    private const string CreateNotification = "低语的陶土药钵落入了行囊。";

    private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

    private readonly Action _onHostLeftTaiwuInventory;
    private bool _disposed;
    private bool _ensuring;
    private bool _inventoryReady;
    private bool _ensureRequested;
    private int _stateVersion;
    private int _itemListenerId = -1;
    private ItemKey _monitoredHostKey = ItemKey.Invalid;

    public static ItemGraftDriver Create(Action onHostLeftTaiwuInventory)
    {
        ItemGraftDriver driver = new(onHostLeftTaiwuInventory);
        GEvent.Add(EEvents.OnGameStateChange, driver.OnGameStateChange);
        return driver;
    }

    private ItemGraftDriver(Action onHostLeftTaiwuInventory)
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
        UnmonitorCurrentHost();
        UnregisterItemListener();
        ItemGraftRuntime.ClearCurrent();
    }

    private void RequestEnsure()
    {
        if (_disposed || !TryGetTaiwuCharId(out int taiwuCharId))
        {
            return;
        }

        if (_ensuring)
        {
            _ensureRequested = true;
            return;
        }

        _ensureRequested = false;
        EnsureGraftAsync(taiwuCharId, _stateVersion).Forget();
    }

    private void ResetReadyState()
    {
        _stateVersion++;
        _inventoryReady = false;
        _ensureRequested = false;
        UnmonitorCurrentHost();
        ItemGraftRuntime.ClearCurrent();
    }

    public void NotifyTaiwuInventorySnapshotChanged()
    {
        if (_disposed)
        {
            return;
        }

        _inventoryReady = true;
        _ensureRequested = true;
        RequestEnsure();
    }

    private void OnGameStateChange(ArgumentBox argBox)
    {
        if (!argBox.Get("newState", out Enum newState))
        {
            return;
        }

        if ((EGameState)(object)newState != EGameState.InGame)
        {
            ResetReadyState();
            return;
        }

        if (_inventoryReady)
        {
            RequestEnsure();
        }
    }

    public void NotifyHostRemoved(ItemKey hostKey)
    {
        if (_disposed || !hostKey.IsValid())
        {
            return;
        }

        if (!ItemGraftRuntime.ClearCurrentIfHost(
                hostKey,
                out bool wasInTaiwuInventory))
        {
            return;
        }

        UnmonitorCurrentHost();

        if (wasInTaiwuInventory)
        {
            _onHostLeftTaiwuInventory();
        }

        RequestEnsure();
    }

    public void NotifyHostTaiwuInventoryChanged(
        ItemKey hostKey,
        bool isInTaiwuInventory)
    {
        if (_disposed || !hostKey.IsValid())
        {
            return;
        }

        if (!ItemGraftRuntime.SetCurrentHostInTaiwuInventory(
                hostKey,
                isInTaiwuInventory))
        {
            return;
        }

        if (isInTaiwuInventory)
        {
            MonitorHost(hostKey);
        }
        else
        {
            _onHostLeftTaiwuInventory();
        }

        RequestEnsure();
    }

    private void OnHostItemDataChanged(List<NotificationWrapper> notifications)
    {
        if (_disposed || !_monitoredHostKey.IsValid())
        {
            return;
        }

        for (int i = 0; i < notifications.Count; i++)
        {
            Notification notification = notifications[i].Notification;

            if (notification.Type == NotificationType.DataModification
                && notification.Uid.DomainId == DomainHelper.DomainIds.Item
                && notification.Uid.DataId == ItemDomainHelper.DataIds.CraftTools
                && notification.Uid.SubId0 == (ulong)_monitoredHostKey.Id)
            {
                RequestEnsure();
                return;
            }
        }
    }

    private async UniTask EnsureGraftAsync(
        int taiwuCharId,
        int stateVersion)
    {
        _ensuring = true;

        try
        {
            if (ItemGraftRuntime.TryGetCurrentHost(out ItemKey currentHost))
            {
                bool currentInInventory = await InventoryContainsItemAsync(taiwuCharId, currentHost);

                if (!IsEnsureCurrent(taiwuCharId, stateVersion))
                {
                    return;
                }

                if (currentInInventory)
                {
                    _ = ItemGraftRuntime.SetCurrentHostInTaiwuInventory(
                        currentHost,
                        isInTaiwuInventory: true);
                    MonitorHost(currentHost);
                    return;
                }

                if (ItemGraftRuntime.SetCurrentHostInTaiwuInventory(
                        currentHost,
                        isInTaiwuInventory: false))
                {
                    _onHostLeftTaiwuInventory();
                }

                bool currentExists = await ItemExistsAsync(currentHost);

                if (!IsEnsureCurrent(taiwuCharId, stateVersion))
                {
                    return;
                }

                if (currentExists)
                {
                    MonitorHost(currentHost);
                    return;
                }

                UnmonitorCurrentHost();
                ItemGraftRuntime.ClearCurrent();
            }

            IReadOnlyList<ItemDisplayData> medicineBowls = await GetMedicineBowlsAsync(taiwuCharId);

            if (!IsEnsureCurrent(taiwuCharId, stateVersion))
            {
                return;
            }

            ItemKey existingHost = SelectHost(medicineBowls);
            GraftDefinition definition = ItemGraftRuntime.CreateDefinition();

            Graft graft = existingHost.IsValid()
                ? await InventoryGrafts.AttachAsync(
                    existingHost,
                    definition,
                    new AttachOptions
                    {
                        NotificationMessage = AttachNotification,
                    })
                : await InventoryGrafts.CreateAsync(
                    taiwuCharId,
                    new HostTemplate(
                        GameData.Domains.Item.ItemType.CraftTool,
                        CraftTool.DefKey.Medicine0),
                    definition,
                    new CreateOptions
                    {
                        NotificationMessage = CreateNotification,
                    });

            if (!IsEnsureCurrent(taiwuCharId, stateVersion))
            {
                return;
            }

            ItemGraftRuntime.SetCurrent(
                graft,
                isInTaiwuInventory: true);
            MonitorHost(graft.HostKey);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Error(ex, "failed to ensure Xiangshu item graft");
        }
        finally
        {
            _ensuring = false;

            if (_ensureRequested)
            {
                RequestEnsure();
            }
        }
    }

    private void MonitorHost(ItemKey hostKey)
    {
        if (_monitoredHostKey == hostKey)
        {
            return;
        }

        UnmonitorCurrentHost(updateBackendRegistration: false);

        if (!IsMedicineBowl(hostKey))
        {
            BackendHostRegistration.UnregisterHost();
            return;
        }

        EnsureItemListener();
        _monitoredHostKey = hostKey;
        GameDataBridge.AddDataMonitor(
            _itemListenerId,
            DomainHelper.DomainIds.Item,
            ItemDomainHelper.DataIds.CraftTools,
            (ulong)hostKey.Id);
        BackendHostRegistration.RegisterHost(hostKey);
    }

    private void UnmonitorCurrentHost(bool updateBackendRegistration = true)
    {
        if (_itemListenerId < 0 || !_monitoredHostKey.IsValid())
        {
            _monitoredHostKey = ItemKey.Invalid;

            if (updateBackendRegistration)
            {
                BackendHostRegistration.UnregisterHost();
            }

            return;
        }

        GameDataBridge.AddDataUnMonitor(
            _itemListenerId,
            DomainHelper.DomainIds.Item,
            ItemDomainHelper.DataIds.CraftTools,
            (ulong)_monitoredHostKey.Id);
        _monitoredHostKey = ItemKey.Invalid;

        if (updateBackendRegistration)
        {
            BackendHostRegistration.UnregisterHost();
        }
    }

    private void EnsureItemListener()
    {
        if (_itemListenerId >= 0)
        {
            return;
        }

        _itemListenerId = GameDataBridge.RegisterListener(OnHostItemDataChanged);
    }

    private void UnregisterItemListener()
    {
        if (_itemListenerId < 0)
        {
            return;
        }

        GameDataBridge.UnregisterListener(_itemListenerId);
        _itemListenerId = -1;
    }

    private bool IsEnsureCurrent(
        int taiwuCharId,
        int stateVersion)
    {
        return !_disposed
            && _stateVersion == stateVersion
            && TryGetTaiwuCharId(out int currentTaiwuCharId)
            && currentTaiwuCharId == taiwuCharId;
    }

    private static async UniTask<bool> InventoryContainsItemAsync(
        int taiwuCharId,
        ItemKey hostKey)
    {
        UniTaskCompletionSource<bool> completionSource = new();

        try
        {
            CharacterDomainMethod.AsyncCall.InventoryContainsItem(
                null,
                taiwuCharId,
                hostKey,
                (offset, dataPool) =>
                {
                    try
                    {
                        bool contains = false;
                        _ = Serializer.Deserialize(dataPool, offset, ref contains);
                        _ = completionSource.TrySetResult(contains);
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _ = completionSource.TrySetException(ex);
                    }
                });
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return await completionSource.Task;
    }

    private static async UniTask<bool> ItemExistsAsync(ItemKey hostKey)
    {
        if (!hostKey.IsValid())
        {
            return false;
        }

        UniTaskCompletionSource<bool> completionSource = new();

        try
        {
            ItemDomainMethod.AsyncCall.GetItemDisplayData(
                null,
                hostKey,
                (offset, dataPool) =>
                {
                    try
                    {
                        ItemDisplayData? item = null;
                        _ = Serializer.Deserialize(dataPool, offset, ref item);
                        _ = completionSource.TrySetResult(GetHostKey(item) == hostKey);
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _ = completionSource.TrySetException(ex);
                    }
                });
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return await completionSource.Task;
    }

    private static async UniTask<List<ItemDisplayData>> GetMedicineBowlsAsync(int taiwuCharId)
    {
        UniTaskCompletionSource<List<ItemDisplayData>> completionSource = new();
        short itemSubType = ItemTemplateHelper.GetItemSubType(
            GameData.Domains.Item.ItemType.CraftTool,
            CraftTool.DefKey.Medicine0);

        try
        {
            CharacterDomainMethod.AsyncCall.GetInventoryItems(
                null,
                taiwuCharId,
                itemSubType,
                (offset, dataPool) =>
                {
                    try
                    {
                        List<ItemDisplayData> inventoryItems = [];
                        _ = Serializer.Deserialize(dataPool, offset, ref inventoryItems);
                        _ = completionSource.TrySetResult(inventoryItems);
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _ = completionSource.TrySetException(ex);
                    }
                });
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return await completionSource.Task;
    }

    private static ItemKey SelectHost(IReadOnlyList<ItemDisplayData> medicineBowls)
    {
        ItemKey selected = ItemKey.Invalid;

        for (int i = 0; i < medicineBowls.Count; i++)
        {
            ItemKey key = GetHostKey(medicineBowls[i]);

            if (!IsMedicineBowl(key))
            {
                continue;
            }

            if (!selected.IsValid() || key.Id < selected.Id)
            {
                selected = key;
            }
        }

        return selected;
    }

    private static ItemKey GetHostKey(ItemDisplayData? item)
    {
        if (item is null)
        {
            return ItemKey.Invalid;
        }

        return item.RealKey.IsValid()
            ? item.RealKey
            : item.Key;
    }

    private static bool IsMedicineBowl(ItemKey key)
    {
        return key.IsValid()
            && key.ItemType == GameData.Domains.Item.ItemType.CraftTool
            && key.TemplateId == CraftTool.DefKey.Medicine0;
    }

    private static bool TryGetTaiwuCharId(out int taiwuCharId)
    {
        taiwuCharId = -1;

        try
        {
            if (GameApp.Instance is null
                || GameApp.Instance.GetCurrentGameStateName() != EGameState.InGame)
            {
                return false;
            }

            taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
            return taiwuCharId >= 0;
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            return false;
        }
    }
}
