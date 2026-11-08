using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A point that, when the game starts, rolls its loot table once and spawns the result at this point.
/// Items receive the rolled count (split into stacks if it exceeds the stack size); anything else spawns that many instances.
/// </summary>
public class F_Point_Spawn : F_Point
{
    [Header("Spawn Settings")]
    // The loot table rolled once to decide what this point spawns
    public F_LootTable spawnLootTable;

    private void Start()
    {
        // F_Point runs in edit mode, so only spawn while the game is running
        if (Application.isPlaying)
        {
            SpawnFromLootTable();
        }
    }

    /// <summary>Rolls this point's loot table and spawns the result at this point, in this point's scene.</summary>
    /// <returns>Every object spawned (more than one only when an item count is split into stacks); empty when nothing was spawned.</returns>
    public List<GameObject> SpawnFromLootTable()
    {
        List<GameObject> spawnedObjects = new List<GameObject>();
        if (spawnLootTable != null && spawnLootTable.RollLoot(out GameObject lootObject, out int lootCount))
        {
            SpawnLoot(lootObject, lootCount, spawnedObjects);
        }

        return spawnedObjects;
    }

    // Spawns the rolled count of a template, splitting items into stacks no larger than their max count
    private void SpawnLoot(GameObject lootObject, int lootCount, List<GameObject> spawnedObjects)
    {
        F_Item templateItem = lootObject.GetComponent<F_Item>();
        int countPerInstance = templateItem != null ? Mathf.Max(1, templateItem.ItemCountMaxValue) : 1;
        int remainingCount = lootCount;

        while (remainingCount > 0)
        {
            int instanceCount = Mathf.Min(remainingCount, countPerInstance);
            GameObject spawnedObject = SpawnInstance(lootObject);
            if (templateItem != null)
            {
                spawnedObject.GetComponent<F_Item>().itemCount = instanceCount;
            }

            spawnedObjects.Add(spawnedObject);
            remainingCount -= instanceCount;
        }
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
}
