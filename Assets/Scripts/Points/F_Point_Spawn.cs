using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A point that, when the game starts, may spawn an entry picked by weight from its spawn list.
/// Items receive the rolled count (split into stacks if it exceeds the stack size); anything else spawns that many instances.
/// </summary>
public class F_Point_Spawn : F_Point
{
    [Header("Constants Private")]
    private const float spawnChanceMin = 0f;
    private const float spawnChanceMax = 100f;

    [Header("Spawn Settings")]
    // Percentage chance that this point spawns anything from its list
    [Range(spawnChanceMin, spawnChanceMax)]
    public float spawnChance = spawnChanceMax;
    // Weighted options; one is picked when the spawn chance succeeds
    public List<F_Point_SpawnEntry> spawnList = new List<F_Point_SpawnEntry>();

    private void Start()
    {
        SpawnFromList();
    }

    private void OnValidate()
    {
        foreach (F_Point_SpawnEntry spawnEntry in spawnList)
        {
            spawnEntry?.ValidateSpawnCounts();
        }
    }

    /// <summary>Rolls the spawn chance, picks a weighted entry, rolls its count and spawns it at this point, in this point's scene.</summary>
    /// <returns>Every object spawned; empty when nothing was spawned.</returns>
    public List<GameObject> SpawnFromList()
    {
        List<GameObject> spawnedObjects = new List<GameObject>();
        if (spawnChance <= spawnChanceMin || Random.value * spawnChanceMax > spawnChance)
        {
            return spawnedObjects;
        }

        F_Point_SpawnEntry spawnEntry = PickWeightedSpawnEntry();
        if (spawnEntry == null)
        {
            return spawnedObjects;
        }

        int remainingCount = spawnEntry.RollSpawnCount();
        F_Item templateItem = spawnEntry.spawnObject.GetComponent<F_Item>();
        int countPerInstance = templateItem != null ? Mathf.Max(1, templateItem.ItemCountMaxValue) : 1;

        while (remainingCount > 0)
        {
            int instanceCount = Mathf.Min(remainingCount, countPerInstance);
            GameObject spawnedObject = SpawnInstance(spawnEntry.spawnObject);
            if (templateItem != null)
            {
                spawnedObject.GetComponent<F_Item>().itemCount = instanceCount;
            }

            spawnedObjects.Add(spawnedObject);
            remainingCount -= instanceCount;
        }

        return spawnedObjects;
    }

    // Creates one copy of the template at this point, keeping it in this point's scene
    private GameObject SpawnInstance(GameObject spawnTemplate)
    {
        GameObject spawnedObject = Instantiate(
            spawnTemplate,
            transform.position,
            transform.rotation * spawnTemplate.transform.rotation);
        SceneManager.MoveGameObjectToScene(spawnedObject, gameObject.scene);
        return spawnedObject;
    }

    // Picks one spawnable entry with probability proportional to its weight
    private F_Point_SpawnEntry PickWeightedSpawnEntry()
    {
        float totalWeight = 0f;
        foreach (F_Point_SpawnEntry spawnEntry in spawnList)
        {
            if (spawnEntry != null && spawnEntry.IsSpawnable)
            {
                totalWeight += spawnEntry.spawnWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float weightRoll = Random.value * totalWeight;
        F_Point_SpawnEntry lastSpawnableEntry = null;
        foreach (F_Point_SpawnEntry spawnEntry in spawnList)
        {
            if (spawnEntry == null || !spawnEntry.IsSpawnable)
            {
                continue;
            }

            lastSpawnableEntry = spawnEntry;
            weightRoll -= spawnEntry.spawnWeight;
            if (weightRoll < 0f)
            {
                return lastSpawnableEntry;
            }
        }

        // Random.value can return exactly 1, which lands on the end of the final entry
        return lastSpawnableEntry;
    }
}
