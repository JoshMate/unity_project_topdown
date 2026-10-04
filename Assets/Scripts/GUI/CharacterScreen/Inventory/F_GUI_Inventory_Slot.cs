using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class F_GUI_Inventory_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Constants Private")]
    private const int firstHotkeySlotIndex = 1;
    private const float hotkeyLabelOffset = 4f;
    private const float hotkeyLabelWidth = 28f;
    private const float hotkeyLabelHeight = 22f;
    private const float minimumHotkeyFontSize = 8f;
    private const float durabilityBarHeight = 6f;
    private const float durabilityBarPadding = 4f;

    [Header("Object Refs")]
    public F_Item slotItemObj;
    public F_Logic_Cursor cursorObj;
    public Image slotDrawBackGroundObj;
    public Image slotDrawBorderObj;
    public Image slotDrawItemObj;
    public TMP_Text slotItemCountText;

    [Header("Art")]
    public Sprite slotIconLocked;
    public Sprite slotIconPlaceHolder;
    public Sprite slotBrokenSprite;

    [Header("Durability GUI")]
    public F_GUI_Durability_Bar durabilityBar;
    public Image durabilityBrokenOverlay;

    [Header("Stats")]
    public enumSlotType slotType;

    [Header("InventorySlotFlags")]
    public bool isSlotLocked;
    public bool isSlotHovered;

    [Header("Privates")]
    private TMP_Text slotHotkeyText;
    private CanvasGroup slotCanvasGroup;
    [SerializeField] private enumInventorySlotHotkeyGroup slotHotkeyGroup;
    [SerializeField] private int slotHotkeyIndex = -1;
    private F_Logic_Controls controlsObj;

    /// <summary>Handles cursor item movement, stack merging, compatible slot swaps, and the right-click context menu.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSlotLocked || cursorObj == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowItemContextMenu(eventData);
            return;
        }

        if (cursorObj.cursorHeldItemObj != null)
        {
            if (!F_Utility_Helper_Inventory.CheckIfItemTypeMatchesSlotType(this, cursorObj.cursorHeldItemObj))
            {
                Debug.Log("The held item type does not fit in that slot!");
            }
            else if (!TryMergeHeldItemStack())
            {
                (cursorObj.cursorHeldItemObj, slotItemObj) = (slotItemObj, cursorObj.cursorHeldItemObj);
            }
        }
        else
        {
            (cursorObj.cursorHeldItemObj, slotItemObj) = (slotItemObj, cursorObj.cursorHeldItemObj);
        }
    }

    /// <summary>Opens the shared item context menu with the actions exposed by this slot's item.</summary>
    private void ShowItemContextMenu(PointerEventData eventData)
    {
        if (slotItemObj == null || cursorObj == null || cursorObj.itemContextMenu == null)
        {
            return;
        }

        F_PlayerInventory playerInventory = cursorObj.playerController != null
            ? cursorObj.playerController.playerInventory
            : null;
        cursorObj.itemContextMenu.Show(slotItemObj, eventData.position, playerInventory);
    }

    private bool TryMergeHeldItemStack()
    {
        F_Item heldItem = cursorObj.cursorHeldItemObj;
        int transferredCount = F_Utility_Helper_Inventory.MergeItemStacks(heldItem, slotItemObj);
        if (transferredCount <= 0)
        {
            return false;
        }

        if (heldItem.itemCount == 0)
        {
            F_PlayerInventory playerInventory = cursorObj.playerController != null
                ? cursorObj.playerController.playerInventory
                : null;
            F_Utility_Helper_Inventory.RemoveItemFromInventory(heldItem, playerInventory);
            cursorObj.cursorHeldItemObj = null;
        }

        return true;
    }

    /// <summary>Marks this slot as hovered by the pointer.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSlotLocked)
        {
            isSlotHovered = true;
        }
    }

    /// <summary>Clears the hover state when the pointer leaves this slot.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isSlotHovered = false;
    }

    /// <summary>Sets whether this slot is locked and immediately updates its input and border state.</summary>
    /// <param name="locked">True to prevent interaction with the slot.</param>
    public void SetSlotLocked(bool locked)
    {
        isSlotLocked = locked;
        ApplySlotLockState();
    }

    private void ApplySlotLockState()
    {
        if (isSlotLocked)
        {
            isSlotHovered = false;
        }

        if (slotCanvasGroup != null)
        {
            slotCanvasGroup.interactable = !isSlotLocked;
            slotCanvasGroup.blocksRaycasts = !isSlotLocked;
        }

        if (slotDrawBorderObj != null)
        {
            slotDrawBorderObj.enabled = !isSlotLocked;
        }
    }

    private void OnDisable()
    {
        isSlotHovered = false;
    }

    private void Awake()
    {
        slotCanvasGroup = GetComponent<CanvasGroup>();
        if (slotCanvasGroup == null)
        {
            slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ApplySlotLockState();

        CreateDurabilityGui();

        if (HasHotkeyBinding())
        {
            CreateSlotHotkeyHint();
        }
    }

    private bool HasHotkeyBinding()
    {
        return slotHotkeyGroup != enumInventorySlotHotkeyGroup.None && slotHotkeyIndex >= firstHotkeySlotIndex;
    }

    private void CreateSlotHotkeyHint()
    {
        GameObject keyHintObject = new GameObject(
            "Slot_KeyHint",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        keyHintObject.transform.SetParent(transform, false);
        keyHintObject.transform.SetAsLastSibling();

        RectTransform keyHintRect = keyHintObject.GetComponent<RectTransform>();
        keyHintRect.anchorMin = Vector2.zero;
        keyHintRect.anchorMax = Vector2.zero;
        keyHintRect.anchoredPosition = new Vector2(hotkeyLabelOffset, hotkeyLabelOffset);
        keyHintRect.sizeDelta = new Vector2(hotkeyLabelWidth, hotkeyLabelHeight);
        keyHintRect.pivot = Vector2.zero;

        slotHotkeyText = keyHintObject.GetComponent<TMP_Text>();
        slotHotkeyText.text = string.Empty;
        slotHotkeyText.alignment = TextAlignmentOptions.BottomLeft;
        slotHotkeyText.raycastTarget = false;
        slotHotkeyText.enableAutoSizing = true;
        slotHotkeyText.fontSizeMin = minimumHotkeyFontSize;

        if (slotItemCountText != null)
        {
            slotHotkeyText.font = slotItemCountText.font;
            slotHotkeyText.fontSharedMaterial = slotItemCountText.fontSharedMaterial;
            slotHotkeyText.color = slotItemCountText.color;
            slotHotkeyText.fontStyle = slotItemCountText.fontStyle;
            slotHotkeyText.fontSize = slotItemCountText.fontSize;
            slotHotkeyText.fontSizeMax = slotItemCountText.fontSize;
        }
    }

    private void DrawSlotIcon()
    {
        Sprite slotIconToDraw = isSlotLocked
            ? slotIconLocked
            : slotItemObj != null ? slotItemObj.itemSprite : slotIconPlaceHolder;

        if (slotIconToDraw == null || slotDrawItemObj == null)
        {
            return;
        }

        slotDrawItemObj.sprite = slotIconToDraw;
        slotDrawItemObj.preserveAspect = true;
        slotDrawItemObj.rectTransform.localScale = Vector3.one;
    }

    private void UpdateSlotHotkeyHint()
    {
        if (slotHotkeyText == null)
        {
            return;
        }

        if (controlsObj == null)
        {
            F_Logic_GameManager gameManager = FindFirstObjectByType<F_Logic_GameManager>();
            if (gameManager != null)
            {
                controlsObj = gameManager.controlsObject;
            }
        }

        string keyHint = isSlotLocked || controlsObj == null
            ? string.Empty
            : controlsObj.GetSlotKeyDisplayName(slotHotkeyGroup, slotHotkeyIndex);
        if (slotHotkeyText.text != keyHint)
        {
            slotHotkeyText.text = keyHint;
        }
    }

    private void CreateDurabilityGui()
    {
        if (durabilityBrokenOverlay == null)
        {
            GameObject overlayObject = new GameObject(
                "Slot_BrokenOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            overlayObject.transform.SetParent(transform, false);

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            F_Utility_Helper_Gui.AnchorRectToSpriteArea(overlayRect, slotBrokenSprite);

            durabilityBrokenOverlay = overlayObject.GetComponent<Image>();
            durabilityBrokenOverlay.sprite = slotBrokenSprite;
            durabilityBrokenOverlay.preserveAspect = false;
            durabilityBrokenOverlay.raycastTarget = false;
            durabilityBrokenOverlay.enabled = false;
        }

        if (durabilityBar == null)
        {
            durabilityBar = F_GUI_Durability_Bar.Create("Slot_DurabilityBar", transform);
            RectTransform barRect = durabilityBar.GetComponent<RectTransform>();
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = new Vector2(durabilityBarPadding, durabilityBarPadding);
            barRect.offsetMax = new Vector2(-durabilityBarPadding, durabilityBarPadding + durabilityBarHeight);
            durabilityBar.Hide();
        }

        if (slotDrawItemObj != null && slotDrawItemObj.transform.parent == transform)
        {
            int itemSiblingIndex = slotDrawItemObj.transform.GetSiblingIndex();
            durabilityBrokenOverlay.transform.SetSiblingIndex(itemSiblingIndex + 1);
            durabilityBar.transform.SetSiblingIndex(itemSiblingIndex + 2);
        }
    }

    private void UpdateDurabilityGui()
    {
        F_Item_Weapon weapon = isSlotLocked ? null : slotItemObj as F_Item_Weapon;
        if (durabilityBar != null)
        {
            durabilityBar.SetDurability(weapon);
        }

        if (durabilityBrokenOverlay != null)
        {
            durabilityBrokenOverlay.enabled = weapon != null && weapon.IsBroken() && slotBrokenSprite != null;
        }
    }

    private void Update()
    {
        ApplySlotLockState();
        if (slotDrawBorderObj != null && !isSlotLocked)
        {
            slotDrawBorderObj.color = isSlotHovered ? F_Utility_Config_Colours.cfgColourInventorySlotBorderHovered : F_Utility_Config_Colours.cfgColourInventorySlotBorderDefault;
        }

        DrawSlotIcon();
        UpdateSlotHotkeyHint();
        UpdateDurabilityGui();

        if (slotItemCountText != null)
        {
            slotItemCountText.text = isSlotLocked || slotItemObj == null || slotItemObj.itemCount == 1
                ? string.Empty
                : slotItemObj.itemCount.ToString();
        }
    }
}
