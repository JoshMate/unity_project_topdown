using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class F_GUI_Inventory_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    [Header("Object Refs")]

    public F_Item slotItemObj;
    public F_Logic_Cursor cursorObj;
    public UnityEngine.UI.Image slotDrawBackGroundObj;
    public UnityEngine.UI.Image slotDrawBorderObj;
    public UnityEngine.UI.Image slotDrawItemObj;
    public TMP_Text slotItemCountText;
    private TMP_Text slotQuickKeyText;

    [SerializeField] private int quickSlotIndex = -1;

    private const int FirstQuickSlotIndex = 1;
    private const int LastQuickSlotIndex = 5;
    private F_Logic_Controls controlsObj;
    
    

    [Header("Art")]
    public Sprite slotIconLocked;
    public Sprite slotIconPlaceHolder;

    [Header("Stats")]
    public enumSlotType slotType;
    

    [Header("InventorySlotFlags")]
    public bool isSlotLocked;
    public bool isSlotHovered;

    public void OnPointerClick(PointerEventData eventData)
    {

        // Check if the Cursor is holding something first
        if (cursorObj.cursorHeldItemObj != null)
        {
            // First check if the cursor item type matches the slot type
            if (F_Utility_Helper_Inventory.CheckIfItemTypeMatchesSlotType(this,cursorObj.cursorHeldItemObj) == false)
            {
                Debug.Log("The held item type does not fit in that slot!");
            }
            else if (TryMergeHeldItemStack() == false)
            {
                // Swap when the items cannot be merged or the slot stack is full.
                (cursorObj.cursorHeldItemObj, slotItemObj) = (slotItemObj, cursorObj.cursorHeldItemObj);
            }
        }
        else
        {
            // Swap the item held between the cursor and the inventory slot (Using a posh Tuple)
            (cursorObj.cursorHeldItemObj, slotItemObj) = (slotItemObj, cursorObj.cursorHeldItemObj);
        }
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
            cursorObj.cursorHeldItemObj = null;
            Destroy(heldItem.gameObject);
        }

        return true;
    }

   public void OnPointerEnter(PointerEventData eventData)
   {
        isSlotHovered = true;
   }

   public void OnPointerExit(PointerEventData eventData)
   {
        isSlotHovered = false;
   }

    // Disable the slot highlight on closing the menu. Fixes the bug with persistent highlights.
    void OnDisable()
    {
        isSlotHovered = false;
    }



    private void Awake()
    {
        if (quickSlotIndex >= FirstQuickSlotIndex && quickSlotIndex <= LastQuickSlotIndex)
        {
            CreateQuickSlotKeyHint();
        }
    }

    private void CreateQuickSlotKeyHint()
    {
        GameObject keyHintObject = new GameObject(
            "Slot_QuickKey",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        keyHintObject.transform.SetParent(transform, false);
        keyHintObject.transform.SetAsLastSibling();

        RectTransform keyHintRect = keyHintObject.GetComponent<RectTransform>();
        keyHintRect.anchorMin = Vector2.zero;
        keyHintRect.anchorMax = Vector2.zero;
        keyHintRect.anchoredPosition = new Vector2(4f, 4f);
        keyHintRect.sizeDelta = new Vector2(28f, 22f);
        keyHintRect.pivot = Vector2.zero;

        slotQuickKeyText = keyHintObject.GetComponent<TMP_Text>();
        slotQuickKeyText.text = string.Empty;
        slotQuickKeyText.alignment = TextAlignmentOptions.BottomLeft;
        slotQuickKeyText.raycastTarget = false;
        slotQuickKeyText.enableAutoSizing = true;
        slotQuickKeyText.fontSizeMin = 8f;

        if (slotItemCountText != null)
        {
            slotQuickKeyText.font = slotItemCountText.font;
            slotQuickKeyText.fontSharedMaterial = slotItemCountText.fontSharedMaterial;
            slotQuickKeyText.color = slotItemCountText.color;
            slotQuickKeyText.fontStyle = slotItemCountText.fontStyle;
            slotQuickKeyText.fontSize = slotItemCountText.fontSize;
            slotQuickKeyText.fontSizeMax = slotItemCountText.fontSize;
        }
    }

    void DrawSlotIcon() {

        Sprite slotIconToDraw = slotIconPlaceHolder;

        if (isSlotLocked == true) slotIconToDraw = slotIconLocked;
        if (slotItemObj != null) slotIconToDraw = slotItemObj.GetComponent<SpriteRenderer>().sprite;
        
        
        
        if (slotIconToDraw == null)
        {
            return;
        }
        else
        {
            slotDrawItemObj.sprite = slotIconToDraw;
        }

    }

    private void UpdateQuickSlotKeyHint()
    {
        if (slotQuickKeyText == null)
        {
            return;
        }

        bool isQuickSlot = quickSlotIndex >= FirstQuickSlotIndex && quickSlotIndex <= LastQuickSlotIndex;
        if (slotQuickKeyText.gameObject.activeSelf != isQuickSlot)
        {
            slotQuickKeyText.gameObject.SetActive(isQuickSlot);
        }

        if (!isQuickSlot)
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

        string keyHint = controlsObj == null ? string.Empty : controlsObj.GetQuickSlotKeyDisplayName(quickSlotIndex);
        if (slotQuickKeyText.text != keyHint)
        {
            slotQuickKeyText.text = keyHint;
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Update Border Colour For Hover
        if (isSlotHovered == true) 
        {
            slotDrawBorderObj.color = Color.white;
        }
        if (isSlotHovered == false) 
        {
            slotDrawBorderObj.color = Color.gray;
        }

        // Draw the Icon in the Slot
        DrawSlotIcon();

        UpdateQuickSlotKeyHint();

        // Show a count only while the slot contains an item.
        if (slotItemCountText != null)
        {
            slotItemCountText.text = slotItemObj == null ? string.Empty : slotItemObj.itemCount.ToString();
        }
        
    }
}
