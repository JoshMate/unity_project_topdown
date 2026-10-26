using UnityEngine;

/// <summary>One weighted option in a spawn point's spawn list.</summary>
[System.Serializable]
public class F_Point_SpawnEntry
{
    [Header("Constants Private")]
    private const float spawnWeightDefault = 1f;
    private const float spawnWeightMin = 0f;
    private const int spawnCountLowest = 1;

    [Header("Spawn Entry")]
    // The prefab to spawn (item, ent, character, etc.)
    public GameObject spawnObject;
    // Relative chance of this entry being picked compared with the other entries in the list
    [Min(spawnWeightMin)]
    public float spawnWeight = spawnWeightDefault;

    [Header("Spawn Count")]
    // Items: the lowest item count assigned. Anything else: the fewest instances spawned
    [Min(spawnCountLowest)]
    public int spawnCountMin = spawnCountLowest;
    // Items: the highest item count assigned. Anything else: the most instances spawned
    [Min(spawnCountLowest)]
    public int spawnCountMax = spawnCountLowest;
    // How the count is distributed between min and max
    public enumRandomCurve spawnCountType = enumRandomCurve.Random;

    /// <summary>Whether this entry can be picked: it has an object assigned and a weight above zero.</summary>
    public bool IsSpawnable => spawnObject != null && spawnWeight > spawnWeightMin;

    /// <summary>Rolls a count between min and max using this entry's count type.</summary>
    public int RollSpawnCount()
    {
        return F_Utility_Helper_Random.RollIntegerInRange(spawnCountMin, spawnCountMax, spawnCountType);
    }

    /// <summary>Keeps the counts valid after editing: at least the lowest count, and max never below min.</summary>
    public void ValidateSpawnCounts()
    {
        spawnCountMin = Mathf.Max(spawnCountLowest, spawnCountMin);
        spawnCountMax = Mathf.Max(spawnCountMin, spawnCountMax);
    }
}
