using Cysharp.Threading.Tasks;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.AsyncInterop;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ModRpc;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 提供创建和附加行囊物品嫁接会话的前端入口。
/// </summary>
public static class InventoryGrafts
{
    private static readonly object SyncRoot = new();

    private static bool s_isInstalled;

    /// <summary>
    /// 将前端嫁接系统绑定到当前太吾 mod。
    /// </summary>
    /// <param name="plugin">前端插件实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> 为 null。</exception>
    public static void Install(TaiwuRemakePlugin plugin)
    {
        if (plugin is null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }

        lock (SyncRoot)
        {
            RpcPeer.Bind(plugin.ModIdStr);
            s_isInstalled = true;
        }
    }

    /// <summary>
    /// 为已有真实宿主物品创建嫁接会话。
    /// </summary>
    /// <param name="hostKey">已有的非堆叠宿主物品 key。</param>
    /// <param name="definition">要应用到宿主物品上的嫁接定义。</param>
    /// <param name="options">可选通知和宿主事件行为。</param>
    /// <param name="cancellationToken">用于停止等待后端宿主订阅的取消令牌。</param>
    /// <returns>返回已建立嫁接会话的 UniTask。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> 为 null。</exception>
    /// <exception cref="InvalidOperationException">前端嫁接系统尚未安装，或宿主在会话建立前已结束。</exception>
    public static async UniTask<GraftSession> AttachAsync(
        ItemKey hostKey,
        GraftDefinition definition,
        AttachOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInstalled();

        Graft graft = CreateGraft(hostKey, definition);

        GraftSession session = await GraftSession.CreateAsync(
            graft,
            options?.OnHostEvent,
            cancellationToken);

        PushNotification(
            options?.NotificationMessage,
            options?.NotificationRecordType ?? GraftNotifications.DefaultNativeRecordType);

        return session;
    }

    /// <summary>
    /// 在角色行囊中创建真实宿主物品，并为其附加嫁接会话。
    /// </summary>
    /// <param name="characterId">接收真实宿主物品的太吾角色 ID。</param>
    /// <param name="hostTemplate">要创建的非堆叠宿主物品模板。</param>
    /// <param name="definition">要应用到新建宿主物品上的嫁接定义。</param>
    /// <param name="options">可选通知、宿主选择和宿主事件行为。</param>
    /// <param name="cancellationToken">用于停止等待异步游戏调用和后端宿主订阅的取消令牌。</param>
    /// <returns>返回已建立嫁接会话的 UniTask。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hostTemplate"/> 或 <paramref name="definition"/> 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterId"/> 小于 0。</exception>
    /// <exception cref="InvalidOperationException">前端嫁接系统尚未安装，无法定位新建宿主，或宿主在会话建立前已结束。</exception>
    public static async UniTask<GraftSession> CreateAsync(
        int characterId,
        GraftHostTemplate hostTemplate,
        GraftDefinition definition,
        CreateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInstalled();

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        int validatedCharacterId = ValidateCharacterId(characterId);
        GraftHostTemplate validatedHostTemplate = ValidateHostTemplate(hostTemplate);
        short hostItemSubType = ItemTemplateHelper.GetItemSubType(
            validatedHostTemplate.ItemType,
            validatedHostTemplate.TemplateId);

        IReadOnlyList<ItemDisplayData> beforeInventoryItems = await GetInventoryItemsAsync(
            validatedCharacterId,
            hostItemSubType);

        CharacterDomainMethod.Call.CreateInventoryItem(
            validatedCharacterId,
            validatedHostTemplate.ItemType,
            validatedHostTemplate.TemplateId,
            1);

        IReadOnlyList<ItemDisplayData> afterInventoryItems = await GetInventoryItemsAsync(
            validatedCharacterId,
            hostItemSubType);

        ItemKey hostKey = ValidateCreatedHostKey(
            (options?.SelectCreatedHost ?? SelectCreatedHost)(
                beforeInventoryItems,
                afterInventoryItems),
            validatedHostTemplate);

        Graft graft = new(
            hostKey,
            definition.Appearance,
            definition.MenuMode,
            definition.Operations);

        GraftSession session = await GraftSession.CreateAsync(
            graft,
            options?.OnHostEvent,
            cancellationToken);

        PushNotification(
            options?.NotificationMessage,
            options?.NotificationRecordType ?? GraftNotifications.DefaultNativeRecordType);

        return session;

        ItemKey SelectCreatedHost(
            IReadOnlyList<ItemDisplayData> beforeItems,
            IReadOnlyList<ItemDisplayData> afterItems)
        {
            return SelectDefaultCreatedHost(
                beforeItems,
                afterItems,
                validatedHostTemplate);
        }
    }

