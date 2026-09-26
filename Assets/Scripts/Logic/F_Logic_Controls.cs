using System;
using UnityEngine;

public class F_Logic_Controls : MonoBehaviour
{
    private const KeyCode DefaultInventoryToggleKey = KeyCode.Tab;
    private const KeyCode DefaultSprintKey = KeyCode.LeftShift;
    private const KeyCode DefaultMoveUpKey = KeyCode.W;
    private const KeyCode DefaultMoveDownKey = KeyCode.S;
    private const KeyCode DefaultMoveLeftKey = KeyCode.A;
    private const KeyCode DefaultMoveRightKey = KeyCode.D;
    private const KeyCode DefaultControlModifierKey = KeyCode.LeftControl;
    private const KeyCode DefaultShiftModifierKey = KeyCode.LeftShift;
    private const KeyCode DefaultAltModifierKey = KeyCode.LeftAlt;
    private const KeyCode DefaultSneakKey = KeyCode.C;
    private const KeyCode DefaultDodgeKey = KeyCode.LeftControl;
    private const KeyCode DefaultInteractKey = KeyCode.E;
    private const KeyCode DefaultReloadKey = KeyCode.R;
    private const KeyCode DefaultQuickSlot1Key = KeyCode.Q;
    private const KeyCode DefaultQuickSlot2Key = KeyCode.T;
    private const KeyCode DefaultQuickSlot3Key = KeyCode.F;
    private const KeyCode DefaultQuickSlot4Key = KeyCode.G;
    private const KeyCode DefaultQuickSlot5Key = KeyCode.V;
    private const int DefaultPrimaryActionMouseButton = 0;
    private const int DefaultSecondaryActionMouseButton = 1;
    private const int DefaultTertiaryActionMouseButton = 2;
    private const string AlphaKeyCodePrefix = "Alpha";
    private const string KeypadKeyCodePrefix = "Keypad";
    private const string KeypadDisplayPrefix = "Num";

    [Header("Movement Bindings")]
    public KeyCode moveUpKey = DefaultMoveUpKey;
    public KeyCode moveDownKey = DefaultMoveDownKey;
    public KeyCode moveLeftKey = DefaultMoveLeftKey;
    public KeyCode moveRightKey = DefaultMoveRightKey;
    public KeyCode sprintKey = DefaultSprintKey;
    public KeyCode dodgeKey = DefaultDodgeKey;
     public KeyCode sneakKey = DefaultSneakKey;
    
    [Header("Usage Bindings")]
    public KeyCode interactKey = DefaultInteractKey;
    public KeyCode reloadKey = DefaultReloadKey;
    public KeyCode quickSlot1Key = DefaultQuickSlot1Key;
    public KeyCode quickSlot2Key = DefaultQuickSlot2Key;
    public KeyCode quickSlot3Key = DefaultQuickSlot3Key;
    public KeyCode quickSlot4Key = DefaultQuickSlot4Key;
    public KeyCode quickSlot5Key = DefaultQuickSlot5Key;

    [Header("Interface Bindings")]
    public KeyCode inventoryToggleKey = DefaultInventoryToggleKey;

    [Header("Mouse Bindings")]
    public int primaryActionMouseButton = DefaultPrimaryActionMouseButton;
    public int secondaryActionMouseButton = DefaultSecondaryActionMouseButton;
    public int tertiaryActionMouseButton = DefaultTertiaryActionMouseButton;

    [Header("Button Modifiers")]
    public KeyCode buttonModifierControl = DefaultControlModifierKey;
    public KeyCode buttonModifierShift = DefaultShiftModifierKey;
    public KeyCode buttonModifierAlt = DefaultAltModifierKey;

    /// <summary>Returns whether the inventory toggle key was pressed this frame.</summary>
    public bool IsInventoryTogglePressed()
    {
        return Input.GetKeyDown(inventoryToggleKey);
    }

    /// <summary>Returns whether the sprint key was pressed this frame.</summary>
    public bool IsSprintPressed()
    {
        return Input.GetKeyDown(sprintKey);
    }

    /// <summary>Returns whether the sprint key was released this frame.</summary>
    public bool IsSprintReleased()
    {
        return Input.GetKeyUp(sprintKey);
    }

    /// <summary>Returns normalized movement input from the configured movement keys.</summary>
    public Vector2 GetMovementInput()
    {
        float horizontal = Input.GetKey(moveRightKey) ? 1f : 0f;
        horizontal -= Input.GetKey(moveLeftKey) ? 1f : 0f;

        float vertical = Input.GetKey(moveUpKey) ? 1f : 0f;
        vertical -= Input.GetKey(moveDownKey) ? 1f : 0f;

        return new Vector2(horizontal, vertical).normalized;
    }

