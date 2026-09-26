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

        if (slotItemObj == null || heldItem == null || slotItemObj == heldItem ||
            slotItemObj.itemType != heldItem.itemType || slotItemObj.itemName != heldItem.itemName)
        {
            return false;
        }

        int availableStackSpace = slotItemObj.itemCountMax - slotItemObj.itemCount;
        if (availableStackSpace <= 0 || heldItem.itemCount <= 0)
        {
            return false;
        }

        int transferredCount = Mathf.Min(availableStackSpace, heldItem.itemCount);
        slotItemObj.itemCount += transferredCount;
        heldItem.itemCount -= transferredCount;

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



    // Start is called before the first frame update
    void Start()
    {
        
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

        // Show a count only while the slot contains an item.
        if (slotItemCountText != null)
        {
            slotItemCountText.text = slotItemObj == null ? string.Empty : slotItemObj.itemCount.ToString();
        }
        
    }
}
