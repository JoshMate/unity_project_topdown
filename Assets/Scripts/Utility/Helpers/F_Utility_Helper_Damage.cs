using UnityEngine;

public static class F_Utility_Helper_Damage
{
    [Header("Constants Private")]
    private const float cfgDamageResistanceMin = 0f;
    private const float cfgDamageResistanceMax = 1f;

    /// <summary>Resolves an ent's resistance to a damage type.</summary>
    /// <param name="ent">The ent receiving damage.</param>
    /// <param name="damageType">The incoming damage type.</param>
    /// <returns>Resistance clamped to 0-1; typeless damage always returns 0.</returns>
    public static float GetResistance(F_Ent ent, enumDamageType damageType)
    {
        if (ent == null)
        {
            return cfgDamageResistanceMin;
        }

        float resistance;
        switch (damageType)
        {
            case enumDamageType.Melee: resistance = ent.entResistanceMelee; break;
            case enumDamageType.Bullet: resistance = ent.entResistanceBullet; break;
            case enumDamageType.Energy: resistance = ent.entResistanceEnergy; break;
            case enumDamageType.Fire: resistance = ent.entResistanceFire; break;
            case enumDamageType.Explosive: resistance = ent.entResistanceExplosive; break;
            case enumDamageType.Toxic: resistance = ent.entResistanceToxic; break;
            default: resistance = cfgDamageResistanceMin; break;
        }

        return Mathf.Clamp(resistance, cfgDamageResistanceMin, cfgDamageResistanceMax);
    }

    /// <summary>Applies the ent's resistance to raw damage.</summary>
    /// <param name="ent">The ent receiving damage.</param>
    /// <param name="rawDamage">Damage before resistance.</param>
    /// <param name="damageType">The incoming damage type.</param>
    /// <returns>The damage remaining after resistance.</returns>
    public static float CalculateFinalDamage(F_Ent ent, float rawDamage, enumDamageType damageType)
    {
        return Mathf.Max(0f, rawDamage) * (cfgDamageResistanceMax - GetResistance(ent, damageType));
    }

    /// <summary>Gets the combined visual bounds of an ent's sprites, or a point at its transform if it has none.</summary>
    /// <param name="ent">The ent to measure.</param>
    /// <returns>World-space bounds.</returns>
    public static Bounds GetEntBounds(F_Ent ent)
    {
        Bounds bounds = new Bounds(ent.transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (SpriteRenderer spriteRenderer in ent.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!spriteRenderer.enabled || spriteRenderer.sprite == null)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(spriteRenderer.bounds);
            }
            else
            {
                bounds = spriteRenderer.bounds;
                hasBounds = true;
            }
        }

        return bounds;
    }

    /// <summary>Finds the sorting layer and highest sorting order used by an ent's sprites.</summary>
    /// <param name="ent">The ent to inspect.</param>
    /// <param name="sortingLayerId">The sorting layer id of the top sprite.</param>
    /// <param name="sortingOrder">The highest sorting order.</param>
    public static void GetEntSorting(F_Ent ent, out int sortingLayerId, out int sortingOrder)
    {
        sortingLayerId = 0;
        sortingOrder = 0;
        bool found = false;
        foreach (SpriteRenderer spriteRenderer in ent.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!found || spriteRenderer.sortingOrder > sortingOrder)
            {
                sortingLayerId = spriteRenderer.sortingLayerID;
                sortingOrder = spriteRenderer.sortingOrder;
                found = true;
            }
        }
    }

    /// <summary>Looks up the blood or hit-effect colour for a material.</summary>
    /// <param name="material">The ent's material type.</param>
    /// <returns>The configured colour for the material.</returns>
    public static Color GetBloodColour(enumMaterialType material)
    {
        switch (material)
        {
            case enumMaterialType.Metal: return F_Utility_Config_Colours.cfgColourBloodMetal;
            case enumMaterialType.Rock: return F_Utility_Config_Colours.cfgColourBloodRock;
            case enumMaterialType.Wood: return F_Utility_Config_Colours.cfgColourBloodWood;
            case enumMaterialType.Meat: return F_Utility_Config_Colours.cfgColourBloodMeat;
            case enumMaterialType.Water: return F_Utility_Config_Colours.cfgColourBloodWater;
            default: return F_Utility_Config_Colours.cfgColourWhite;
        }
    }
}
