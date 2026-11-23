using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps each held weapon sprite type to the sprite drawn in front of a character holding a firearm of that type.
/// Stored as an asset in a Resources folder so it is available in any scene.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_HeldWeapons", menuName = "Config/Held Weapons")]
public class F_Utility_Config_HeldWeapons : ScriptableObject
{
    /// <summary>One held weapon sprite type and the sprite drawn for it.</summary>
    [Serializable]
    public class F_HeldWeaponSpriteEntry
    {
        public enumWeaponHeldSprite heldWeaponSpriteType;
        // Drawn pointing right (+X) with the barrel tip as the rightmost opaque pixels
        public Sprite heldWeaponSprite;
    }

    [Header("Constants Public")]
    // Gap between the front edge of the holder's body sprite and the rear of a held firearm sprite (negative tucks it under the body)
    public const float cfgHeldWeaponFirearmEdgeOffset = 0f;

    [Header("Constants Private")]
    private const string cfgHeldWeaponResourcePath = "OB_Config_HeldWeapons";

    [Header("Held Weapon Config")]
    // Held weapon rig spawned on NPCs that have an equipped weapon (shows and uses their firearms and melee weapons)
    public GameObject cfgHeldWeaponNpcRigPrefab;
    // One entry per enumWeaponHeldSprite; firearms draw the sprite matching their held sprite type
    public List<F_HeldWeaponSpriteEntry> cfgHeldWeaponSprites = new List<F_HeldWeaponSpriteEntry>();

    [Header("Privates")]
    private static F_Utility_Config_HeldWeapons instance;

    private void Reset()
    {
        EnsureSpriteEntries();
    }

    private void OnValidate()
    {
        EnsureSpriteEntries();
    }

    // Keeps one entry per held sprite type in the list, adding missing ones with no sprite.
    private void EnsureSpriteEntries()
    {
        foreach (enumWeaponHeldSprite heldSpriteType in Enum.GetValues(typeof(enumWeaponHeldSprite)))
        {
            if (!cfgHeldWeaponSprites.Exists(entry => entry != null && entry.heldWeaponSpriteType == heldSpriteType))
            {
                cfgHeldWeaponSprites.Add(new F_HeldWeaponSpriteEntry { heldWeaponSpriteType = heldSpriteType });
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_HeldWeapons Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_HeldWeapons>(cfgHeldWeaponResourcePath);
        }

        return instance;
    }

    /// <summary>The held weapon rig prefab spawned on NPCs that have an equipped weapon.</summary>
    /// <returns>The rig prefab, or null when the asset or reference is missing.</returns>
    public static GameObject GetNpcRigPrefab()
    {
        F_Utility_Config_HeldWeapons config = Get();
        return config != null ? config.cfgHeldWeaponNpcRigPrefab : null;
    }

    /// <summary>Finds the sprite drawn for a held weapon sprite type.</summary>
    /// <param name="heldSpriteType">The held sprite type selected on the firearm.</param>
    /// <returns>The matching sprite, or null when the asset or entry is missing.</returns>
    public static Sprite GetHeldSprite(enumWeaponHeldSprite heldSpriteType)
    {
        F_Utility_Config_HeldWeapons config = Get();
        if (config == null)
        {
            return null;
        }

        F_HeldWeaponSpriteEntry spriteEntry = config.cfgHeldWeaponSprites.Find(entry => entry != null && entry.heldWeaponSpriteType == heldSpriteType);
        return spriteEntry != null ? spriteEntry.heldWeaponSprite : null;
    }
}
