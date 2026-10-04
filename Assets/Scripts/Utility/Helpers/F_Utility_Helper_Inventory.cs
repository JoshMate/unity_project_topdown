using System.Collections.Generic;
using UnityEngine;

public static class F_Utility_Helper_Inventory
{
    /// <summary>Checks whether an item is compatible with an inventory slot.</summary>
    /// <param name="slotToCheck">The inventory slot to validate.</param>
    /// <param name="itemToCheck">The item being placed.</param>
    /// <returns>True when the slot accepts the item's type.</returns>
    public static bool CheckIfItemTypeMatchesSlotType(F_GUI_Inventory_Slot slotToCheck, F_Item itemToCheck)
    {
        if (slotToCheck == null || itemToCheck == null || slotToCheck.isSlotLocked)
        {
            return false;
        }

        if (slotToCheck.slotType == enumSlotType.MiscAny)
        {
            return true;
        }

        if (slotToCheck.slotType == enumSlotType.GearAccesory)
        {
            return itemToCheck.itemType == enumItemType.GearAccesory;
        }

        if (slotToCheck.slotType == enumSlotType.GearBackpack)
        {
            return itemToCheck.itemType == enumItemType.GearBackpack;
        }

        if (slotToCheck.slotType == enumSlotType.GearBoots)
        {
            return itemToCheck.itemType == enumItemType.GearBoots;
        }

        if (slotToCheck.slotType == enumSlotType.GearChest)
        {
            return itemToCheck.itemType == enumItemType.GearChest;
        }

        if (slotToCheck.slotType == enumSlotType.GearFace)
        {
            return itemToCheck.itemType == enumItemType.GearFace;
        }

        if (slotToCheck.slotType == enumSlotType.GearGloves)
        {
            return itemToCheck.itemType == enumItemType.GearGloves;
        }

        if (slotToCheck.slotType == enumSlotType.GearHelmet)
        {
            return itemToCheck.itemType == enumItemType.GearHelmet;
        }

        if (slotToCheck.slotType == enumSlotType.GearLegs)
        {
            return itemToCheck.itemType == enumItemType.GearLegs;
        }

        if (slotToCheck.slotType == enumSlotType.MiscActive)
        {
            return itemToCheck.itemType == enumItemType.ActiveConsumable ||
                   itemToCheck.itemType == enumItemType.ActiveGadget;
        }

        if (slotToCheck.slotType == enumSlotType.WeaponPrimary)
        {
            return itemToCheck.itemType == enumItemType.WeaponGunTwoHanded ||
                   itemToCheck.itemType == enumItemType.WeaponMeleeTwoHanded;
        }

        if (slotToCheck.slotType == enumSlotType.WeaponSecondary)
        {
            return itemToCheck.itemType == enumItemType.WeaponGunOneHanded ||
                   itemToCheck.itemType == enumItemType.WeaponMeleeOneHanded;
        }

        return false;
    }

