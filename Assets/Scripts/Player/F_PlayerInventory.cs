using System.Collections.Generic;
using UnityEngine;

public class F_PlayerInventory : MonoBehaviour
{
    [Header("Constants Private")]
    private const int oneBasedIndexOffset = 1;

    [Header("Object Refs")]
    [Header("Inventory Slot - Gear")]
    public F_GUI_Inventory_Slot invSlotClothingHead;
    public F_GUI_Inventory_Slot invSlotClothingTorso;
    public F_GUI_Inventory_Slot invSlotClothingLegs;
    public F_GUI_Inventory_Slot invSlotClothingHands;
    public F_GUI_Inventory_Slot invSlotClothingFeet;
    public F_GUI_Inventory_Slot invSlotClothingBack;

    [Header("Inventory Slot - Accessories")]
    public F_GUI_Inventory_Slot invSlotAccessory01;
    public F_GUI_Inventory_Slot invSlotAccessory02;
    public F_GUI_Inventory_Slot invSlotAccessory03;
    public F_GUI_Inventory_Slot invSlotAccessory04;

    [Header("Inventory Slot - Weapons")]
    public List<F_GUI_Inventory_Slot> invSlotWeapons = new List<F_GUI_Inventory_Slot>();

    [Header("Inventory Slot - Quick Slots")]
    public List<F_GUI_Inventory_Slot> invSlotQuickSlots = new List<F_GUI_Inventory_Slot>();

    [Header("Inventory Slot - Inventory")]
    public List<F_GUI_Inventory_Slot> invSlotInventory = new List<F_GUI_Inventory_Slot>();

    [Header("Privates")]
    private readonly HashSet<F_GUI_Inventory_Slot> visitedWeightSlots = new HashSet<F_GUI_Inventory_Slot>();
    private readonly HashSet<F_Item> visitedWeightItems = new HashSet<F_Item>();
    private F_PlayerController playerController;
    private F_PlayerStats playerStats;

    private void Awake()
    {
        playerController = GetComponent<F_PlayerController>();
        playerStats = GetComponent<F_PlayerStats>();
        if (playerStats != null)
        {
            ApplyInventorySlotAvailability(playerStats.inventorySlotsAvailable);
        }
    }

    /// <summary>Applies the current player-stat slot capacity to the inventory slot UI.</summary>
    /// <param name="availableSlotCount">The number of inventory slots that should be usable.</param>
    public void ApplyInventorySlotAvailability(int availableSlotCount)
    {
        if (invSlotInventory == null)
        {
            return;
        }

        int boundedAvailableSlotCount = Mathf.Clamp(availableSlotCount, 0, invSlotInventory.Count);
        for (int slotIndex = 0; slotIndex < invSlotInventory.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = invSlotInventory[slotIndex];
            if (slot != null)
            {
                slot.SetSlotLocked(slotIndex >= boundedAvailableSlotCount);
            }
        }
    }

    /// <summary>Returns the weapon slot at a one-based index, or null when the index is invalid.</summary>
    /// <param name="oneBasedSlotIndex">The one-based weapon slot index.</param>
    public F_GUI_Inventory_Slot GetWeaponSlot(int oneBasedSlotIndex)
    {
        int slotIndex = oneBasedSlotIndex - oneBasedIndexOffset;
        if (invSlotWeapons == null || slotIndex < 0 || slotIndex >= invSlotWeapons.Count)
        {
            return null;
        }

        return invSlotWeapons[slotIndex];
    }

    /// <summary>Calculates the total weight of items held in every inventory and equipment slot.</summary>
    /// <returns>The sum of each occupied stack's item weight multiplied by its item count.</returns>
    public float CalculateCurrentWeight()
    {
        visitedWeightSlots.Clear();
        visitedWeightItems.Clear();

        float totalWeight = 0f;
        totalWeight += GetSlotWeight(invSlotClothingHead);
        totalWeight += GetSlotWeight(invSlotClothingTorso);
        totalWeight += GetSlotWeight(invSlotClothingLegs);
        totalWeight += GetSlotWeight(invSlotClothingHands);
        totalWeight += GetSlotWeight(invSlotClothingFeet);
        totalWeight += GetSlotWeight(invSlotClothingBack);
        totalWeight += GetSlotWeight(invSlotAccessory01);
        totalWeight += GetSlotWeight(invSlotAccessory02);
        totalWeight += GetSlotWeight(invSlotAccessory03);
        totalWeight += GetSlotWeight(invSlotAccessory04);
        totalWeight += GetSlotsWeight(invSlotWeapons, true);
        totalWeight += GetSlotsWeight(invSlotQuickSlots, true);
        totalWeight += GetSlotsWeight(invSlotInventory, false);

        if (playerController != null && playerController.playerCursor != null)
        {
            totalWeight += GetItemWeight(playerController.playerCursor.cursorHeldItemObj);
        }

        return totalWeight;
    }

    private float GetSlotsWeight(List<F_GUI_Inventory_Slot> slots, bool includeLockedSlots)
    {
        if (slots == null)
        {
            return 0f;
        }

        float totalWeight = 0f;
        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = slots[slotIndex];
            if (slot != null && (includeLockedSlots || !slot.isSlotLocked))
            {
                totalWeight += GetSlotWeight(slot);
            }
        }

        return totalWeight;
    }

    private float GetSlotWeight(F_GUI_Inventory_Slot slot)
    {
        if (slot == null || !visitedWeightSlots.Add(slot))
        {
            return 0f;
        }

        return GetItemWeight(slot.slotItemObj);
    }

    private float GetItemWeight(F_Item item)
    {
        if (item == null || !visitedWeightItems.Add(item))
        {
            return 0f;
        }

        return Mathf.Max(0f, item.itemWeight) * Mathf.Max(0, item.itemCount);
    }
}
