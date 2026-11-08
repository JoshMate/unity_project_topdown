using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An ent that stores items in a configurable number of slots. When the game starts it rolls each of its loot tables
/// and keeps whatever fits; anything left over is ignored. The player opens it with the interact key to move items in and out,
/// and it drops all held items onto free grid cells around it when destroyed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class F_Ent_Container : F_Ent
{
    [Header("Constants Private")]
    private const int containerSlotCountMin = 1;
    // How many rings of grid cells around the container are searched for free space when it drops its items
    private const int containerDropSearchRadius = 5;
    private const string containerDefaultOpenSoundAssetPath = "Assets/Sound/Ents/Container/SD_Container_Open.ogg";
    private const float containerSoundVolume = 100f;

    [Header("Container Settings")]
    // Number of item slots this container has
    [Min(containerSlotCountMin)]
    public int containerSlotCount = 8;
    // Each table is rolled once when the game starts; results that do not fit in the remaining slots are ignored
    public List<F_LootTable> containerLootTables = new List<F_LootTable>();
    // When true, the container is removed once the last item is taken out of it
    public bool containerDestroyWhenEmpty = false;

    [Header("Container Art")]
    // Played when the container is opened; defaults to SD_Container_Open when left empty
    public AudioClip containerSoundOpen;

    [Header("Privates")]
    private readonly List<F_Item> containerItems = new List<F_Item>();
    private bool containerHasHeldItems = false;

    /// <summary>The items held in each slot, indexed by slot; empty slots are null. Modify only through F_Utility_Helper_Inventory or F_GUI_Container.</summary>
    public List<F_Item> ContainerItems => containerItems;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (containerSoundOpen == null)
        {
            containerSoundOpen = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(containerDefaultOpenSoundAssetPath);
        }
#endif
    }

    /// <summary>Plays this container's open sound at its position.</summary>
    public void PlayOpenSound()
    {
        if (containerSoundOpen != null)
        {
            F_Logic_Audio.PlaySound(containerSoundOpen, EnumSoundType.Spatial, EnumSoundLoudness.NoSound, containerSoundVolume, transform.position);
        }
    }

    /// <summary>Removes the container when Destroy When Empty is enabled and its last item has been taken out.
    /// A container that never held anything is kept, so a failed loot roll does not remove it immediately.</summary>
    /// <returns>True when the container is being destroyed.</returns>
    public bool TryDestroyWhenEmpty()
    {
        bool isEmpty = !containerItems.Exists(containerItem => containerItem != null);
        if (!isEmpty)
        {
            containerHasHeldItems = true;
            return false;
        }

        if (!containerDestroyWhenEmpty || !containerHasHeldItems || IsDead)
        {
            return false;
        }

        Destroy(gameObject);
        return true;
    }

    protected override void Awake()
    {
        base.Awake();
        F_Utility_Helper_Sprite.ApplyPhysicsShape(GetComponent<PolygonCollider2D>(), GetComponent<SpriteRenderer>().sprite);
        ResizeContainerSlots();
    }

    protected override void Start()
    {
        base.Start();
        FillFromLootTables();
    }

    // Rolls every loot table in order and adds each item result; the helper discards whatever no longer fits
    private void FillFromLootTables()
    {
        foreach (F_LootTable containerLootTable in containerLootTables)
        {
            if (containerLootTable == null || !containerLootTable.RollLoot(out GameObject lootObject, out int lootCount))
            {
                continue;
            }

            F_Item lootItem = lootObject.GetComponent<F_Item>();
            if (lootItem != null)
            {
                F_Utility_Helper_Inventory.AddItemToContainer(lootItem, lootCount, this);
            }
        }

        // Records whether the rolls produced anything, so Destroy When Empty only triggers once items have been taken out
        TryDestroyWhenEmpty();
    }

    // Keeps one list entry per slot so items can be addressed by slot index
    private void ResizeContainerSlots()
    {
        int slotCount = Mathf.Max(containerSlotCountMin, containerSlotCount);
        while (containerItems.Count < slotCount)
        {
            containerItems.Add(null);
        }

        if (containerItems.Count > slotCount)
        {
            containerItems.RemoveRange(slotCount, containerItems.Count - slotCount);
        }
    }

    // OnDied fires before this, so an open container GUI has already written its slots back
    protected override void OnDeath()
    {
        base.OnDeath();
        F_Utility_Helper_Inventory.DropContainerContents(this, containerDropSearchRadius);
    }
}
