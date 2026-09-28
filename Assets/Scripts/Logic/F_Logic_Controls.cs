using System;
using System.Collections.Generic;
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
    private const int OneBasedIndexOffset = 1;
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
    [SerializeField] private List<KeyCode> quickSlotKeys = new List<KeyCode>
    {
        KeyCode.Q, KeyCode.T, KeyCode.F, KeyCode.G, KeyCode.V
    };
    [SerializeField] private List<KeyCode> weaponSlotKeys = new List<KeyCode>
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4
    };

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

    /// <summary>Returns whether the configured key for an indexed slot was pressed this frame.</summary>
    /// <param name="slotGroup">The slot collection whose binding should be checked.</param>
    /// <param name="oneBasedSlotIndex">The one-based index within that collection.</param>
    public bool IsSlotKeyPressed(enumInventorySlotHotkeyGroup slotGroup, int oneBasedSlotIndex)
    {
        KeyCode keyCode = GetSlotKey(slotGroup, oneBasedSlotIndex);
        return keyCode != KeyCode.None && Input.GetKeyDown(keyCode);
    }

    /// <summary>Returns the configured key for an indexed slot as concise display text.</summary>
    /// <param name="slotGroup">The slot collection whose binding should be displayed.</param>
    /// <param name="oneBasedSlotIndex">The one-based index within that collection.</param>
    public string GetSlotKeyDisplayName(enumInventorySlotHotkeyGroup slotGroup, int oneBasedSlotIndex)
    {
        return FormatKeyCodeDisplayName(GetSlotKey(slotGroup, oneBasedSlotIndex));
    }

    private KeyCode GetSlotKey(enumInventorySlotHotkeyGroup slotGroup, int oneBasedSlotIndex)
    {
        List<KeyCode> keys = slotGroup == enumInventorySlotHotkeyGroup.QuickSlot
            ? quickSlotKeys
            : slotGroup == enumInventorySlotHotkeyGroup.WeaponSlot
                ? weaponSlotKeys
                : null;

        int keyIndex = oneBasedSlotIndex - OneBasedIndexOffset;
        if (keys == null || keyIndex < 0 || keyIndex >= keys.Count)
        {
            return KeyCode.None;
        }

        return keys[keyIndex];
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
