using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A reusable loot table asset. Defines the chance of producing anything and the weighted entries (object, weight and count range) to pick from.
/// Spawn points (and later containers) reference loot tables and roll them to decide what to create.
/// </summary>
[CreateAssetMenu(fileName = "OB_LootTable_New", menuName = "Loot/Loot Table")]
public class F_LootTable : ScriptableObject
{
    [Header("Constants Private")]
    private const float lootTableChanceMin = 0f;
    private const float lootTableChanceMax = 100f;

    [Header("Loot Table Settings")]
    // Percentage chance that a roll of this table produces anything
    [Range(lootTableChanceMin, lootTableChanceMax)]
    public float lootTableChance = lootTableChanceMax;
    // Weighted options; one is picked when the chance succeeds
    public List<F_LootTable_Entry> lootTableEntries = new List<F_LootTable_Entry>();

    private void OnValidate()
    {
        foreach (F_LootTable_Entry lootEntry in lootTableEntries)
        {
            lootEntry?.ValidateLootCounts();
        }
    }

    /// <summary>Rolls this table's chance, picks a weighted entry and rolls its count.</summary>
    /// <param name="lootObject">The prefab to create, or null when nothing was rolled.</param>
    /// <param name="lootCount">Items: the total item count. Anything else: the number of instances. Zero when nothing was rolled.</param>
    /// <returns>True when the roll produced something.</returns>
    public bool RollLoot(out GameObject lootObject, out int lootCount)
    {
        lootObject = null;
        lootCount = 0;
        if (lootTableChance <= lootTableChanceMin || Random.value * lootTableChanceMax > lootTableChance)
        {
            return false;
        }

        F_LootTable_Entry lootEntry = PickWeightedEntry();
        if (lootEntry == null)
        {
            return false;
        }

        lootObject = lootEntry.lootEntryObject;
        lootCount = lootEntry.RollLootCount();
        return true;
    }

    // Picks one lootable entry with probability proportional to its weight
    private F_LootTable_Entry PickWeightedEntry()
    {
        float totalWeight = 0f;
        foreach (F_LootTable_Entry lootEntry in lootTableEntries)
        {
            if (lootEntry != null && lootEntry.IsLootable)
            {
                totalWeight += lootEntry.lootEntryWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float weightRoll = Random.value * totalWeight;
        F_LootTable_Entry lastLootableEntry = null;
        foreach (F_LootTable_Entry lootEntry in lootTableEntries)
        {
            if (lootEntry == null || !lootEntry.IsLootable)
            {
                continue;
            }

            lastLootableEntry = lootEntry;
            weightRoll -= lootEntry.lootEntryWeight;
            if (weightRoll < 0f)
            {
                return lastLootableEntry;
            }
        }

        // Random.value can return exactly 1, which lands on the end of the final entry
        return lastLootableEntry;
    }
}
