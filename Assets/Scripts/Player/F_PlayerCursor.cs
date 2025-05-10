using UnityEngine;

public class F_PlayerCursor : MonoBehaviour
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

    [Header("Art")]
    public Sprite cursorSpritePointer;
    public Sprite cursorSpriteAim;

    [Header("Privates")]

    private Vector2 mousePosition;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide OS hardware cursor
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        CursorTypeSelection();
        processInputs();
    }

    void OnTriggerEnter(Collider otherObject)
{
   
}

    void processInputs()
    {
        // Handle Cursor Pos
        mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);

        cursorTransformPointer.position = mousePosition;

        //Left Click Controls
        if (characterScreenManager.isMenuOpen == true)
        {

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
    
}
