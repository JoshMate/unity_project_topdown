using UnityEngine;

/// <summary>One weighted option in a loot table: what to create and how many.</summary>
[System.Serializable]
public class F_LootTable_Entry
{
    [Header("Constants Private")]
    private const float lootEntryWeightDefault = 1f;
    private const float lootEntryWeightMin = 0f;
    private const int lootEntryCountLowest = 1;

    [Header("Loot Entry")]
    // The prefab to create (item, ent, character, etc.)
    public GameObject lootEntryObject;
    // Relative chance of this entry being picked compared with the other entries in the table
    [Min(lootEntryWeightMin)]
    public float lootEntryWeight = lootEntryWeightDefault;

    [Header("Loot Count")]
    // Items: the lowest item count assigned. Anything else: the fewest instances created
    [Min(lootEntryCountLowest)]
    public int lootEntryCountMin = lootEntryCountLowest;
    // Items: the highest item count assigned, capped at the item's max count. Anything else: the most instances created
    [Min(lootEntryCountLowest)]
    public int lootEntryCountMax = lootEntryCountLowest;
    // How the count is distributed between min and max
    public enumRandomCurve lootEntryCountType = enumRandomCurve.Random;

    /// <summary>Whether this entry can be picked: it has an object assigned and a weight above zero.</summary>
    public bool IsLootable => lootEntryObject != null && lootEntryWeight > lootEntryWeightMin;

    /// <summary>Rolls a count between min and max using this entry's count type.</summary>
    public int RollLootCount()
    {
        return F_Utility_Helper_Random.RollIntegerInRange(lootEntryCountMin, lootEntryCountMax, lootEntryCountType);
    }

    /// <summary>Keeps the counts valid after editing: at least the lowest count, max never below min, and items never above their max stack size.</summary>
    public void ValidateLootCounts()
    {
        int lootEntryCountHighest = GetLootCountHighest();
        lootEntryCountMin = Mathf.Clamp(lootEntryCountMin, lootEntryCountLowest, lootEntryCountHighest);
        lootEntryCountMax = Mathf.Clamp(lootEntryCountMax, lootEntryCountMin, lootEntryCountHighest);
    }

    // Items are capped at the max count defined on the item; anything else has no upper limit
    private int GetLootCountHighest()
    {
        F_Item lootItem = lootEntryObject != null ? lootEntryObject.GetComponent<F_Item>() : null;
        return lootItem != null ? Mathf.Max(lootEntryCountLowest, lootItem.ItemCountMaxValue) : int.MaxValue;
    }
}
