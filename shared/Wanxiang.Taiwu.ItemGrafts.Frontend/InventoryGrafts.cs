using Cysharp.Threading.Tasks;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;
using Wanxiang.Taiwu.ModRpc;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

public static class InventoryGrafts
{
    private static readonly object SyncRoot = new();

    private static bool s_isInstalled;

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
        GraftHostTemplate validatedHostTemplate =
            GraftHostValidation.ValidateTemplate(hostTemplate, nameof(hostTemplate));
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
                (offset, dataPool) => callback(offset, dataPool)));
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

            if (GraftHostValidation.MatchesTemplate(key, hostTemplate))
            {
                _ = beforeKeys.Add(key);
            }
        }

        ItemKey selectedNewKey = ItemKey.Invalid;

        for (int i = 0; i < afterInventoryItems.Count; i++)
        {
            ItemDisplayData item = afterInventoryItems[i];
            ItemKey key = item.RealKey;

            if (!GraftHostValidation.MatchesTemplate(key, hostTemplate))
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

        ItemKey validatedHostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));

        return new Graft(
            validatedHostKey,
            definition.Appearance,
            definition.MenuMode,
            definition.Operations);
    }

    private static ItemKey ValidateCreatedHostKey(ItemKey hostKey, GraftHostTemplate hostTemplate)
    {
        ItemKey validatedHostKey = GraftHostValidation.ValidateKey(hostKey, "selectedHostKey");

        if (!GraftHostValidation.MatchesTemplate(validatedHostKey, hostTemplate))
        {
            throw new InvalidOperationException("Selected host item does not match the requested host template.");
        }

        return validatedHostKey;
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
