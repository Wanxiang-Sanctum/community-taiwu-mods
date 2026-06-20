using Cysharp.Threading.Tasks;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Serializer;

namespace Wanxiang.Taiwu.ItemGrafts;

public static class InventoryGrafts
{
    public static UniTask<Graft> AttachAsync(
        ItemKey hostKey,
        GraftDefinition definition,
        AttachOptions? options = null)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        ItemKey validatedHostKey = Graft.ValidateHostKey(hostKey, nameof(hostKey));

        Graft graft = new(
            validatedHostKey,
            definition.Appearance,
            definition.MenuMode,
            definition.Operations);

        PushNotification(
            options?.NotificationMessage,
            options?.NotificationRecordType ?? GraftNotifications.DefaultNativeRecordType);

        return UniTask.FromResult(graft);
    }

    public static async UniTask<Graft> CreateAsync(
        int characterId,
        HostTemplate hostTemplate,
        GraftDefinition definition,
        CreateOptions? options = null)
    {
        if (hostTemplate is null)
        {
            throw new ArgumentNullException(nameof(hostTemplate));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        int validatedCharacterId = ValidateCharacterId(characterId);
        ValidateHostTemplate(hostTemplate);
        short hostItemSubType = ItemTemplateHelper.GetItemSubType(
            hostTemplate.ItemType,
            hostTemplate.TemplateId);

        IReadOnlyList<ItemDisplayData> beforeInventoryItems = await GetInventoryItemsAsync(
            validatedCharacterId,
            hostItemSubType);

        CharacterDomainMethod.Call.CreateInventoryItem(
            validatedCharacterId,
            hostTemplate.ItemType,
            hostTemplate.TemplateId,
            1);

        IReadOnlyList<ItemDisplayData> afterInventoryItems = await GetInventoryItemsAsync(
            validatedCharacterId,
            hostItemSubType);

        ItemKey hostKey = ValidateCreatedHostKey(
            (options?.SelectHost ?? SelectCreatedHost)(
                beforeInventoryItems,
                afterInventoryItems),
            hostTemplate);

        Graft graft = new(
            hostKey,
            definition.Appearance,
            definition.MenuMode,
            definition.Operations);

        PushNotification(
            options?.NotificationMessage,
            options?.NotificationRecordType ?? GraftNotifications.DefaultNativeRecordType);

        return graft;

        ItemKey SelectCreatedHost(
            IReadOnlyList<ItemDisplayData> beforeItems,
            IReadOnlyList<ItemDisplayData> afterItems)
        {
            return SelectDefaultCreatedHost(
                beforeItems,
                afterItems,
                hostTemplate.ItemType,
                hostTemplate.TemplateId);
        }
    }

    private static async UniTask<List<ItemDisplayData>> GetInventoryItemsAsync(
        int characterId,
        short itemSubType)
    {
        UniTaskCompletionSource<List<ItemDisplayData>> completionSource = new();

        try
        {
            CharacterDomainMethod.AsyncCall.GetInventoryItems(
                null,
                characterId,
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
                    // Complete the UniTask with callback failures instead of letting the game dispatcher swallow them.
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _ = completionSource.TrySetException(ex);
                    }
                });
        }
#pragma warning disable CA1031
        // Convert immediate dispatch failures into the same UniTask failure path as callback failures.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _ = completionSource.TrySetException(ex);
        }

        return await completionSource.Task;
    }

    private static ItemKey SelectDefaultCreatedHost(
        IReadOnlyList<ItemDisplayData> beforeInventoryItems,
        IReadOnlyList<ItemDisplayData> afterInventoryItems,
        sbyte hostItemType,
        short hostTemplateId)
    {
        HashSet<ItemKey> beforeKeys = [];

        for (int i = 0; i < beforeInventoryItems.Count; i++)
        {
            ItemDisplayData item = beforeInventoryItems[i];
            ItemKey key = item.RealKey;

            if (IsHostTemplate(key, hostItemType, hostTemplateId))
            {
                _ = beforeKeys.Add(key);
            }
        }

        ItemKey selectedNewKey = ItemKey.Invalid;

        for (int i = 0; i < afterInventoryItems.Count; i++)
        {
            ItemDisplayData item = afterInventoryItems[i];
            ItemKey key = item.RealKey;

            if (!IsHostTemplate(key, hostItemType, hostTemplateId))
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

    private static bool IsHostTemplate(ItemKey key, sbyte hostItemType, short hostTemplateId)
    {
        return key.IsValid()
            && key.ItemType == hostItemType
            && key.TemplateId == hostTemplateId;
    }

    private static void PushNotification(string? message, short nativeRecordType)
    {
        if (message is null)
        {
            return;
        }

        GraftNotifications.Push(message, nativeRecordType);
    }

    private static void ValidateHostTemplate(HostTemplate hostTemplate)
    {
        if (!ItemTemplateHelper.CheckTemplateValid(
            hostTemplate.ItemType,
            hostTemplate.TemplateId))
        {
            throw new ArgumentException(
                "Host item template must be valid.",
                nameof(hostTemplate));
        }

        if (ItemTemplateHelper.IsStackable(hostTemplate.ItemType, hostTemplate.TemplateId))
        {
            throw new ArgumentException(
                "Host item must not be stackable.",
                nameof(hostTemplate));
        }
    }

    private static ItemKey ValidateCreatedHostKey(ItemKey hostKey, HostTemplate hostTemplate)
    {
        ItemKey validatedHostKey = Graft.ValidateHostKey(hostKey, "selectedHostKey");

        if (!IsHostTemplate(validatedHostKey, hostTemplate.ItemType, hostTemplate.TemplateId))
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