    /// <summary>Creates new item instances from a template, stacks them where possible, and drops any remainder at the player.</summary>
    /// <param name="itemTemplate">The item prefab or scene item used as the instance template.</param>
    /// <param name="count">The number of items to add.</param>
    /// <param name="playerInventory">The inventory that receives the new item instances.</param>
    /// <returns>True when all requested items were added or dropped as the full-inventory fallback.</returns>
    public static bool AddItemToInventory(F_Item itemTemplate, int count, F_PlayerInventory playerInventory)
    {
        if (itemTemplate == null || playerInventory == null || count <= 0)
        {
            return false;
        }

        List<F_GUI_Inventory_Slot> inventorySlots = playerInventory.invSlotInventory;
        int remainingCount = count;

        if (inventorySlots != null)
        {
            for (int slotIndex = 0; slotIndex < inventorySlots.Count && remainingCount > 0; slotIndex++)
            {
                F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
                if (slot == null || slot.isSlotLocked || !AreStackCompatible(itemTemplate, slot.slotItemObj))
                {
                    continue;
                }

                remainingCount -= AddToStack(slot.slotItemObj, remainingCount);
            }

            int maximumStackCount = Mathf.Max(1, itemTemplate.itemCountMax);
            for (int slotIndex = 0; slotIndex < inventorySlots.Count && remainingCount > 0; slotIndex++)
            {
                F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
                if (slot == null || slot.isSlotLocked || slot.slotItemObj != null ||
                    !CheckIfItemTypeMatchesSlotType(slot, itemTemplate))
                {
                    continue;
                }

                int stackCount = Mathf.Min(remainingCount, maximumStackCount);
                F_Item newItem = CreateItemInstance(itemTemplate, playerInventory, stackCount);
                if (newItem == null)
                {
                    return false;
                }

                slot.slotItemObj = newItem;
                newItem.SetInventoryStoredState(true);
                remainingCount -= stackCount;
            }
        }

        if (remainingCount > 0)
        {
            F_Item remainderItem = CreateItemInstance(itemTemplate, playerInventory, remainingCount);
            if (remainderItem == null)
            {
                return false;
            }

            DropItemFromInventory(remainderItem, playerInventory);
        }

        return true;
    }

    /// <summary>Removes the loaded ammo from a weapon and adds it to the inventory as new ammo items.</summary>
    /// <param name="weapon">The weapon to unload.</param>
    /// <param name="playerInventory">The inventory that receives the ammo (overflow drops under the player).</param>
    /// <returns>The number of rounds moved into the inventory; 0 when nothing could be unloaded.</returns>
    public static int UnloadWeaponAmmoToInventory(F_Item_Weapon weapon, F_PlayerInventory playerInventory)
    {
        if (weapon == null || playerInventory == null || !weapon.CanUnloadAmmo)
        {
            return 0;
        }

        F_Item ammoTemplate = weapon.weaponAmmoType;
        int unloadedAmmo = weapon.TakeLoadedAmmo();
        if (unloadedAmmo <= 0)
        {
            return 0;
        }

        AddItemToInventory(ammoTemplate, unloadedAmmo, playerInventory);
        return unloadedAmmo;
    }

    /// <summary>Moves an existing floor item into the player's inventory, stacking it or dropping it under the player if no slot accepts it.</summary>
    /// <param name="itemToMove">The existing item instance to move.</param>
    /// <param name="playerInventory">The inventory that receives the item.</param>
    /// <returns>True when the item was already stored, moved, merged, or dropped as a fallback.</returns>
    public static bool MoveItemToInventory(F_Item itemToMove, F_PlayerInventory playerInventory)
    {
        if (itemToMove == null || playerInventory == null || itemToMove.itemCount <= 0)
        {
            return false;
        }

        List<F_GUI_Inventory_Slot> allSlots = GetAllSlots(playerInventory);
        for (int slotIndex = 0; slotIndex < allSlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = allSlots[slotIndex];
            if (slot != null && !slot.isSlotLocked && slot.slotItemObj == itemToMove)
            {
                return true;
            }
        }

        bool transferredToExistingStack = false;
        List<F_GUI_Inventory_Slot> inventorySlots = playerInventory.invSlotInventory;
        if (inventorySlots != null)
        {
            for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
            {
                F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
                if (slot == null || slot.isSlotLocked || slot.slotItemObj == null)
                {
                    continue;
                }

                int transferredCount = MergeItemStacks(itemToMove, slot.slotItemObj);
                transferredToExistingStack |= transferredCount > 0;
                if (itemToMove.itemCount <= 0)
                {
                    PlayPickupSound(itemToMove);
                    RemoveItemFromInventory(itemToMove, playerInventory);
                    return true;
                }
            }

            for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
            {
                F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
                if (slot == null || slot.isSlotLocked || slot.slotItemObj != null ||
                    !CheckIfItemTypeMatchesSlotType(slot, itemToMove))
                {
                    continue;
                }

                ClearItemReferences(itemToMove, playerInventory);
                slot.slotItemObj = itemToMove;
                itemToMove.SetInventoryStoredState(true);
                PlayPickupSound(itemToMove);
                return true;
            }
        }

        if (transferredToExistingStack)
        {
            PlayPickupSound(itemToMove);
        }

        DropItemFromInventoryInternal(itemToMove, playerInventory, null, false);
        return true;
    }

