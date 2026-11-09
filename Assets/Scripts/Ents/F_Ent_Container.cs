using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An ent that stores items in a configurable number of slots, or grows by rows when infinite size is enabled. At start it rolls each loot table,
/// keeping whatever fits in finite containers, and drops all held items onto free grid cells around it when destroyed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class F_Ent_Container : F_Ent
{
    [Header("Constants Public")]
    public const int containerSlotsPerRow = 6;

    [Header("Constants Private")]
    private const int containerSlotCountMin = 1;
    // How many rings of grid cells around the container are searched for free space when it drops its items
    private const int containerDropSearchRadius = 5;
    private const string containerDefaultOpenSoundAssetPath = "Assets/Sound/Ents/Container/SD_Container_Open.ogg";
    private const float containerSoundVolume = 100f;

    [Header("Container Settings")]
    // Number of item slots this container has when infinite size is disabled
    [Min(containerSlotCountMin)]
    public int containerSlotCount = 8;
    // When true, the container is removed once the last item is taken out of it
    public bool containerDestroyWhenEmpty = false;
    // When true, the container grows and shrinks in rows as items are added or removed
    public bool containerIsInfiniteSize = false;
    // When true, the player can take items but cannot place items into this container
    public bool containerTakeOnly = false;
    
    [Header("Container Loot")]
    // Each table is rolled once when the game starts; results that do not fit in the remaining slots are ignored
    public List<F_LootTable> containerLootTables = new List<F_LootTable>();

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

    // Rolls every loot table in order and adds each item result; finite containers discard anything that does not fit
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

    /// <summary>Ensures the container has at least the requested number of slots.</summary>
    /// <param name="slotCount">The minimum slot count to provide.</param>
    public void EnsureContainerSlotCount(int slotCount)
    {
        int targetSlotCount = Mathf.Max(containerSlotCountMin, slotCount);
        while (containerItems.Count < targetSlotCount)
        {
            containerItems.Add(null);
        }
    }

    /// <summary>Removes the specified row when every slot in it is empty, keeping the first row intact.</summary>
    /// <param name="rowSize">The number of slots in a row.</param>
    /// <param name="rowIndex">The zero-based row to remove.</param>
    /// <returns>True when an empty row was removed.</returns>
    public bool RemoveEmptyContainerSlotRow(int rowSize, int rowIndex)
    {
        int rowStartIndex = rowIndex * rowSize;
        if (rowSize <= 0 || rowIndex <= 0 || rowStartIndex + rowSize > containerItems.Count)
        {
            return false;
        }

        for (int slotIndex = rowStartIndex; slotIndex < rowStartIndex + rowSize; slotIndex++)
        {
            if (containerItems[slotIndex] != null)
            {
                return false;
            }
        }

        containerItems.RemoveRange(rowStartIndex, rowSize);
        return true;
    }

    // Keeps one list entry per slot so items can be addressed by slot index
    private void ResizeContainerSlots()
    {
        int slotCount = containerIsInfiniteSize
            ? containerSlotsPerRow
            : Mathf.Max(containerSlotCountMin, containerSlotCount);
        EnsureContainerSlotCount(slotCount);

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
