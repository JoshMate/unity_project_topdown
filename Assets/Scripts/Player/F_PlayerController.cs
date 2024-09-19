using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_PlayerController : MonoBehaviour
{

    [Header("Object Refs")]
    public Camera playerCamera;
    public Rigidbody2D rb;
    public F_PlayerHeldWeapon playerHeldWeapon;
    

    [Header("Stats")]
    public float statMovementSpeed = 2;
    
    private Vector2 moveDirection;
    private Vector2 mousePosition;

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
        // Movement Controls
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(moveX, moveY).normalized;

        mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);

        // Weapon Controls
        if (Input.GetMouseButtonDown(0))
        {
            playerHeldWeapon.FireWeapon();
        }
    }

    void Move()
    {
        // Look at cursor location
        Vector2 aimDirection = mousePosition - rb.position;
        float aimAngle = Mathf.Atan2(aimDirection.y,aimDirection.x) * Mathf.Rad2Deg;
        rb.rotation = aimAngle;

        // Movement
        rb.velocity = new Vector2(moveDirection.x, moveDirection.y) * statMovementSpeed;
    }
}