    private static UniTask<List<ItemDisplayData>> GetInventoryItemsAsync(
        int characterId,
        short itemSubType)
    {
        return TaiwuAsyncCall.InvokeAsync<List<ItemDisplayData>>(
            callback => CharacterDomainMethod.AsyncCall.GetInventoryItems(
                null,
                characterId,
                itemSubType,
                callback.Invoke));
    }

    private static void EnsureInstalled()
    {
        lock (SyncRoot)
        {
            if (!s_isInstalled)
            {
                throw new InvalidOperationException(
                    "InventoryGrafts.Install(plugin) must be called before using item graft actions.");
            }
        }
    }

    private static ItemKey SelectDefaultCreatedHost(
        IReadOnlyList<ItemDisplayData> beforeInventoryItems,
        IReadOnlyList<ItemDisplayData> afterInventoryItems,
        GraftHostTemplate hostTemplate)
    {
        HashSet<ItemKey> beforeKeys = [];

        for (int i = 0; i < beforeInventoryItems.Count; i++)
        {
            ItemDisplayData item = beforeInventoryItems[i];
            ItemKey key = item.RealKey;

            if (hostTemplate.Matches(key))
            {
                _ = beforeKeys.Add(key);
            }
        }

        ItemKey selectedNewKey = ItemKey.Invalid;

        for (int i = 0; i < afterInventoryItems.Count; i++)
        {
            ItemDisplayData item = afterInventoryItems[i];
            ItemKey key = item.RealKey;

            if (!hostTemplate.Matches(key))
            {
                continue;
            }

            if (!beforeKeys.Contains(key)
                && (!selectedNewKey.IsValid() || key.Id > selectedNewKey.Id))
            {
                selectedNewKey = key;
            }
        }

        if (selectedNewKey.IsValid())
        {
            return selectedNewKey;
        }

        throw new InvalidOperationException("Created host item was not found in refreshed inventory.");
    }

    private static void PushNotification(string? message, short nativeRecordType)
    {
        if (message is null)
        {
            return;
        }

        GraftNotifications.Push(message, nativeRecordType);
    }

    private static Graft CreateGraft(ItemKey hostKey, GraftDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return new Graft(
            hostKey,
            definition.Appearance,
            definition.MenuMode,
            definition.Operations);
    }

    private static ItemKey ValidateCreatedHostKey(ItemKey hostKey, GraftHostTemplate hostTemplate)
    {
        if (!GraftHostId.TryCreate(hostKey, out _))
        {
            throw new InvalidOperationException("Selected host item is not a valid graft host.");
        }

        if (!hostTemplate.Matches(hostKey))
        {
            throw new InvalidOperationException("Selected host item does not match the requested host template.");
        }

        return hostKey;
    }

    private static GraftHostTemplate ValidateHostTemplate(GraftHostTemplate hostTemplate)
    {
        return hostTemplate ?? throw new ArgumentNullException(nameof(hostTemplate));
    }

    private static int ValidateCharacterId(int characterId)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Character id must be valid.");
        }

        return characterId;
    }
}
