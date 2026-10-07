using System;
using UnityEngine;

/// <summary>
/// Listens for damage and healing on every ent and spawns floating damage numbers.
/// Holds the icon shown for each damage type; add one to the persistent logic object.
/// </summary>
public class F_Logic_DamageNumbers : MonoBehaviour
{
    /// <summary>Links a damage type to its icon.</summary>
    [Serializable]
    public struct DamageTypeIcon
    {
        public enumDamageType damageType;
        public Sprite icon;
    }

    [Header("Damage Type Icons")]
    // Icon drawn left of the number for each damage type; types without an entry show no icon
    public DamageTypeIcon[] damageNumberIcons;

    private void OnEnable()
    {
        F_Ent.OnAnyEntDamaged += HandleDamaged;
        F_Ent.OnAnyEntHealed += HandleHealed;
    }

    private void OnDisable()
    {
        F_Ent.OnAnyEntDamaged -= HandleDamaged;
        F_Ent.OnAnyEntHealed -= HandleHealed;
    }

    private void HandleDamaged(F_Ent ent, float amount, enumDamageType damageType, GameObject source)
    {
        SpawnNumber(ent, source, amount, damageType, false);
    }

    private void HandleHealed(F_Ent ent, float amount, GameObject source)
    {
        SpawnNumber(ent, source, amount, enumDamageType.Heal, true);
    }

    private void SpawnNumber(F_Ent ent, GameObject source, float amount, enumDamageType damageType, bool isHeal)
    {
        if (ent == null)
        {
            return;
        }

        F_Utility_Helper_Damage.GetEntSorting(ent, out int sortingLayerId, out int sortingOrder);
        Vector3 centre = F_Utility_Helper_Damage.GetEntBounds(ent).center;
        centre.z = ent.transform.position.z;
        F_Effects_DamageNumber.Spawn(
            ent,
            source,
            damageType,
            amount,
            isHeal,
            GetIcon(damageType),
            centre,
            sortingLayerId,
            sortingOrder + F_Utility_Config_Damage.cfgDamageNumberSortingOrderOffset);
    }

    private Sprite GetIcon(enumDamageType damageType)
    {
        if (damageNumberIcons == null)
        {
            return null;
        }

        foreach (DamageTypeIcon entry in damageNumberIcons)
        {
            if (entry.damageType == damageType)
            {
                return entry.icon;
            }
        }

        return null;
    }
}
