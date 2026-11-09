using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character screen panel showing the slots of the currently open container. Slots are standard inventory slots,
/// so items move in and out exactly like the player's own inventory. While open, the slots are bound to the
/// container's items and written back every frame; the panel closes when the container dies or moves out of range.
/// </summary>
public class F_GUI_Container : MonoBehaviour
{
    [Header("Constants Private")]
    private const int containerGuiColumnCount = F_Ent_Container.containerSlotsPerRow;
    private const string containerGuiSlotNamePrefix = "Inventory_Slot_Container_";

    [Header("Object Refs")]
    public F_Logic_Cursor containerGuiCursor;
    public F_GUI_Inventory_Slot containerGuiSlotPrefab;
    public GridLayoutGroup containerGuiSlotGrid;
    public TMP_Text containerGuiTitleText;
    public Image containerGuiBorderImage;
    public Image containerGuiFillImage;

    [Header("Privates")]
    private readonly List<F_GUI_Inventory_Slot> containerGuiSlots = new List<F_GUI_Inventory_Slot>();
    private F_Ent_Container containerGuiOpenContainer;
    private int containerGuiBoundSlotCount;

    /// <summary>Whether a container is currently shown.</summary>
    public bool IsOpen => containerGuiOpenContainer != null;

    /// <summary>The container currently shown, or null.</summary>
    public F_Ent_Container OpenedContainer => containerGuiOpenContainer;

    private void Awake()
    {
        ApplyStyle();
    }

    /// <summary>Shows a container's slots and binds them to its items. Closes any other open container first.</summary>
    /// <param name="container">The container to show.</param>
    public void OpenContainer(F_Ent_Container container)
    {
        if (container == null || container.IsDead || container == containerGuiOpenContainer)
        {
            return;
        }

        CloseContainer();
        containerGuiOpenContainer = container;
        containerGuiOpenContainer.OnDied += HandleOpenContainerDied;
        UpdateInfiniteSlotRows();

        List<F_Item> containerItems = container.ContainerItems;
        EnsureSlotCount(containerItems.Count);
        RefreshContainerSlots(containerItems);

        if (containerGuiTitleText != null)
        {
            containerGuiTitleText.text = container.entInfoName;
        }

        gameObject.SetActive(true);
        container.PlayOpenSound();
    }

    /// <summary>Writes the slots back to the open container, unbinds them and hides the panel.</summary>
    public void CloseContainer()
    {
        if (containerGuiOpenContainer != null)
        {
            SyncSlotsToContainer();
            containerGuiOpenContainer.OnDied -= HandleOpenContainerDied;
        }

        containerGuiOpenContainer = null;
        containerGuiBoundSlotCount = 0;
        foreach (F_GUI_Inventory_Slot slot in containerGuiSlots)
        {
            slot.slotItemObj = null;
        }

        GetPlayerInventory()?.invSlotContainer.Clear();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (containerGuiOpenContainer == null)
        {
            if (gameObject.activeSelf)
            {
                CloseContainer();
            }
            return;
        }

        if (!IsContainerInRange(containerGuiOpenContainer))
        {
            CloseContainer();
        }
    }

    // Runs after the slots have handled this frame's clicks, so the container always matches what the slots show.
    // Closes straight away if that left a Destroy When Empty container empty, so nothing can be placed into it before it is removed
    private void LateUpdate()
    {
        SyncSlotsToContainer();
        if (containerGuiOpenContainer != null)
        {
            bool slotRowsChanged = UpdateInfiniteSlotRows();
            List<F_Item> containerItems = containerGuiOpenContainer.ContainerItems;
            if (slotRowsChanged || containerGuiBoundSlotCount != containerItems.Count)
            {
                RefreshContainerSlots(containerItems);
            }
        }

        if (containerGuiOpenContainer != null && containerGuiOpenContainer.TryDestroyWhenEmpty())
        {
            CloseContainer();
        }
    }

    /// <summary>Whether a container is close enough to the player to be opened or kept open.</summary>
    /// <param name="container">The container to check.</param>
    public bool IsContainerInRange(F_Ent_Container container)
    {
        if (container == null || containerGuiCursor == null || containerGuiCursor.playerController == null)
        {
            return false;
        }

        float distanceToPlayer = Vector2.Distance(container.transform.position, containerGuiCursor.playerController.transform.position);
        return distanceToPlayer <= containerGuiCursor.CursorInteractMaxDistance;
    }

    // Copies each bound slot's item into the matching container slot
    private void SyncSlotsToContainer()
    {
        if (containerGuiOpenContainer == null)
        {
            return;
        }

        List<F_Item> containerItems = containerGuiOpenContainer.ContainerItems;
        for (int slotIndex = 0; slotIndex < containerItems.Count && slotIndex < containerGuiSlots.Count; slotIndex++)
        {
            containerItems[slotIndex] = containerGuiSlots[slotIndex].slotItemObj;
        }
    }

