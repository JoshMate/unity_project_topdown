/// <summary>
/// Somewhere a firearm can draw reload ammo from. The player inventory supplies real ammo items, while NPCs supply
/// virtual ammo from a simple count, so firearms reload the same way for both.
/// </summary>
public interface IF_Item_Weapon_AmmoReserve
{
    /// <summary>Returns how many rounds of the given ammo are available to reload with.</summary>
    /// <param name="ammoItem">The ammo item the firearm uses, or null when it uses no specific ammo item.</param>
    int GetReserveAmmoCount(F_Item ammoItem);

    /// <summary>Removes up to the requested rounds from the reserve and returns how many were actually removed.</summary>
    /// <param name="ammoItem">The ammo item the firearm uses, or null when it uses no specific ammo item.</param>
    /// <param name="amountToConsume">How many rounds the firearm wants to load.</param>
    int ConsumeReserveAmmo(F_Item ammoItem, int amountToConsume);
}
