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

    private void Update()
    {
        ApplySlotLockState();
        if (slotDrawBorderObj != null && !isSlotLocked)
        {
            slotDrawBorderObj.color = isSlotHovered ? Color.white : Color.gray;
        }

        DrawSlotIcon();
        UpdateSlotHotkeyHint();

        if (slotItemCountText != null)
        {
            slotItemCountText.text = isSlotLocked || slotItemObj == null || slotItemObj.itemCount == 1
                ? string.Empty
                : slotItemObj.itemCount.ToString();
        }
    }
}
