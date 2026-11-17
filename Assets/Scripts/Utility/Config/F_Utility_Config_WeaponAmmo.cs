using UnityEngine;

/// <summary>Maps the weapon ammo enum to the existing ammo item prefabs.</summary>
[CreateAssetMenu(fileName = "OB_Config_WeaponAmmo", menuName = "Config/Weapon Ammo")]
public class F_Utility_Config_WeaponAmmo : ScriptableObject
{
    [Header("Constants Private")]
    private const string cfgWeaponAmmoResourcePath = "OB_Config_WeaponAmmo";

    [Header("Weapon Ammo Config")]
    public F_Item cfgWeaponAmmoEnergyItem;
    public F_Item cfgWeaponAmmoMagnumItem;
    public F_Item cfgWeaponAmmoOrdnanceItem;
    public F_Item cfgWeaponAmmoPistolItem;
    public F_Item cfgWeaponAmmoRifleItem;
    public F_Item cfgWeaponAmmoShotgunItem;

    [Header("Privates")]
    private static F_Utility_Config_WeaponAmmo instance;

    /// <summary>Loads (and caches) the ammo config asset from Resources.</summary>
    /// <returns>The ammo config asset, or null when it is missing.</returns>
    public static F_Utility_Config_WeaponAmmo Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_WeaponAmmo>(cfgWeaponAmmoResourcePath);
        }

        return instance;
    }

    /// <summary>Resolves an ammo enum value to its configured item prefab.</summary>
    /// <param name="ammoType">The ammo type selected on the weapon.</param>
    /// <returns>The matching ammo item, or null if the config or entry is missing.</returns>
    public static F_Item GetAmmoItem(enumWeaponAmmoType ammoType)
    {
        F_Utility_Config_WeaponAmmo config = Get();
        if (config == null)
        {
            return null;
        }

        switch (ammoType)
        {
            case enumWeaponAmmoType.AmmoEnergy:
                return config.cfgWeaponAmmoEnergyItem;
            case enumWeaponAmmoType.AmmoMagnum:
                return config.cfgWeaponAmmoMagnumItem;
            case enumWeaponAmmoType.AmmoOrdnance:
                return config.cfgWeaponAmmoOrdnanceItem;
            case enumWeaponAmmoType.AmmoPistol:
                return config.cfgWeaponAmmoPistolItem;
            case enumWeaponAmmoType.AmmoRifle:
                return config.cfgWeaponAmmoRifleItem;
            case enumWeaponAmmoType.AmmoShotgun:
                return config.cfgWeaponAmmoShotgunItem;
            default:
                return null;
        }
    }
}
