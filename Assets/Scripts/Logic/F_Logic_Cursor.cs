using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class F_Logic_Cursor : MonoBehaviour
{
    [Header("Object Refs")]
    public F_Item cursorHeldItemObj;
    public F_PlayerController playerController;
    public F_GUI_CharacterScreen_Manager characterScreenManager;
    public Camera playerCamera;
    public F_Logic_Controls controls;
    public SpriteRenderer cursorRendererPointer;
    public SpriteRenderer cursorRendererItem;
    public Transform cursorTransformPointer;
    public Transform cursorTransformItem;
    public TMP_Text  cursorText;
    public F_GUI_ItemTooltip itemTooltip;
    public F_GUI_ItemContextMenu itemContextMenu;
    public F_GUI_Inventory_Slot cursorModifierPickupSlot;

    [Header("Art")]
    public Sprite cursorSpritePointer;
    public Sprite cursorSpriteAim;
    public Sprite cursorSpriteReload;
    public Sprite cursorSpriteOkay;
    public Sprite cursorSpriteDeploy;

    [Header("Stats")]
    private float cursorPlaceMaxDistance = 2.5f;

    [Header("Constants Private")]
    private const float cfgCursorHeldCountScale = 1f;
    private const float cfgCursorHeldCountFontSize = 2f;

    [Header("Privates")]
    private readonly Vector2 cursorHeldItemPadding = new Vector2(0.02f, -0.02f);
    private readonly Vector2 cursorHeldCountPadding = new Vector2(-0.02f, 0.02f);
    private readonly Vector2 cursorHeldCountBoxSize = new Vector2(1f, 1f);
    private TextMeshPro cursorHeldCountText;
    private Vector2 mousePosition;
    private GameObject cursorHoveredObject;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide OS hardware cursor
        Cursor.visible = false;

        // Hide Cursor Text on start
        cursorText.text = "";

        CreateWeaponCrosshair();
    }

    // Builds the weapon spread crosshair as a child of the pointer so it follows the cursor
    private void CreateWeaponCrosshair()
    {
        GameObject crosshairObject = new GameObject("Cursor_WeaponCrosshair");
        crosshairObject.transform.SetParent(cursorTransformPointer, false);

        F_Logic_CursorCrosshair crosshair = crosshairObject.AddComponent<F_Logic_CursorCrosshair>();
        crosshair.playerHeldWeapon = playerController != null ? playerController.playerHeldWeapon : null;
        crosshair.characterScreenManager = characterScreenManager;
        crosshair.sortingReferenceRenderer = cursorRendererPointer;
    }

    // Update is called once per frame
    void Update()
    {
        CursorTypeSelection();
        CursorItemDisplay();
        processInputs();
        RayCastCursorToGetHoveredElement();
        ProcessInteractInput();
        CursorDropHeldItemWhenMenuClosed();
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        // Store the Game Object of what ever the mouse is hovering over now
        cursorHoveredObject = otherCollider.gameObject;
    }

    void RayCastCursorToGetHoveredElement()
    {
        F_Item hoveredItem = null;
        Vector2 pointerScreenPosition = controls.GetMouseScreenPosition();
        itemContextMenu?.CloseIfPointerMovedAway(pointerScreenPosition);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = pointerScreenPosition
            };

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int resultIndex = 0; resultIndex < raycastResults.Count; resultIndex++)
            {
                F_GUI_Inventory_Slot hoveredSlot = raycastResults[resultIndex].gameObject.GetComponentInParent<F_GUI_Inventory_Slot>();
                if (hoveredSlot == null)
                {
                    continue;
                }

                hoveredItem = hoveredSlot.slotItemObj;
                break;
            }

            cursorHoveredObject = null;
            cursorText.text = string.Empty;
            UpdateItemTooltip(hoveredItem, pointerScreenPosition);
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
        if (hit.collider != null)
        {
            hoveredItem = hit.collider.GetComponentInParent<F_Item>();
            cursorHoveredObject = hoveredItem != null ? hoveredItem.gameObject : hit.collider.gameObject;
        }
        else
        {
            cursorHoveredObject = null;
        }

        if (characterScreenManager.isMenuOpen)
        {
            cursorText.text = string.Empty;
        }
        else if (hoveredItem != null)
        {
            cursorText.transform.position = mousePosition + new Vector2(0.5f, -1.2f);
            cursorText.text = hoveredItem.itemName;
        }
        else
        {
            cursorText.text = string.Empty;
        }

        UpdateItemTooltip(hoveredItem, pointerScreenPosition);
    }

    private void UpdateItemTooltip(F_Item hoveredItem, Vector2 pointerScreenPosition)
    {
        if (itemContextMenu != null && itemContextMenu.IsOpen)
        {
            itemTooltip.Hide();
            return;
        }

        if (characterScreenManager.isMenuOpen && hoveredItem != null)
        {
            itemTooltip.Show(hoveredItem, pointerScreenPosition);
        }
        else
        {
            itemTooltip.Hide();
        }
    }

    void processInputs()
    {
        // Handle Cursor Pos
        mousePosition = playerCamera.ScreenToWorldPoint(controls.GetMouseScreenPosition());

        cursorTransformPointer.position = mousePosition;

        // Left Click Controls
        if (controls.IsPrimaryActionPressed() && characterScreenManager.isMenuOpen)
        {
            // Drop held cursor item on the floor at mouse position when no GUI element is in the way.
            if (cursorHeldItemObj != null && !EventSystem.current.IsPointerOverGameObject())
            {
                CursorDropItemAtLocation();
            }

            if (cursorHoveredObject != null)
            {
                F_Item hoveredItem = cursorHoveredObject.GetComponent<F_Item>();
                if (hoveredItem != null)
                {
                    float distanceToPlayer = Vector2.Distance(hoveredItem.transform.position, playerController.transform.position);
                    if (distanceToPlayer <= cursorPlaceMaxDistance)
                    {
                        F_Utility_Helper_Inventory.MoveItemToInventory(hoveredItem, this);
                    }
                }
            }
        }
    }

    private void ProcessInteractInput()
    {
        if (!characterScreenManager.isMenuOpen || !controls.IsInteractPressed() || cursorHoveredObject == null)
        {
            return;
        }

        F_Item hoveredItem = cursorHoveredObject.GetComponent<F_Item>();
        if (hoveredItem == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(hoveredItem.transform.position, playerController.transform.position);
        if (distanceToPlayer > cursorPlaceMaxDistance)
        {
            return;
        }

        if (F_Utility_Helper_Inventory.MoveItemToInventory(hoveredItem, playerController.playerInventory))
        {
            cursorHoveredObject = null;
        }
    }


    void CursorTypeSelection()
    {
        if (characterScreenManager.isMenuOpen == true)
        {
            cursorRendererPointer.sprite = cursorSpritePointer;
        }
        if (characterScreenManager.isMenuOpen == false)
        {
            F_Item_Weapon heldWeapon = playerController != null && playerController.playerHeldWeapon != null
                ? playerController.playerHeldWeapon.SelectedWeaponItem
                : null;
            bool isReloading = heldWeapon != null && heldWeapon.IsReloading && cursorSpriteReload != null;
            bool isDeploying = heldWeapon != null && heldWeapon.IsDeploying && cursorSpriteDeploy != null;
            bool isFinishingReload = heldWeapon != null && heldWeapon.IsFinishingReload && cursorSpriteOkay != null;
            if (isDeploying)
            {
                cursorRendererPointer.sprite = cursorSpriteDeploy;
            }
            else if (isFinishingReload)
            {
                cursorRendererPointer.sprite = cursorSpriteOkay;
            }
            else
            {
                cursorRendererPointer.sprite = isReloading ? cursorSpriteReload : cursorSpriteAim;
            }
        }

    }

    void CursorDropHeldItemWhenMenuClosed()
    {
        if (characterScreenManager.isMenuOpen == false)
        {
            CursorDropItemAtLocation();
            itemContextMenu?.Hide();
        }
    }

    void CursorDropItemAtLocation()
    {
        if (cursorHeldItemObj == null) return;

        Vector2 playerPos = playerController.transform.position;
        Vector2 cursorPos = new Vector2(mousePosition.x, mousePosition.y);
        
        Vector2 finalDropPosition;

        // Check if the item is within pickup range
        if (Vector2.Distance(cursorPos, playerPos) <= cursorPlaceMaxDistance)
        {
            finalDropPosition = cursorPos;
        }
        else
        {
            // Calculate the direction from the player to the mouse
            Vector2 directionToCursor = (cursorPos - playerPos).normalized;
            
            // Set the position to the maximum distance in that direction
            finalDropPosition = playerPos + (directionToCursor * cursorPlaceMaxDistance);
        }

        // Apply the drop through the inventory transaction helper.
        F_Utility_Helper_Inventory.DropItemFromInventory(
            cursorHeldItemObj,
            playerController != null ? playerController.playerInventory : null,
            finalDropPosition);
        cursorHeldItemObj = null;
    }

    void CursorItemDisplay()
    {
        if (cursorHeldItemObj != null)
        {
            cursorRendererItem.sprite = cursorHeldItemObj.itemSprite;
        }
        if (cursorHeldItemObj == null)
        {
            cursorRendererItem.sprite = null;
        }

        PositionHeldItemVisual();
        UpdateHeldItemCountText();
    }

    // Places the held item icon just below and right of the pointer so the two never overlap
    private void PositionHeldItemVisual()
    {
        Sprite itemSprite = cursorRendererItem.sprite;
        if (itemSprite == null || cursorRendererPointer.sprite == null)
        {
            return;
        }

        Bounds pointerBounds = cursorRendererPointer.bounds;
        Vector2 iconTopLeft = new Vector2(pointerBounds.max.x, pointerBounds.min.y) + cursorHeldItemPadding;
        Vector2 iconHalfSize = GetHeldIconHalfSize(itemSprite);
        Vector2 iconCentre = new Vector2(iconTopLeft.x + iconHalfSize.x, iconTopLeft.y - iconHalfSize.y);
        Vector2 iconCentreOffset = GetHeldIconCentreOffset(itemSprite);
        cursorTransformItem.position = new Vector3(
            iconCentre.x - iconCentreOffset.x,
            iconCentre.y - iconCentreOffset.y,
            cursorTransformItem.position.z);
    }

    private Vector2 GetHeldIconHalfSize(Sprite itemSprite)
    {
        Vector3 iconScale = cursorTransformItem.lossyScale;
        return new Vector2(itemSprite.bounds.extents.x * Mathf.Abs(iconScale.x), itemSprite.bounds.extents.y * Mathf.Abs(iconScale.y));
    }

    private Vector2 GetHeldIconCentreOffset(Sprite itemSprite)
    {
        Vector3 iconScale = cursorTransformItem.lossyScale;
        return new Vector2(itemSprite.bounds.center.x * iconScale.x, itemSprite.bounds.center.y * iconScale.y);
    }

    // Builds the small stack counter as a child of the held item visual so it follows the cursor
    private void CreateHeldItemCountText()
    {
        GameObject countObject = new GameObject("Cursor_HeldItemCount");
        countObject.transform.SetParent(cursorTransformItem, false);
        countObject.transform.localScale = Vector3.one * cfgCursorHeldCountScale;

        cursorHeldCountText = countObject.AddComponent<TextMeshPro>();
        cursorHeldCountText.alignment = TextAlignmentOptions.BottomRight;
        cursorHeldCountText.fontSize = cfgCursorHeldCountFontSize;
        cursorHeldCountText.color = F_Utility_Config_Colours.cfgColourGuiText;
        cursorHeldCountText.raycastTarget = false;
        cursorHeldCountText.rectTransform.pivot = new Vector2(1f, 0f);
        cursorHeldCountText.rectTransform.sizeDelta = cursorHeldCountBoxSize;
        if (cursorText != null)
        {
            cursorHeldCountText.font = cursorText.font;
            cursorHeldCountText.fontSharedMaterial = cursorText.fontSharedMaterial;
        }

        MeshRenderer countRenderer = cursorHeldCountText.GetComponent<MeshRenderer>();
        countRenderer.sortingLayerID = cursorRendererItem.sortingLayerID;
        countRenderer.sortingOrder = cursorRendererItem.sortingOrder + 1;
        cursorHeldCountText.text = string.Empty;
    }

    // Shows the held stack count inside the icon's bottom-right corner, hiding it for empty or single-item cursors
    private void UpdateHeldItemCountText()
    {
        if (cursorHeldCountText == null)
        {
            CreateHeldItemCountText();
        }

        string countLabel = cursorHeldItemObj != null && cursorHeldItemObj.itemCount > 1
            ? cursorHeldItemObj.itemCount.ToString()
            : string.Empty;
        if (cursorHeldCountText.text != countLabel)
        {
            cursorHeldCountText.text = countLabel;
        }

        Sprite itemSprite = cursorRendererItem.sprite;
        if (itemSprite != null)
        {
            Vector2 iconHalfSize = GetHeldIconHalfSize(itemSprite);
            Vector2 iconCentre = (Vector2)cursorTransformItem.position + GetHeldIconCentreOffset(itemSprite);
            cursorHeldCountText.transform.position = new Vector3(
                iconCentre.x + iconHalfSize.x + cursorHeldCountPadding.x,
                iconCentre.y - iconHalfSize.y + cursorHeldCountPadding.y,
                cursorTransformItem.position.z);
        }
    }
    
}
