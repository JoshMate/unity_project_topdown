using System;
using System.Collections.Generic;
using UnityEngine;

public class F_Logic_Controls : MonoBehaviour
{
    [Header("Constants Private")]
    private const KeyCode defaultInventoryToggleKey = KeyCode.Tab;
    private const KeyCode defaultSprintKey = KeyCode.LeftShift;
    private const KeyCode defaultMoveUpKey = KeyCode.W;
    private const KeyCode defaultMoveDownKey = KeyCode.S;
    private const KeyCode defaultMoveLeftKey = KeyCode.A;
    private const KeyCode defaultMoveRightKey = KeyCode.D;
    private const KeyCode defaultControlModifierKey = KeyCode.LeftControl;
    private const KeyCode defaultShiftModifierKey = KeyCode.LeftShift;
    private const KeyCode defaultAltModifierKey = KeyCode.LeftAlt;
    private const KeyCode defaultSneakKey = KeyCode.C;
    private const KeyCode defaultDodgeKey = KeyCode.LeftControl;
    private const KeyCode defaultInteractKey = KeyCode.E;
    private const KeyCode defaultReloadKey = KeyCode.R;
    private const int oneBasedIndexOffset = 1;
    private const int defaultPrimaryActionMouseButton = 0;
    private const int defaultSecondaryActionMouseButton = 1;
    private const int defaultTertiaryActionMouseButton = 2;
    private const string alphaKeyCodePrefix = "Alpha";
    private const string keypadKeyCodePrefix = "Keypad";
    private const string keypadDisplayPrefix = "Num";

    [Header("Movement Bindings")]
    public KeyCode moveUpKey = defaultMoveUpKey;
    public KeyCode moveDownKey = defaultMoveDownKey;
    public KeyCode moveLeftKey = defaultMoveLeftKey;
    public KeyCode moveRightKey = defaultMoveRightKey;
    public KeyCode sprintKey = defaultSprintKey;
    public KeyCode dodgeKey = defaultDodgeKey;
    public KeyCode sneakKey = defaultSneakKey;

    [Header("Usage Bindings")]
    public KeyCode interactKey = defaultInteractKey;
    public KeyCode reloadKey = defaultReloadKey;

    [Header("Interface Bindings")]
    public KeyCode inventoryToggleKey = defaultInventoryToggleKey;

    [Header("Mouse Bindings")]
    public int primaryActionMouseButton = defaultPrimaryActionMouseButton;
    public int secondaryActionMouseButton = defaultSecondaryActionMouseButton;
    public int tertiaryActionMouseButton = defaultTertiaryActionMouseButton;

    [Header("Button Modifiers")]
    public KeyCode buttonModifierControl = defaultControlModifierKey;
    public KeyCode buttonModifierShift = defaultShiftModifierKey;
    public KeyCode buttonModifierAlt = defaultAltModifierKey;

    [Header("Privates")]
    [SerializeField] private List<KeyCode> quickSlotKeys = new List<KeyCode>
    {
        KeyCode.Q, KeyCode.T, KeyCode.F, KeyCode.G, KeyCode.V
    };
    [SerializeField] private List<KeyCode> weaponSlotKeys = new List<KeyCode>
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4
    };

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

        int keyIndex = oneBasedSlotIndex - oneBasedIndexOffset;
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
        if (keyName.StartsWith(alphaKeyCodePrefix, StringComparison.Ordinal))
        {
            return keyName.Substring(alphaKeyCodePrefix.Length);
        }

        if (keyName.StartsWith(keypadKeyCodePrefix, StringComparison.Ordinal))
        {
            return keypadDisplayPrefix + keyName.Substring(keypadKeyCodePrefix.Length);
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
