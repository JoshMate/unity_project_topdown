using System.Collections.Generic;
using UnityEngine;


public static class F_Utility_Helper_Inventory
{
    // A global helper utility that checks if an item fits into a slot (For placing items in iventory slots of different types etc...)
    public static bool CheckIfItemTypeMatchesSlotType(F_GUI_Inventory_Slot slotToCheck, F_Item itemToCheck)
    {
        if (slotToCheck == null) return false;
        if (itemToCheck == null) return false;

        // If the slot type is any then allow ALL items through
        if (slotToCheck.slotType == enumSlotType.MiscAny) return true;

        // Otherwise check agains the many slot type rules
        if (slotToCheck.slotType == enumSlotType.GearAccesory)
        {
            if (itemToCheck.itemType == enumItemType.GearAccesory) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearBackpack)
        {
            if (itemToCheck.itemType == enumItemType.GearBackpack) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearBoots)
        {
            if (itemToCheck.itemType == enumItemType.GearBoots) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearChest)
        {
            if (itemToCheck.itemType == enumItemType.GearChest) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearFace)
        {
            if (itemToCheck.itemType == enumItemType.GearFace) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearGloves)
        {
            if (itemToCheck.itemType == enumItemType.GearGloves) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearHelmet)
        {
            if (itemToCheck.itemType == enumItemType.GearHelmet) return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearLegs)
        {
            if (itemToCheck.itemType == enumItemType.GearLegs) return true;
        }

        if (slotToCheck.slotType == enumSlotType.MiscActive)
        {
            if (itemToCheck.itemType == enumItemType.ActiveConsumable) return true;
            if (itemToCheck.itemType == enumItemType.ActiveGadget) return true;
        }

        if (slotToCheck.slotType == enumSlotType.WeaponPrimary)
        {
            if (itemToCheck.itemType == enumItemType.WeaponGunTwoHanded) return true;
            if (itemToCheck.itemType == enumItemType.WeaponMeleeTwoHanded) return true;
        }

        if (slotToCheck.slotType == enumSlotType.WeaponSecondary)
        {
            if (itemToCheck.itemType == enumItemType.WeaponGunOneHanded) return true;
            if (itemToCheck.itemType == enumItemType.WeaponMeleeOneHanded) return true;
        }

        return false;

    }

    /// <summary>Adds an item to the player's general inventory, stacking it when possible and dropping it at the player if the inventory is full.</summary>
    /// <param name="itemToAdd">The item object to move into the inventory.</param>
    /// <param name="playerInventory">The inventory that receives the item.</param>
    /// <returns>True if the item was already stored or was added to inventory or dropped as a fallback.</returns>
    public static bool AddItemToInventory(F_Item itemToAdd, F_PlayerInventory playerInventory)
    {
        if (itemToAdd == null || playerInventory == null || itemToAdd.itemCount <= 0)
        {
            return false;
        }

        List<F_GUI_Inventory_Slot> inventorySlots = playerInventory.invSlotInventory;
        if (inventorySlots == null)
        {
            DropItemAtPlayer(itemToAdd, playerInventory);
            return true;
        }

        for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
            if (slot != null && slot.slotItemObj == itemToAdd)
            {
                return true;
            }
        }

        for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
            if (slot == null || slot.isSlotLocked || slot.slotItemObj == null)
            {
                continue;
            }

            MergeItemStacks(itemToAdd, slot.slotItemObj);
            if (itemToAdd.itemCount <= 0)
            {
                Object.Destroy(itemToAdd.gameObject);
                return true;
            }
        }

        for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
            if (slot == null || slot.isSlotLocked || slot.slotItemObj != null ||
                !CheckIfItemTypeMatchesSlotType(slot, itemToAdd))
            {
                continue;
            }

            slot.slotItemObj = itemToAdd;
            itemToAdd.MoveItemToInventory();
            return true;
        }

        DropItemAtPlayer(itemToAdd, playerInventory);
        return true;
    }

    /// <summary>Transfers as much of one compatible item stack as the destination can hold.</summary>
    /// <param name="sourceItem">The item whose count is reduced.</param>
    /// <param name="destinationStack">The item stack whose count is increased.</param>
    /// <returns>The number of items transferred.</returns>
    public static int MergeItemStacks(F_Item sourceItem, F_Item destinationStack)
    {
        if (sourceItem == null || destinationStack == null || sourceItem == destinationStack ||
            sourceItem.itemType != destinationStack.itemType || sourceItem.itemName != destinationStack.itemName ||
            sourceItem.itemCount <= 0)
        {
            return 0;
        }

        int availableStackSpace = destinationStack.itemCountMax - destinationStack.itemCount;
        if (availableStackSpace <= 0)
        {
            return 0;
        }

        int transferredCount = Mathf.Min(availableStackSpace, sourceItem.itemCount);
        destinationStack.itemCount += transferredCount;
        sourceItem.itemCount -= transferredCount;
        return transferredCount;
    }

    private static void DropItemAtPlayer(F_Item itemToDrop, F_PlayerInventory playerInventory)
    {
        itemToDrop.MoveItemOutOfInventory();
        itemToDrop.transform.position = playerInventory.transform.position;
    }

}