    /// <summary>Moves an existing floor item into the cursor-held inventory position.</summary>
    /// <param name="itemToMove">The existing item instance to pick up.</param>
    /// <param name="cursor">The cursor that will hold the item.</param>
    /// <returns>True if the cursor was empty or already held this item.</returns>
    public static bool MoveItemToInventory(F_Item itemToMove, F_Logic_Cursor cursor)
    {
        if (itemToMove == null || cursor == null ||
            (cursor.cursorHeldItemObj != null && cursor.cursorHeldItemObj != itemToMove))
        {
            return false;
        }

        if (cursor.cursorHeldItemObj == itemToMove)
        {
            return true;
        }

        F_PlayerInventory playerInventory = cursor.playerController != null
            ? cursor.playerController.playerInventory
            : null;
        if (playerInventory != null)
        {
            ClearItemReferences(itemToMove, playerInventory);
        }

        cursor.cursorHeldItemObj = itemToMove;
        itemToMove.SetInventoryStoredState(true);
        PlayPickupSound(itemToMove);
        return true;
    }

    /// <summary>Drops an inventory item at the cursor position, or beneath the player when no position is provided.</summary>
    /// <param name="itemToDrop">The existing item instance to drop.</param>
    /// <param name="playerInventory">The inventory whose references should be cleared.</param>
    /// <param name="cursorWorldPosition">Optional world position for the drop; omitted values fall back to the player.</param>
    /// <returns>True when the item was released to the world.</returns>
    public static bool DropItemFromInventory(
        F_Item itemToDrop,
        F_PlayerInventory playerInventory,
        Vector3? cursorWorldPosition = null)
    {
        return DropItemFromInventoryInternal(itemToDrop, playerInventory, cursorWorldPosition, true);
    }

    private static bool DropItemFromInventoryInternal(
        F_Item itemToDrop,
        F_PlayerInventory playerInventory,
        Vector3? cursorWorldPosition,
        bool playDropSound)
    {
        if (itemToDrop == null)
        {
            return false;
        }

        Vector3 fallbackPosition = playerInventory != null
            ? playerInventory.transform.position
            : itemToDrop.transform.position;
        Vector3 dropPosition = cursorWorldPosition ?? fallbackPosition;
        if (!IsFinite(dropPosition))
        {
            dropPosition = fallbackPosition;
        }

        ClearItemReferences(itemToDrop, playerInventory);
        itemToDrop.SetInventoryStoredState(false);
        itemToDrop.transform.position = dropPosition;

        if (playDropSound)
        {
            PlayDropSound(itemToDrop);
        }

        return true;
    }

    private static void PlayPickupSound(F_Item item)
    {
        if (item != null && item.itemSoundPickup != null)
        {
            F_Logic_Audio.PlaySound(
                item.itemSoundPickup,
                EnumSoundType.Direct);
        }
    }

    private static void PlayDropSound(F_Item item)
    {
        if (item != null && item.itemSoundDrop != null)
        {
            F_Logic_Audio.PlaySound(
                item.itemSoundDrop,
                EnumSoundType.Direct);
        }
    }