    private bool UpdateInfiniteSlotRows()
    {
        if (containerGuiOpenContainer == null || !containerGuiOpenContainer.containerIsInfiniteSize)
        {
            return false;
        }

        List<F_Item> containerItems = containerGuiOpenContainer.ContainerItems;
        bool slotCountChanged = containerItems.Count < containerGuiColumnCount;
        containerGuiOpenContainer.EnsureContainerSlotCount(containerGuiColumnCount);

        int rowCount;
        bool rowRemoved;
        do
        {
            rowRemoved = false;
            rowCount = containerItems.Count / containerGuiColumnCount;
            for (int rowIndex = rowCount - 1; rowIndex > 0; rowIndex--)
            {
                if (!IsContainerSlotRowEmpty(containerItems, rowIndex))
                {
                    continue;
                }

                bool isTrailingRow = rowIndex == rowCount - 1;
                int rowStartIndex = rowIndex * containerGuiColumnCount;
                if (isTrailingRow && AreContainerSlotsFilledBeforeIndex(containerItems, rowStartIndex))
                {
                    continue;
                }

                if (containerGuiOpenContainer.RemoveEmptyContainerSlotRow(containerGuiColumnCount, rowIndex))
                {
                    slotCountChanged = true;
                    rowRemoved = true;
                    break;
                }
            }
        }
        while (rowRemoved);

        if (AreAllContainerSlotsFilled(containerItems))
        {
            containerGuiOpenContainer.EnsureContainerSlotCount(containerItems.Count + containerGuiColumnCount);
            slotCountChanged = true;
        }

        return slotCountChanged;
    }

    private bool AreAllContainerSlotsFilled(List<F_Item> containerItems)
    {
        if (containerItems.Count == 0)
        {
            return false;
        }

        foreach (F_Item containerItem in containerItems)
        {
            if (containerItem == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsContainerSlotRowEmpty(List<F_Item> containerItems, int rowIndex)
    {
        int rowStartIndex = rowIndex * containerGuiColumnCount;
        for (int slotIndex = rowStartIndex; slotIndex < rowStartIndex + containerGuiColumnCount; slotIndex++)
        {
            if (containerItems[slotIndex] != null)
            {
                return false;
            }
        }

        return true;
    }

    private bool AreContainerSlotsFilledBeforeIndex(List<F_Item> containerItems, int endIndex)
    {
        for (int slotIndex = 0; slotIndex < endIndex; slotIndex++)
        {
            if (containerItems[slotIndex] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshContainerSlots(List<F_Item> containerItems)
    {
        EnsureSlotCount(containerItems.Count);
        F_PlayerInventory playerInventory = GetPlayerInventory();
        playerInventory?.invSlotContainer.Clear();

        for (int slotIndex = 0; slotIndex < containerGuiSlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = containerGuiSlots[slotIndex];
            bool isUsed = slotIndex < containerItems.Count;
            slot.slotItemObj = isUsed ? containerItems[slotIndex] : null;
            slot.gameObject.SetActive(isUsed);
            if (isUsed)
            {
                playerInventory?.invSlotContainer.Add(slot);
            }
        }

        containerGuiBoundSlotCount = containerItems.Count;
    }

    // Closes before the container drops its items, so the drop sees every item the player left inside
    private void HandleOpenContainerDied(F_Ent deadEnt)
    {
        CloseContainer();
    }

    // Creates slots until there are enough for the container; extra slots are hidden rather than destroyed
    private void EnsureSlotCount(int slotCount)
    {
        while (containerGuiSlots.Count < slotCount)
        {
            F_GUI_Inventory_Slot slot = Instantiate(containerGuiSlotPrefab, containerGuiSlotGrid.transform);
            slot.name = containerGuiSlotNamePrefix + containerGuiSlots.Count;
            slot.cursorObj = containerGuiCursor;
            slot.slotType = enumSlotType.MiscAny;
            containerGuiSlots.Add(slot);
        }
    }

    // Applies the shared GUI panel colours and grid layout so the panel matches the rest of the interface
    private void ApplyStyle()
    {
        if (containerGuiBorderImage != null)
        {
            containerGuiBorderImage.color = F_Utility_Config_Colours.cfgColourGuiPanelBorder;
        }

        if (containerGuiFillImage != null)
        {
            containerGuiFillImage.color = F_Utility_Config_Colours.cfgColourGuiPanelFill;
        }

        if (containerGuiTitleText != null)
        {
            containerGuiTitleText.color = F_Utility_Config_Colours.cfgColourGuiText;
        }

        if (containerGuiSlotGrid != null)
        {
            containerGuiSlotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            containerGuiSlotGrid.constraintCount = containerGuiColumnCount;
        }
    }

    private F_PlayerInventory GetPlayerInventory()
    {
        return containerGuiCursor != null && containerGuiCursor.playerController != null
            ? containerGuiCursor.playerController.playerInventory
            : null;
    }
}
