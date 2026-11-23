using UnityEngine;

/// <summary>
/// The player's held weapon: the weapon in the selected weapon inventory slot, using the shared character held weapon logic.
/// When the selected slot is empty the player holds their unarmed attack, which is always available and never stored in the inventory.
/// </summary>
public class F_PlayerHeldWeapon : F_Character_HeldWeapon
{
    [Header("Player Equipped Weapons")]
    // Unarmed attack (such as fists) held whenever the selected weapon slot is empty; used like any other melee weapon but never an inventory item; leave empty for none
    public F_Item_Weapon_Melee playerEquippedWeaponUnarmed;

    [Header("Privates")]
    private F_GUI_Inventory_Slot selectedWeaponSlot;
    private F_Item_Weapon_Melee playerUnarmedInstance;

    protected override void Start()
    {
        // Spawned in the player's own scene so it persists with the player between scenes
        playerUnarmedInstance = F_Utility_Helper_Weapon.SpawnStoredWeaponInstance(playerEquippedWeaponUnarmed, gameObject.scene);
        base.Start();
    }

    private void OnDestroy()
    {
        if (playerUnarmedInstance != null)
        {
            Destroy(playerUnarmedInstance.gameObject);
            playerUnarmedInstance = null;
        }
    }

    /// <summary>Selects a weapon inventory slot and updates the visible held weapon.</summary>
    /// <param name="weaponSlot">The weapon slot to activate.</param>
    public void SelectWeaponSlot(F_GUI_Inventory_Slot weaponSlot)
    {
        F_Item_Weapon previousWeaponItem = GetSelectedWeaponItem();
        selectedWeaponSlot = weaponSlot;
        OnSelectedWeaponChanged(previousWeaponItem);
    }

    /// <summary>
    /// The held weapon is whatever is in the selected weapon slot, so dropping or swapping it updates immediately.
    /// Falls back to the unarmed attack when the slot is empty or no slot is selected.
    /// </summary>
    protected override F_Item_Weapon GetSelectedWeaponItem()
    {
        F_Item_Weapon slotWeaponItem = selectedWeaponSlot != null ? selectedWeaponSlot.slotItemObj as F_Item_Weapon : null;
        return slotWeaponItem != null ? slotWeaponItem : playerUnarmedInstance;
    }
}