    /// <summary>Removes an item from all inventory references and permanently destroys its instance.</summary>
    /// <param name="itemToRemove">The item instance to delete.</param>
    /// <param name="playerInventory">The inventory whose references should be cleared.</param>
    /// <returns>True if the item instance was removed.</returns>
    public static bool RemoveItemFromInventory(F_Item itemToRemove, F_PlayerInventory playerInventory)
    {
        if (itemToRemove == null)
        {
            return false;
        }

        if (!itemToRemove.gameObject.scene.IsValid())
        {
            return false;
        }

        ClearItemReferences(itemToRemove, playerInventory);
        itemToRemove.SetInventoryStoredState(false);
        Object.Destroy(itemToRemove.gameObject);
        return true;
    }

    /// <summary>Transfers as much of one compatible item stack as the destination can hold.</summary>
    /// <param name="sourceItem">The item whose count is reduced.</param>
    /// <param name="destinationStack">The item stack whose count is increased.</param>
    /// <returns>The number of items transferred.</returns>
    public static int MergeItemStacks(F_Item sourceItem, F_Item destinationStack)
    {
        if (sourceItem == null || destinationStack == null || sourceItem == destinationStack ||
            !AreStackCompatible(sourceItem, destinationStack) || sourceItem.itemCount <= 0)
        {
            return 0;
        }

        int transferredCount = AddToStack(destinationStack, sourceItem.itemCount);
        sourceItem.itemCount -= transferredCount;
        return transferredCount;
    }

    private static F_Item CreateItemInstance(
        F_Item itemTemplate,
        F_PlayerInventory playerInventory,
        int itemCount)
    {
        GameObject itemInstance = Object.Instantiate(
            itemTemplate.gameObject,
            playerInventory.transform.position,
            itemTemplate.transform.rotation);
        F_Item createdItem = itemInstance.GetComponent<F_Item>();
        if (createdItem == null)
        {
            Object.Destroy(itemInstance);
            return null;
        }

        createdItem.itemCount = itemCount;
        createdItem.SetInventoryStoredState(false);
        return createdItem;
    }

    private static int AddToStack(F_Item destinationStack, int requestedCount)
    {
        if (destinationStack == null || requestedCount <= 0)
        {
            return 0;
        }

        int availableStackSpace = destinationStack.itemCountMax - destinationStack.itemCount;
        int transferredCount = Mathf.Min(Mathf.Max(0, availableStackSpace), requestedCount);
        destinationStack.itemCount += transferredCount;
        return transferredCount;
    }

    private static bool AreStackCompatible(F_Item firstItem, F_Item secondItem)
    {
        return firstItem != secondItem && IsSameItemDefinition(firstItem, secondItem);
    }

    private static bool IsSameItemDefinition(F_Item firstItem, F_Item secondItem)
    {
        return firstItem != null && secondItem != null &&
               firstItem.itemType == secondItem.itemType && firstItem.itemName == secondItem.itemName;
    }

    /// <summary>Returns how many of a matching item template are currently stacked across the inventory slots.</summary>
    /// <param name="playerInventory">The inventory to search.</param>
    /// <param name="itemTemplate">The item definition (e.g. an ammo type) to match by type and name.</param>
    public static int GetItemCountInInventory(F_PlayerInventory playerInventory, F_Item itemTemplate)
    {
        if (playerInventory == null || itemTemplate == null || playerInventory.invSlotInventory == null)
        {
            return 0;
        }

        int totalCount = 0;
        List<F_GUI_Inventory_Slot> inventorySlots = playerInventory.invSlotInventory;
        for (int slotIndex = 0; slotIndex < inventorySlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
            if (slot != null && slot.slotItemObj != null && IsSameItemDefinition(slot.slotItemObj, itemTemplate))
            {
                totalCount += slot.slotItemObj.itemCount;
            }
        }

        return totalCount;
    }

