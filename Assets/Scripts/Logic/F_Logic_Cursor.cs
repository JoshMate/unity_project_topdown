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

    [Header("Art")]
    public Sprite cursorSpritePointer;
    public Sprite cursorSpriteAim;

    [Header("Stats")]
    private float cursorPlaceMaxDistance = 2.5f;

    [Header("Privates")]
    private Vector2 mousePosition;
    private GameObject cursorHoveredObject;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide OS hardware cursor
        Cursor.visible = false;

        // Hide Cursor Text on start
        cursorText.text = "";
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
                        cursorHeldItemObj = hoveredItem;
                        cursorHeldItemObj.MoveItemToInventory();
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

        if (F_Utility_Helper_Inventory.AddItemToInventory(hoveredItem, playerController.playerInventory))
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
            cursorRendererPointer.sprite = cursorSpriteAim;
        }

    }

    void CursorDropHeldItemWhenMenuClosed()
    {
        if (characterScreenManager.isMenuOpen == false)
        {
            CursorDropItemAtLocation();
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

        // Apply the drop
        cursorHeldItemObj.MoveItemOutOfInventory();
        cursorHeldItemObj.gameObject.transform.position = finalDropPosition;
        cursorHeldItemObj = null;
    }

    void CursorItemDisplay()
    {
        if (cursorHeldItemObj != null)
        {
            cursorRendererItem.sprite = cursorHeldItemObj.itemSpriteRenderer.sprite;
        }
        if (cursorHeldItemObj == null)
        {
            cursorRendererItem.sprite = null;
        }
    }
    
}
