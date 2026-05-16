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
    public SpriteRenderer cursorRendererPointer;
    public SpriteRenderer cursorRendererItem;
    public Transform cursorTransformPointer;
    public Transform cursorTransformItem;
    public TMP_Text  cursorText;

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
        CursorDropHeldItemWhenMenuClosed();
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        // Store the Game Object of what ever the mouse is hovering over now
        cursorHoveredObject = otherCollider.gameObject;
    }

    void RayCastCursorToGetHoveredElement()
    {
        // Check if the mouse is over a UI element (Then Don't look for Game Objects to hover over)
        if (EventSystem.current.IsPointerOverGameObject()) {

            // 2. Create fake pointer data at the mouse position
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            // 3. Raycast to find what we hit
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            // 4. Check if we hit any UI elements
            if (raycastResults.Count > 0)
            {
                GameObject hitObject = raycastResults[0].gameObject;

                // 5. Try to find the F_GUI_Inventory_Slot component on the hit object
                F_GUI_Inventory_Slot hoveredSlot = hitObject.GetComponentInParent<F_GUI_Inventory_Slot>();

                // 6. If it's not null, we are successfully hovering over an inventory slot!
                if (hoveredSlot != null)
                {
                    // Optional: Check if the slot actually has an item in it before checking the name
                    if (hoveredSlot.slotItemObj != null)
                    {
                        Vector2 mousePos = Input.mousePosition;
                        cursorText.transform.position = mousePosition + new Vector2(0.5f, -1.2f);
                        cursorHoveredObject = hitObject;
                        cursorText.text = hoveredSlot.slotItemObj.itemName;
                    }
                }
                else
                {
                    cursorHoveredObject = null;
                    cursorText.text = "";
                }
            }
            return; 
        }
        
        // Ray Cast to hit game objects in the world
        // Perform a 2D raycast at the mouse position
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            cursorHoveredObject = hit.collider.gameObject;
            Vector2 mousePos = Input.mousePosition;
            cursorText.transform.position = mousePosition + new Vector2(0.5f, -1.2f);

            if (cursorHoveredObject != null && cursorHoveredObject.GetComponent<F_Item>() != null)
            {
                cursorText.text = cursorHoveredObject.GetComponent<F_Item>().itemName;
            }

            

        }
        else
        {
            cursorHoveredObject = null;
            cursorText.text = "";
        }
        
    }

    void processInputs()
    {
        // Handle Cursor Pos
        mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);

        cursorTransformPointer.position = mousePosition;

        //Left Click Controls
        if (Input.GetMouseButtonDown(0))
        {
            if (characterScreenManager.isMenuOpen == true)
            {

                // Drop Held Cursor Item on the floor at mouse position (Only if no GUI element is in the way)
                if (cursorHeldItemObj != null && !EventSystem.current.IsPointerOverGameObject())
                {
                    CursorDropItemAtLocation();
                }

                if (cursorHoveredObject != null)
                {
                    // Pick Item up off the floor (If no inventory slot found)
                    if (cursorHoveredObject.GetComponent<F_Item>() != null)
                    {
                        // Check if the item is within pikcup range first
                        float distanceBetweenCuroseObjectAndPlayer = Vector2.Distance (cursorHoveredObject.transform.position, playerController.transform.position);
                        if (distanceBetweenCuroseObjectAndPlayer <= cursorPlaceMaxDistance)
                        {
                            cursorHeldItemObj = cursorHoveredObject.GetComponent<F_Item>();
                            cursorHeldItemObj.MoveItemToInventory();
                        }
                        
                    }
                }

            }
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

        // Check if the item is within pikcup range first
        float distanceBetweenCuroseObjectAndPlayer = Vector2.Distance (new Vector2(mousePosition.x,mousePosition.y), playerController.transform.position);
        if (distanceBetweenCuroseObjectAndPlayer <= cursorPlaceMaxDistance)
        {
            cursorHeldItemObj.MoveItemOutOfInventory();
            cursorHeldItemObj.gameObject.transform.position = new Vector2(mousePosition.x,mousePosition.y);
            cursorHeldItemObj = null;
        }
        else
        {
            cursorHeldItemObj.MoveItemOutOfInventory();
            cursorHeldItemObj.gameObject.transform.position = playerController.transform.position;
            cursorHeldItemObj = null;
        }
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
