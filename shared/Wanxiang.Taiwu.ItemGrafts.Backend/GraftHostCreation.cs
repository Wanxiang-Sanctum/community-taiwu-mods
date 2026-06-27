using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

internal static class GraftHostCreation
{
    internal static ItemKey CreateInOwner(
        DataContext context,
        GraftHostOwnerKey targetOwner,
        GraftHostTemplate hostTemplate)
    {
        ItemOwnerType ownerType = (ItemOwnerType)targetOwner.OwnerType;

        if (ownerType == ItemOwnerType.CharacterInventory)
        {
            return CreateInCharacterInventory(
                context,
                targetOwner.OwnerId,
                hostTemplate);
        }

        if (TryGetTaiwuVillageStorageSource(ownerType, out ItemSourceType sourceType))
        {
            return CreateInTaiwuVillageStorage(
                context,
                targetOwner,
                sourceType,
                hostTemplate);
        }

        throw new NotSupportedException(
            $"Graft host owner {targetOwner} has no game item collection add path.");
    }

    private static ItemKey CreateInCharacterInventory(
        DataContext context,
        int characterId,
        GraftHostTemplate hostTemplate)
    {
        if (!DomainManager.Character.TryGetElement_Objects(characterId, out Character character))
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Target character does not exist.");
        }

        ItemKey hostKey = CreateHostItem(context, hostTemplate);

        try
        {
            if (!character.AddInventoryItem(
                    context,
                    hostKey,
                    amount: 1,
                    offLine: false,
                    source: EItemAutoOperationSource.Invalid))
            {
                throw new InvalidOperationException(
                    "Created graft host could not be added to the target character inventory.");
            }

            return hostKey;
        }
        catch
        {
            RemoveCreatedHostFromCharacterInventory(context, character, hostKey);
            throw;
        }
    }

    private static ItemKey CreateInTaiwuVillageStorage(
        DataContext context,
        GraftHostOwnerKey targetOwner,
        ItemSourceType sourceType,
        GraftHostTemplate hostTemplate)
    {
        int taiwuVillageSettlementId = DomainManager.Taiwu.GetTaiwuVillageSettlementId();

        if (targetOwner.OwnerId != taiwuVillageSettlementId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetOwner),
                targetOwner,
                "Taiwu village storage owner id must be the current Taiwu village settlement id.");
        }

        ItemKey hostKey = CreateHostItem(context, hostTemplate);

        try
        {
            if (!DomainManager.Taiwu.AddItem(
                    context,
                    hostKey,
                    amount: 1,
                    itemSourceType: sourceType,
                    offLine: false,
                    source: EItemAutoOperationSource.Invalid))
            {
                throw new InvalidOperationException(
                    "Created graft host could not be added to the target Taiwu village storage.");
            }

            return hostKey;
        }
        catch
        {
            RemoveCreatedHostFromTaiwuVillageStorage(context, hostKey, sourceType);
            throw;
        }
    }

    private static bool TryGetTaiwuVillageStorageSource(
        ItemOwnerType ownerType,
        out ItemSourceType sourceType)
    {
        if (ownerType == ItemOwnerType.Warehouse)
        {
            sourceType = ItemSourceType.Warehouse;
            return true;
        }

        if (ownerType == ItemOwnerType.Treasury)
        {
            sourceType = ItemSourceType.Treasury;
            return true;
        }

        if (ownerType == ItemOwnerType.Stock)
        {
            sourceType = ItemSourceType.Stock;
            return true;
        }

        if (ownerType == ItemOwnerType.Trough)
        {
            sourceType = ItemSourceType.Trough;
            return true;
        }

        sourceType = default;
        return false;
    }

    private static ItemKey CreateHostItem(
        DataContext context,
        GraftHostTemplate hostTemplate)
    {
        return DomainManager.Item.CreateItem(
            context,
            hostTemplate.ItemType,
            hostTemplate.TemplateId);
    }

    private static void RemoveCreatedHost(DataContext context, ItemKey hostKey)
    {
        if (hostKey.IsValid() && DomainManager.Item.ItemExists(hostKey))
        {
            DomainManager.Item.RemoveItem(context, hostKey);
        }
    }

    private static void RemoveCreatedHostFromCharacterInventory(
        DataContext context,
        Character character,
        ItemKey hostKey)
    {
        if (character.GetInventory().Items.ContainsKey(hostKey))
        {
            character.RemoveInventoryItem(
                context,
                hostKey,
                amount: 1,
                deleteItem: true,
                offLine: false);
            return;
        }

        RemoveCreatedHost(context, hostKey);
    }

    private static void RemoveCreatedHostFromTaiwuVillageStorage(
        DataContext context,
        ItemKey hostKey,
        ItemSourceType sourceType)
    {
        if (DomainManager.Taiwu.GetInventory(sourceType).Items.ContainsKey(hostKey))
        {
            DomainManager.Taiwu.RemoveItem(
                context,
                hostKey,
                amount: 1,
                itemSourceType: sourceType,
                deleteItem: true,
                offLine: false);
            return;
        }

        RemoveCreatedHost(context, hostKey);
    }
}
