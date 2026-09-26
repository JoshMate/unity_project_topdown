using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_PlayerController : MonoBehaviour
{

    [Header("Object Refs")]
    public Camera playerCamera;
    public F_PlayerStats playerStats;
    public F_GUI_CharacterScreen_Manager characterScreenManager;
    public Rigidbody2D rb;
    public F_PlayerHeldWeapon playerHeldWeapon;
    public F_PlayerInventory playerInventory;
    public F_Logic_Cursor playerCursor;
    
    [Header("Privates")]
    
    private Vector2 moveDirection;
    private Vector2 mousePosition;

    // Persist the player before the initialization scene loads gameplay.
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        playerCamera.GetComponent<F_Logic_Camera>().playerObject = this.transform;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInputs();
    }

    void FixedUpdate()
    {
        Move();
    }

    void ProcessInputs()
    {
        // Menu Controls
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            characterScreenManager.ToggleInventoryScreen();
        }


        // Movement Controls
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(moveX, moveY).normalized;

        mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);

        // Sprint Controls
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            playerStats.SprintStart();
        }
        // Sprint Controls
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            playerStats.SprintEnd();
        }

        //Left Click Controls
        if (characterScreenManager.isMenuOpen == false)
        {
            // Fire Weapon if Menu is Closed
            if (Input.GetMouseButtonDown(0))
            {
                playerHeldWeapon.FireWeapon();
            }
        }
        // The duplicate mouse-click fire handler was removed.
    }

    void Move()
    {
        // Look at cursor location
        Vector2 aimDirection = mousePosition - rb.position;
        float aimAngle = Mathf.Atan2(aimDirection.y,aimDirection.x) * Mathf.Rad2Deg;
        rb.rotation = aimAngle;

        float finalSpeed = playerStats.speedMove;

        // Handle Sprinting
        if (playerStats.isSprinting == true && playerStats.stamina > 0)
        {
            finalSpeed = playerStats.speedSprint;
        }


        // Movement
        rb.linearVelocity = new Vector2(moveDirection.x, moveDirection.y) * finalSpeed;
    }
}
