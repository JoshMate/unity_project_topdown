using UnityEngine;

public class F_PlayerController : MonoBehaviour
{
    [Header("Constants Private")]
    private const int oneBasedIndexOffset = 1;

    [Header("Object Refs")]
    public Camera playerCamera;
    public F_PlayerStats playerStats;
    public F_GUI_CharacterScreen_Manager characterScreenManager;
    public Rigidbody2D rb;
    public F_PlayerHeldWeapon playerHeldWeapon;
    public F_PlayerInventory playerInventory;
    public F_Logic_Cursor playerCursor;
    public F_Logic_Controls controls;

    [Header("Privates")]
    private Vector2 moveDirection;
    private Vector2 mousePosition;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        playerCamera.GetComponent<F_Logic_Camera>().playerObject = transform;
    }

    private void Update()
    {
        ProcessInputs();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void ProcessInputs()
    {
        if (controls.IsInventoryTogglePressed())
        {
            characterScreenManager.ToggleInventoryScreen();
        }

        moveDirection = controls.GetMovementInput();
        mousePosition = playerCamera.ScreenToWorldPoint(controls.GetMouseScreenPosition());

        if (characterScreenManager.isMenuOpen && playerStats.IsSprinting)
        {
            playerStats.SprintEnd();
        }

        if (controls.IsSprintPressed() && !characterScreenManager.isMenuOpen)
        {
            playerStats.SprintStart();
        }

        if (controls.IsSprintReleased())
        {
            playerStats.SprintEnd();
        }

        if (characterScreenManager.isMenuOpen)
        {
            return;
        }

        if (controls.IsSneakPressed())
        {
            playerStats.SneakToggle();
        }

        ProcessWeaponSlotSelectionInputs();
        bool primaryActionRequested = playerHeldWeapon.IsCurrentWeaponContinuous
            ? controls.IsPrimaryActionHeld()
            : controls.IsPrimaryActionPressed();
        if (primaryActionRequested && playerHeldWeapon.CanUseCurrentWeapon)
        {
            playerHeldWeapon.UseCurrentWeapon();
        }
        else if (primaryActionRequested && controls.IsPrimaryActionPressed())
        {
            playerHeldWeapon.TryPlayDryFireSound();
        }

        if (controls.IsReloadPressed())
        {
            playerHeldWeapon.ReloadCurrentWeapon(playerInventory);
        }
    }

    private void ProcessWeaponSlotSelectionInputs()
    {
        if (playerInventory == null || playerInventory.invSlotWeapons == null)
        {
            return;
        }

        for (int slotIndex = 0; slotIndex < playerInventory.invSlotWeapons.Count; slotIndex++)
        {
            int oneBasedSlotIndex = slotIndex + oneBasedIndexOffset;
            if (!controls.IsSlotKeyPressed(enumInventorySlotHotkeyGroup.WeaponSlot, oneBasedSlotIndex))
            {
                continue;
            }

            F_GUI_Inventory_Slot weaponSlot = playerInventory.GetWeaponSlot(oneBasedSlotIndex);
            if (weaponSlot != null)
            {
                playerHeldWeapon.SelectWeaponSlot(weaponSlot);
            }
        }
    }

    private void Move()
    {
        Vector2 aimDirection = mousePosition - rb.position;
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        rb.rotation = aimAngle;

        float finalSpeed = playerStats.GetCurrentMoveSpeed();
        rb.linearVelocity = new Vector2(moveDirection.x, moveDirection.y) * finalSpeed + playerStats.KnockbackVelocity;
    }
}