    /// <summary>Permanently removes up to the requested amount of a matching item template from the inventory, destroying emptied stacks.</summary>
    /// <param name="playerInventory">The inventory to consume from.</param>
    /// <param name="itemTemplate">The item definition (e.g. an ammo type) to match by type and name.</param>
    /// <param name="amountToConsume">The requested amount to remove.</param>
    /// <returns>The number of items actually consumed.</returns>
    public static int ConsumeItemFromInventory(F_PlayerInventory playerInventory, F_Item itemTemplate, int amountToConsume)
    {
        if (playerInventory == null || itemTemplate == null || playerInventory.invSlotInventory == null || amountToConsume <= 0)
        {
            return 0;
        }

        int remainingToConsume = amountToConsume;
        List<F_GUI_Inventory_Slot> inventorySlots = playerInventory.invSlotInventory;
        for (int slotIndex = 0; slotIndex < inventorySlots.Count && remainingToConsume > 0; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = inventorySlots[slotIndex];
            F_Item stackItem = slot != null ? slot.slotItemObj : null;
            if (stackItem == null || !IsSameItemDefinition(stackItem, itemTemplate))
            {
                continue;
            }

            int consumedFromStack = Mathf.Min(remainingToConsume, stackItem.itemCount);
            stackItem.itemCount -= consumedFromStack;
            remainingToConsume -= consumedFromStack;

            if (stackItem.itemCount <= 0)
            {
                RemoveItemFromInventory(stackItem, playerInventory);
            }
        }

        return amountToConsume - remainingToConsume;
    }

    private static List<F_GUI_Inventory_Slot> GetAllSlots(F_PlayerInventory playerInventory)
    {
        List<F_GUI_Inventory_Slot> slots = new List<F_GUI_Inventory_Slot>();
        if (playerInventory == null)
        {
            return slots;
        }

        AddSlot(slots, playerInventory.invSlotClothingHead);
        AddSlot(slots, playerInventory.invSlotClothingTorso);
        AddSlot(slots, playerInventory.invSlotClothingLegs);
        AddSlot(slots, playerInventory.invSlotClothingHands);
        AddSlot(slots, playerInventory.invSlotClothingFeet);
        AddSlot(slots, playerInventory.invSlotClothingBack);
        AddSlot(slots, playerInventory.invSlotAccessory01);
        AddSlot(slots, playerInventory.invSlotAccessory02);
        AddSlot(slots, playerInventory.invSlotAccessory03);
        AddSlot(slots, playerInventory.invSlotAccessory04);
        AddSlots(slots, playerInventory.invSlotWeapons);
        AddSlots(slots, playerInventory.invSlotQuickSlots);
        AddSlots(slots, playerInventory.invSlotInventory);

        return slots;
    }

    private static void AddSlots(List<F_GUI_Inventory_Slot> slots, List<F_GUI_Inventory_Slot> slotsToAdd)
    {
        if (slotsToAdd == null)
        {
            return;
        }

        for (int slotIndex = 0; slotIndex < slotsToAdd.Count; slotIndex++)
        {
            AddSlot(slots, slotsToAdd[slotIndex]);
        }
    }

    private static void AddSlot(List<F_GUI_Inventory_Slot> slots, F_GUI_Inventory_Slot slot)
    {
        if (slot != null && !slots.Contains(slot))
        {
            slots.Add(slot);
        }
    }

    private static void ClearItemReferences(F_Item item, F_PlayerInventory playerInventory)
    {
        if (item == null || playerInventory == null)
        {
            return;
        }

        List<F_GUI_Inventory_Slot> slots = GetAllSlots(playerInventory);
        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            if (slots[slotIndex].slotItemObj == item)
            {
                slots[slotIndex].slotItemObj = null;
            }
        }

        F_PlayerController playerController = playerInventory.GetComponent<F_PlayerController>();
        if (playerController != null && playerController.playerCursor != null &&
            playerController.playerCursor.cursorHeldItemObj == item)
        {
            playerController.playerCursor.cursorHeldItemObj = null;
        }
    }

    private static bool IsFinite(Vector3 position)
    {
        return !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
               !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
               !float.IsNaN(position.z) && !float.IsInfinity(position.z);
    }
}
