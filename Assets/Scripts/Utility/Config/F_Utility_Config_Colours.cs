using UnityEngine;

/// <summary>
/// Central colour configuration for GUI, HUD and any reusable or referenced colours.
/// All colours are defined with RGB values (0-255) and convert implicitly to Color.
/// </summary>
public static class F_Utility_Config_Colours
{
    private const byte opaqueAlpha = 255;

    // Constants Public: General
    public static readonly Color32 cfgColourWhite = new Color32(255, 255, 255, opaqueAlpha);
    public static readonly Color32 cfgColourGrey = new Color32(128, 128, 128, opaqueAlpha);

    // Constants Public: GUI panels and menus
    public static readonly Color32 cfgColourGuiPanelBorder = new Color32(240, 240, 240, opaqueAlpha);
    public static readonly Color32 cfgColourGuiPanelFill = new Color32(27, 27, 27, opaqueAlpha);
    public static readonly Color32 cfgColourGuiText = new Color32(240, 240, 240, opaqueAlpha);
    public static readonly Color32 cfgColourGuiSeparator = new Color32(158, 158, 158, opaqueAlpha);
    public static readonly Color32 cfgColourGuiOptionBackground = new Color32(41, 41, 41, opaqueAlpha);
    // Button state tints are multipliers over the option background, so they can exceed 1 (RGB 255)
    public const float cfgColourGuiOptionHighlightMultiplier = 1.6f;
    public const float cfgColourGuiOptionPressedMultiplier = 1.3f;

    // Constants Public: Inventory slots
    public static readonly Color32 cfgColourInventorySlotBorderHovered = cfgColourWhite;
    public static readonly Color32 cfgColourInventorySlotBorderDefault = cfgColourGrey;

    // Constants Public: Cursor
    public static readonly Color32 cfgColourCursorCrosshair = cfgColourWhite;
    public static readonly Color32 cfgColourCursorReloadRing = cfgColourWhite;

    // Constants Public: HUD weapon ammo
    public static readonly Color32 cfgColourHudAmmoEmptyText = new Color32(217, 77, 77, opaqueAlpha);

    // Constants Public: HUD bar fills
    public static readonly Color32 cfgColourHudBarHealth = new Color32(159, 0, 0, opaqueAlpha);
    public static readonly Color32 cfgColourHudBarStamina = new Color32(0, 140, 9, opaqueAlpha);
    public static readonly Color32 cfgColourHudBarHunger = new Color32(183, 160, 0, opaqueAlpha);
    public static readonly Color32 cfgColourHudBarThirst = new Color32(0, 148, 164, opaqueAlpha);
    public static readonly Color32 cfgColourHudBarToxic = new Color32(126, 0, 164, opaqueAlpha);

    // Constants Public: HUD weight bar, per weight class
    public static readonly Color32 cfgColourHudWeightFree = new Color32(51, 128, 255, opaqueAlpha);
    public static readonly Color32 cfgColourHudWeightLight = new Color32(51, 204, 51, opaqueAlpha);
    public static readonly Color32 cfgColourHudWeightMedium = new Color32(255, 235, 51, opaqueAlpha);
    public static readonly Color32 cfgColourHudWeightHeavy = new Color32(255, 140, 0, opaqueAlpha);
    public static readonly Color32 cfgColourHudWeightTooMuch = new Color32(230, 26, 26, opaqueAlpha);

    // Constants Public: Durability bar
    public static readonly Color32 cfgDurabilityBarGood = new Color32(51, 204, 51, opaqueAlpha);
    public static readonly Color32 cfgDurabilityBarYellow = new Color32(255, 235, 51, opaqueAlpha);
    public static readonly Color32 cfgDurabilityBarOrange = new Color32(255, 140, 0, opaqueAlpha);
    public static readonly Color32 cfgDurabilityBarBad = new Color32(230, 26, 26, opaqueAlpha);
    public static readonly Color32 cfgDurabilityBarLost = new Color32(0, 0, 0, opaqueAlpha);
    public static readonly Color32 cfgDurabilityBarBackground = new Color32(41, 41, 41, opaqueAlpha);

    // Constants Public: Damage numbers
    public static readonly Color32 cfgColourDamageText = cfgColourWhite;
    public static readonly Color32 cfgColourDamageHealText = new Color32(51, 204, 51, opaqueAlpha);

    // Constants Public: Ent blood / material hit colours
    public static readonly Color32 cfgColourBloodMetal = new Color32(255, 200, 80, opaqueAlpha);
    public static readonly Color32 cfgColourBloodRock = new Color32(160, 160, 150, opaqueAlpha);
    public static readonly Color32 cfgColourBloodWood = new Color32(139, 100, 60, opaqueAlpha);
    public static readonly Color32 cfgColourBloodMeat = new Color32(159, 0, 0, opaqueAlpha);
    public static readonly Color32 cfgColourBloodWater = new Color32(60, 140, 220, opaqueAlpha);

    // Constants Public: Loading screen
    public static readonly Color32 cfgColourLoadingBarBackground = new Color32(41, 46, 51, opaqueAlpha);
    public static readonly Color32 cfgColourLoadingBarFill = new Color32(217, 140, 38, opaqueAlpha);
}
