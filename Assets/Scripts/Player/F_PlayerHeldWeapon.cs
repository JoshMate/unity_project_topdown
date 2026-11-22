using UnityEngine;

/// <summary>The player's held weapon: the weapon in the selected weapon inventory slot, using the shared character held weapon logic.</summary>
public class F_PlayerHeldWeapon : F_Character_HeldWeapon
{
    [Header("Privates")]
    private F_GUI_Inventory_Slot selectedWeaponSlot;

    /// <summary>The player always hears their own weapon clearly.</summary>
    protected override EnumSoundType HeldWeaponSoundType => EnumSoundType.Direct;

    /// <summary>Selects a weapon inventory slot and updates the visible held weapon.</summary>
    /// <param name="weaponSlot">The weapon slot to activate.</param>
    public void SelectWeaponSlot(F_GUI_Inventory_Slot weaponSlot)
    {
        F_Item_Weapon previousWeaponItem = GetSelectedWeaponItem();
        selectedWeaponSlot = weaponSlot;
        OnSelectedWeaponChanged(previousWeaponItem);
    }

    /// <summary>The held weapon is whatever is in the selected weapon slot, so dropping or swapping it updates immediately.</summary>
    protected override F_Item_Weapon GetSelectedWeaponItem()
    {
        return selectedWeaponSlot != null ? selectedWeaponSlot.slotItemObj as F_Item_Weapon : null;
    }
}
