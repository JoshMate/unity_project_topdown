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
    private const string containerGuiSizeTextName = "Text_Container_Size";
    private const string containerGuiWeightTextName = "Text_Container_Weight";
    private const string containerGuiSlotPaddingName = "Container_HeaderSlotPadding";
    private const string containerGuiTakeAllButtonName = "Button_Container_TakeAll";
    private const string containerGuiTakeAllPaddingName = "Container_TakeAllBottomPadding";
    private const string containerGuiSizeFormat = "Size: {0}";
    private const string containerGuiWeightFormat = "Weight: {0:0.##}";
    private const string containerGuiSizeInfiniteLabel = "Infinite";
    private const string containerGuiTakeAllLabel = "Take All";
    private const float containerGuiBorderThickness = 2f;
    private const float containerGuiInfoFontSize = 18f;
    private const float containerGuiInfoHeight = 24f;
    private const float containerGuiHeaderSlotPadding = 14f;
    private const float containerGuiTakeAllVerticalPadding = 14f;
    private const float containerGuiTakeAllButtonWidth = 140f;
    private const float containerGuiTakeAllButtonHeight = 32f;
    private const float containerGuiButtonHorizontalPadding = 8f;

    private static readonly Color containerGuiButtonHighlightColour = new Color(
        F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier,
        F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier,
        F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier,
        1f);
    private static readonly Color containerGuiButtonPressedColour = new Color(
        F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier,
        F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier,
        F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier,
        1f);

    [Header("Object Refs")]
    public F_Logic_Cursor containerGuiCursor;
    public F_GUI_Inventory_Slot containerGuiSlotPrefab;
    public GridLayoutGroup containerGuiSlotGrid;
    public TMP_Text containerGuiTitleText;
    public TMP_Text containerGuiSizeText;
    public Image containerGuiBorderImage;
    public Image containerGuiFillImage;

    [Header("Privates")]
    private readonly List<F_GUI_Inventory_Slot> containerGuiSlots = new List<F_GUI_Inventory_Slot>();
    private F_Ent_Container containerGuiOpenContainer;
    private TMP_Text containerGuiWeightText;
    private Button containerGuiTakeAllButton;
    private int containerGuiBoundSlotCount;

    /// <summary>Whether a container is currently shown.</summary>
    public bool IsOpen => containerGuiOpenContainer != null;

    /// <summary>The container currently shown, or null.</summary>
    public F_Ent_Container OpenedContainer => containerGuiOpenContainer;

    private void Awake()
    {
        EnsureSizeText();
        EnsureWeightText();
        EnsureHeaderSlotPadding();
        EnsureTakeAllButton();
        EnsureTakeAllButtonPadding();
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

        EnsureSizeText();
        EnsureWeightText();
        UpdateContainerSizeText();
        UpdateContainerWeightText();

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
            slot.SetContainerTakeOnly(false);
        }

        if (containerGuiTitleText != null)
        {
            containerGuiTitleText.text = string.Empty;
        }

        if (containerGuiSizeText != null)
        {
            containerGuiSizeText.text = string.Empty;
        }

        if (containerGuiWeightText != null)
        {
            containerGuiWeightText.text = string.Empty;
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
            else
            {
                UpdateContainerSlotLockStates();
            }

            UpdateContainerWeightText();
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

    // Transfers container items in slot order, moving a partial final stack when that fills the inventory
    private void TakeAllItems()
    {
        F_PlayerInventory playerInventory = GetPlayerInventory();
        if (containerGuiOpenContainer == null || playerInventory == null)
        {
            return;
        }

        SyncSlotsToContainer();
        List<F_Item> containerItems = containerGuiOpenContainer.ContainerItems;
        int slotCount = Mathf.Min(containerItems.Count, containerGuiSlots.Count);
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            F_Item itemToMove = containerItems[slotIndex];
            if (itemToMove == null)
            {
                continue;
            }

            int requestedCount = itemToMove.itemCount;
            int transferCount = F_Utility_Helper_Inventory.GetInventoryItemCapacity(
                itemToMove, playerInventory, requestedCount);
            if (transferCount <= 0)
            {
                break;
            }

            int inventoryItemCountBefore = F_Utility_Helper_Inventory.GetItemCountInInventory(playerInventory, itemToMove);
            bool addSucceeded = F_Utility_Helper_Inventory.AddItemToInventory(itemToMove, transferCount, playerInventory);
            int inventoryItemCountAfter = F_Utility_Helper_Inventory.GetItemCountInInventory(playerInventory, itemToMove);
            int transferredCount = Mathf.Clamp(inventoryItemCountAfter - inventoryItemCountBefore, 0, transferCount);
            if (transferredCount <= 0)
            {
                break;
            }

            itemToMove.itemCount -= transferredCount;
            if (itemToMove.itemCount <= 0)
            {
                containerItems[slotIndex] = null;
                containerGuiSlots[slotIndex].slotItemObj = null;
                containerGuiSlots[slotIndex].SetContainerTakeOnly(containerGuiOpenContainer.containerTakeOnly);
                F_Utility_Helper_Inventory.RemoveItemFromInventory(itemToMove, playerInventory);
            }

            if (!addSucceeded || transferredCount < requestedCount)
            {
                break;
            }
        }

        bool slotRowsChanged = UpdateInfiniteSlotRows();
        if (slotRowsChanged || containerGuiBoundSlotCount != containerItems.Count)
        {
            RefreshContainerSlots(containerItems);
        }

        UpdateContainerWeightText();
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
            slot.SetContainerTakeOnly(isUsed && containerGuiOpenContainer.containerTakeOnly);
            slot.gameObject.SetActive(isUsed);
            if (isUsed)
            {
                playerInventory?.invSlotContainer.Add(slot);
            }
        }

        containerGuiBoundSlotCount = containerItems.Count;
    }

    private void UpdateContainerSlotLockStates()
    {
        if (containerGuiOpenContainer == null)
        {
            return;
        }

        int slotCount = Mathf.Min(containerGuiOpenContainer.ContainerItems.Count, containerGuiSlots.Count);
        for (int slotIndex = 0; slotIndex < containerGuiSlots.Count; slotIndex++)
        {
            F_GUI_Inventory_Slot slot = containerGuiSlots[slotIndex];
            bool isUsed = slotIndex < slotCount;
            slot.SetContainerTakeOnly(isUsed && containerGuiOpenContainer.containerTakeOnly);
        }
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

    // Updates the size label to reflect whether the container has a fixed slot count or infinite capacity
    private void UpdateContainerSizeText()
    {
        if (containerGuiSizeText == null || containerGuiOpenContainer == null)
        {
            return;
        }

        string sizeValue = containerGuiOpenContainer.containerIsInfiniteSize
            ? containerGuiSizeInfiniteLabel
            : containerGuiOpenContainer.containerSlotCount.ToString();
        containerGuiSizeText.text = string.Format(containerGuiSizeFormat, sizeValue);
    }

    // Uses the player's item-weight calculation so stack and loaded-ammunition weight match carried weight rules
    private void UpdateContainerWeightText()
    {
        if (containerGuiWeightText == null || containerGuiOpenContainer == null)
        {
            return;
        }

        F_PlayerInventory playerInventory = GetPlayerInventory();
        float containerWeight = playerInventory != null
            ? playerInventory.CalculateWeightForItems(containerGuiOpenContainer.ContainerItems)
            : 0f;
        containerGuiWeightText.text = string.Format(containerGuiWeightFormat, containerWeight);
    }

    private void EnsureSizeText()
    {
        containerGuiSizeText = EnsureInfoText(containerGuiSizeText, containerGuiSizeTextName, 1);
    }

    private void EnsureWeightText()
    {
        containerGuiWeightText = EnsureInfoText(containerGuiWeightText, containerGuiWeightTextName, 2);
    }

    private TMP_Text EnsureInfoText(TMP_Text infoText, string objectName, int titleSiblingOffset)
    {
        Transform existingText = transform.Find(objectName);
        if (infoText == null && existingText != null)
        {
            infoText = existingText.GetComponent<TMP_Text>();
        }

        if (infoText == null)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            textObject.transform.SetParent(transform, false);
            textObject.layer = gameObject.layer;
            infoText = textObject.GetComponent<TMP_Text>();
        }

        LayoutElement layoutElement = infoText.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = infoText.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = containerGuiInfoHeight;
        layoutElement.flexibleWidth = 1f;
        RectTransform textRect = infoText.rectTransform;
        textRect.sizeDelta = new Vector2(0f, containerGuiInfoHeight);

        if (containerGuiTitleText != null)
        {
            infoText.font = containerGuiTitleText.font;
            infoText.fontSharedMaterial = containerGuiTitleText.fontSharedMaterial;
        }

        int titleSiblingIndex = containerGuiTitleText != null
            ? containerGuiTitleText.transform.GetSiblingIndex()
            : 0;
        int targetSiblingIndex = Mathf.Clamp(titleSiblingIndex + titleSiblingOffset, 0, transform.childCount - 1);
        infoText.transform.SetSiblingIndex(targetSiblingIndex);
        return infoText;
    }

    // Adds extra vertical space between the header and the first inventory row
    private void EnsureHeaderSlotPadding()
    {
        Transform paddingTransform = transform.Find(containerGuiSlotPaddingName);
        if (paddingTransform == null)
        {
            GameObject paddingObject = new GameObject(
                containerGuiSlotPaddingName,
                typeof(RectTransform),
                typeof(LayoutElement));
            paddingObject.transform.SetParent(transform, false);
            paddingObject.layer = gameObject.layer;
            paddingTransform = paddingObject.transform;
        }

        LayoutElement paddingLayout = paddingTransform.GetComponent<LayoutElement>();
        if (paddingLayout == null)
        {
            paddingLayout = paddingTransform.gameObject.AddComponent<LayoutElement>();
        }

        paddingLayout.minHeight = containerGuiHeaderSlotPadding;
        paddingLayout.preferredHeight = containerGuiHeaderSlotPadding;
        paddingLayout.flexibleHeight = 0f;

        int weightSiblingIndex = containerGuiWeightText != null
            ? containerGuiWeightText.transform.GetSiblingIndex()
            : containerGuiTitleText != null ? containerGuiTitleText.transform.GetSiblingIndex() + 2 : 2;
        paddingTransform.SetSiblingIndex(Mathf.Clamp(weightSiblingIndex + 1, 0, transform.childCount - 1));
    }

    // Adds spacing above the button equal to the panel's bottom inset
    private void EnsureTakeAllButtonPadding()
    {
        Transform paddingTransform = transform.Find(containerGuiTakeAllPaddingName);
        if (paddingTransform == null)
        {
            GameObject paddingObject = new GameObject(
                containerGuiTakeAllPaddingName,
                typeof(RectTransform),
                typeof(LayoutElement));
            paddingObject.transform.SetParent(transform, false);
            paddingObject.layer = gameObject.layer;
            paddingTransform = paddingObject.transform;
        }

        LayoutElement paddingLayout = paddingTransform.GetComponent<LayoutElement>();
        if (paddingLayout == null)
        {
            paddingLayout = paddingTransform.gameObject.AddComponent<LayoutElement>();
        }

        paddingLayout.minHeight = containerGuiTakeAllVerticalPadding;
        paddingLayout.preferredHeight = containerGuiTakeAllVerticalPadding;
        paddingLayout.flexibleHeight = 0f;
        if (containerGuiTakeAllButton != null)
        {
            paddingTransform.SetSiblingIndex(containerGuiTakeAllButton.transform.GetSiblingIndex());
        }
    }

    // Creates the bottom-centred button using the shared GUI option colours and button state styling
    private void EnsureTakeAllButton()
    {
        Transform buttonTransform = transform.Find(containerGuiTakeAllButtonName);
        if (buttonTransform == null)
        {
            GameObject buttonObject = new GameObject(
                containerGuiTakeAllButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.layer = gameObject.layer;
            buttonTransform = buttonObject.transform;
        }

        containerGuiTakeAllButton = buttonTransform.GetComponent<Button>();
        if (containerGuiTakeAllButton == null)
        {
            containerGuiTakeAllButton = buttonTransform.gameObject.AddComponent<Button>();
        }

        LayoutElement buttonLayout = buttonTransform.GetComponent<LayoutElement>();
        if (buttonLayout == null)
        {
            buttonLayout = buttonTransform.gameObject.AddComponent<LayoutElement>();
        }

        buttonLayout.preferredWidth = containerGuiTakeAllButtonWidth;
        buttonLayout.minWidth = containerGuiTakeAllButtonWidth;
        buttonLayout.preferredHeight = containerGuiTakeAllButtonHeight;
        buttonLayout.minHeight = containerGuiTakeAllButtonHeight;

        Image buttonImage = buttonTransform.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonTransform.gameObject.AddComponent<Image>();
        }

        buttonImage.color = F_Utility_Config_Colours.cfgColourGuiOptionBackground;
        buttonImage.raycastTarget = true;
        containerGuiTakeAllButton.targetGraphic = buttonImage;
        ColorBlock buttonColours = containerGuiTakeAllButton.colors;
        buttonColours.normalColor = Color.white;
        buttonColours.highlightedColor = containerGuiButtonHighlightColour;
        buttonColours.pressedColor = containerGuiButtonPressedColour;
        buttonColours.selectedColor = Color.white;
        buttonColours.fadeDuration = 0.05f;
        containerGuiTakeAllButton.colors = buttonColours;
        containerGuiTakeAllButton.onClick.RemoveListener(TakeAllItems);
        containerGuiTakeAllButton.onClick.AddListener(TakeAllItems);

        Transform labelTransform = buttonTransform.Find("TakeAll_Label");
        if (labelTransform == null)
        {
            GameObject labelObject = new GameObject(
                "TakeAll_Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonTransform, false);
            labelObject.layer = buttonTransform.gameObject.layer;
            labelTransform = labelObject.transform;
        }

        RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(containerGuiButtonHorizontalPadding, 0f);
        labelRect.offsetMax = new Vector2(-containerGuiButtonHorizontalPadding, 0f);

        TMP_Text buttonLabel = labelTransform.GetComponent<TMP_Text>();
        buttonLabel.text = containerGuiTakeAllLabel;
        buttonLabel.color = F_Utility_Config_Colours.cfgColourGuiText;
        buttonLabel.fontSize = containerGuiInfoFontSize;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.raycastTarget = false;
        if (containerGuiTitleText != null)
        {
            buttonLabel.font = containerGuiTitleText.font;
            buttonLabel.fontSharedMaterial = containerGuiTitleText.fontSharedMaterial;
        }

        buttonTransform.SetAsLastSibling();
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
            RectTransform fillRect = containerGuiFillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(containerGuiBorderThickness, containerGuiBorderThickness);
            fillRect.offsetMax = new Vector2(-containerGuiBorderThickness, -containerGuiBorderThickness);
        }

        if (containerGuiTitleText != null)
        {
            containerGuiTitleText.color = F_Utility_Config_Colours.cfgColourGuiText;
            containerGuiTitleText.fontStyle = FontStyles.Bold;
            containerGuiTitleText.alignment = TextAlignmentOptions.Center;
        }

        if (containerGuiSizeText != null)
        {
            containerGuiSizeText.color = F_Utility_Config_Colours.cfgColourGuiText;
            containerGuiSizeText.fontSize = containerGuiInfoFontSize;
            containerGuiSizeText.fontStyle = FontStyles.Normal;
            containerGuiSizeText.alignment = TextAlignmentOptions.Center;
            containerGuiSizeText.raycastTarget = false;
            containerGuiSizeText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (containerGuiWeightText != null)
        {
            containerGuiWeightText.color = F_Utility_Config_Colours.cfgColourGuiText;
            containerGuiWeightText.fontSize = containerGuiInfoFontSize;
            containerGuiWeightText.fontStyle = FontStyles.Normal;
            containerGuiWeightText.alignment = TextAlignmentOptions.Center;
            containerGuiWeightText.raycastTarget = false;
            containerGuiWeightText.textWrappingMode = TextWrappingModes.NoWrap;
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
