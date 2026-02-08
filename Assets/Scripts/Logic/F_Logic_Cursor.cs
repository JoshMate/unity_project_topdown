using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public float cursorPlaceMaxDistance = 128;

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
                    cursorHeldItemObj.MoveItemOutOfInventory();
                    cursorHeldItemObj.gameObject.transform.position = new Vector2(mousePosition.x,mousePosition.y);
                    cursorHeldItemObj = null;
                }

                if (cursorHoveredObject != null)
                {
                    // Pick Item up off the floor (If no inventory slot found)
                    if (cursorHoveredObject.GetComponent<F_Item>() != null)
                    {
                        cursorHeldItemObj = cursorHoveredObject.GetComponent<F_Item>();
                        cursorHeldItemObj.MoveItemToInventory();
                        Debug.Log("Floor Item Clicked");
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
