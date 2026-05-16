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

    [Header("Inventory Slot - Inventory")]

    public List<F_GUI_Inventory_Slot> invSlotInventory;



    void Start()
    {
        
    }


    void Update()
    {
        
    }
}
