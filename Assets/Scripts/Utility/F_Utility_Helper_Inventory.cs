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
}