    /// <summary>Returns the current screen-space mouse position.</summary>
    public Vector3 GetMouseScreenPosition()
    {
        return Input.mousePosition;
    }

    /// <summary>Returns whether the configured sneak key is currently held.</summary>
    public bool IsSneakHeld()
    {
        return Input.GetKey(sneakKey);
    }

    /// <summary>Returns whether the configured dodge key was pressed this frame.</summary>
    public bool IsDodgePressed()
    {
        return Input.GetKeyDown(dodgeKey);
    }

    /// <summary>Returns whether the configured interact key was pressed this frame.</summary>
    public bool IsInteractPressed()
    {
        return Input.GetKeyDown(interactKey);
    }

    /// <summary>Returns whether the configured reload key was pressed this frame.</summary>
    public bool IsReloadPressed()
    {
        return Input.GetKeyDown(reloadKey);
    }

    /// <summary>Returns whether quick-slot one was selected this frame.</summary>
    public bool IsQuickSlot1Pressed()
    {
        return Input.GetKeyDown(quickSlot1Key);
    }

    /// <summary>Returns whether quick-slot two was selected this frame.</summary>
    public bool IsQuickSlot2Pressed()
    {
        return Input.GetKeyDown(quickSlot2Key);
    }

    /// <summary>Returns whether quick-slot three was selected this frame.</summary>
    public bool IsQuickSlot3Pressed()
    {
        return Input.GetKeyDown(quickSlot3Key);
    }

    /// <summary>Returns whether quick-slot four was selected this frame.</summary>
    public bool IsQuickSlot4Pressed()
    {
        return Input.GetKeyDown(quickSlot4Key);
    }

    /// <summary>Returns whether quick-slot five was selected this frame.</summary>
    public bool IsQuickSlot5Pressed()
    {
        return Input.GetKeyDown(quickSlot5Key);
    }

    /// <summary>Returns the configured quick-slot binding as concise display text.</summary>
    /// <param name="oneBasedQuickSlotIndex">The quick-slot number, from 1 through 5.</param>
    public string GetQuickSlotKeyDisplayName(int oneBasedQuickSlotIndex)
    {
        KeyCode keyCode;
        switch (oneBasedQuickSlotIndex)
        {
            case 1:
                keyCode = quickSlot1Key;
                break;
            case 2:
                keyCode = quickSlot2Key;
                break;
            case 3:
                keyCode = quickSlot3Key;
                break;
            case 4:
                keyCode = quickSlot4Key;
                break;
            case 5:
                keyCode = quickSlot5Key;
                break;
            default:
                return string.Empty;
        }

        return FormatKeyCodeDisplayName(keyCode);
    }

    private static string FormatKeyCodeDisplayName(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.None:
                return string.Empty;
            case KeyCode.LeftControl:
            case KeyCode.RightControl:
                return "Ctrl";
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
                return "Shift";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
                return "Alt";
        }

        string keyName = keyCode.ToString();
        if (keyName.StartsWith(AlphaKeyCodePrefix, StringComparison.Ordinal))
        {
            return keyName.Substring(AlphaKeyCodePrefix.Length);
        }

        if (keyName.StartsWith(KeypadKeyCodePrefix, StringComparison.Ordinal))
        {
            return KeypadDisplayPrefix + keyName.Substring(KeypadKeyCodePrefix.Length);
        }

        return keyName;
    }


    /// <summary>Returns whether the configured primary action mouse button was pressed this frame.</summary>
    public bool IsPrimaryActionPressed()
    {
        return Input.GetMouseButtonDown(primaryActionMouseButton);
    }

    /// <summary>Returns whether the configured secondary action mouse button was pressed this frame.</summary>
    public bool IsSecondaryActionPressed()
    {
        return Input.GetMouseButtonDown(secondaryActionMouseButton);
    }

    /// <summary>Returns whether the configured tertiary action mouse button was pressed this frame.</summary>
    public bool IsTertiaryActionPressed()
    {
        return Input.GetMouseButtonDown(tertiaryActionMouseButton);
    }

    /// <summary>Returns whether the control modifier is currently held.</summary>
    public bool IsControlModifierHeld()
    {
        return Input.GetKey(buttonModifierControl);
    }

    /// <summary>Returns whether the shift modifier is currently held.</summary>
    public bool IsShiftModifierHeld()
    {
        return Input.GetKey(buttonModifierShift);
    }

    /// <summary>Returns whether the alt modifier is currently held.</summary>
    public bool IsAltModifierHeld()
    {
        return Input.GetKey(buttonModifierAlt);
    }
}
