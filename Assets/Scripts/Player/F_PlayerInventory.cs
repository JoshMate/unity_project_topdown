using System.Collections.Generic;
using UnityEngine;

public class F_PlayerInventory : MonoBehaviour
{
    

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
    public F_GUI_Inventory_Slot invSlotWeaponPrimary01;
    public F_GUI_Inventory_Slot invSlotWeaponPrimary02;
    public F_GUI_Inventory_Slot invSlotWeaponSecondary01;
    public F_GUI_Inventory_Slot invSlotWeaponMelee01;

    [Header("Inventory Slot - Quick Slots")]
    public F_GUI_Inventory_Slot invSlotQuick01;
    public F_GUI_Inventory_Slot invSlotQuick02;
    public F_GUI_Inventory_Slot invSlotQuick03;
    public F_GUI_Inventory_Slot invSlotQuick04;
    public F_GUI_Inventory_Slot invSlotQuick05;

    [Header("Inventory Slot - Inventory")]

    public List<F_GUI_Inventory_Slot> invSlotInventory;

    private readonly HashSet<F_GUI_Inventory_Slot> visitedWeightSlots = new HashSet<F_GUI_Inventory_Slot>();
    private readonly HashSet<F_Item> visitedWeightItems = new HashSet<F_Item>();
    private F_PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<F_PlayerController>();
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
        totalWeight += GetSlotWeight(invSlotWeaponPrimary01);
        totalWeight += GetSlotWeight(invSlotWeaponPrimary02);
        totalWeight += GetSlotWeight(invSlotWeaponSecondary01);
        totalWeight += GetSlotWeight(invSlotWeaponMelee01);
        totalWeight += GetSlotWeight(invSlotQuick01);
        totalWeight += GetSlotWeight(invSlotQuick02);
        totalWeight += GetSlotWeight(invSlotQuick03);
        totalWeight += GetSlotWeight(invSlotQuick04);
        totalWeight += GetSlotWeight(invSlotQuick05);

        if (invSlotInventory != null)
        {
            for (int slotIndex = 0; slotIndex < invSlotInventory.Count; slotIndex++)
            {
                totalWeight += GetSlotWeight(invSlotInventory[slotIndex]);
            }
        }

        if (playerController != null && playerController.playerCursor != null)
        {
            totalWeight += GetItemWeight(playerController.playerCursor.cursorHeldItemObj);
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



    void Start()
    {
        
    }


    void Update()
    {
        
    }
}
