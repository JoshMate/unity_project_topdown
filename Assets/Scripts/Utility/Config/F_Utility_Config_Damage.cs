using TMPro;
using UnityEngine;

/// <summary>
/// Global configuration for floating damage numbers and ent health bars.
/// Distances are in world units and times are in seconds.
/// </summary>
public static class F_Utility_Config_Damage
{
    // ---------------------------------------------------------------------------------------------
    // Damage numbers: text
    // ---------------------------------------------------------------------------------------------

    // Base font size of a damage number before size scaling is applied
    public const float cfgDamageNumberFontSize = 2f;
    // Font style used for the number text
    public const FontStyles cfgDamageNumberFontStyle = FontStyles.Bold;
    // .NET numeric format applied to the amount ("0" shows whole numbers)
    public const string cfgDamageNumberFormat = "0";
    // Text placed in front of healing amounts
    public const string cfgDamageNumberHealPrefix = "+";
    // Text colour for damage and for healing
    public static readonly Color32 cfgDamageNumberColourDamage = F_Utility_Config_Colours.cfgColourDamageText;
    public static readonly Color32 cfgDamageNumberColourHeal = F_Utility_Config_Colours.cfgColourDamageHealText;

    // ---------------------------------------------------------------------------------------------
    // Damage numbers: icon
    // ---------------------------------------------------------------------------------------------

    // Height of the damage type icon before size scaling is applied
    public const float cfgDamageNumberIconHeight = 0.2f;
    // Space between the icon and the text
    public const float cfgDamageNumberIconGap = 0.05f;

    // ---------------------------------------------------------------------------------------------
    // Damage numbers: size scaling (text and icon grow together as the amount increases)
    // ---------------------------------------------------------------------------------------------

    // Size multiplier for the smallest (lowest amount) and largest (highest amount) numbers
    public const float cfgDamageNumberScaleMin = 1f;
    public const float cfgDamageNumberScaleMax = 3.0f;
    // Amount at or above which a number reaches its maximum size
    public const float cfgDamageNumberScaleFullDamage = 300f;

    // ---------------------------------------------------------------------------------------------
    // Damage numbers: movement and lifetime
    // ---------------------------------------------------------------------------------------------

    // Constant upward speed, in world units per second
    public const float cfgDamageNumberMoveSpeed = 1f;
    // How long a number lives before it starts to fade
    public const float cfgDamageNumberLifetime = 0.75f;
    // How long a number takes to fade out completely
    public const float cfgDamageNumberFadeDuration = 0.25f;

    // ---------------------------------------------------------------------------------------------
    // Damage numbers: pooling (merging matching numbers into one running total)
    // Numbers only merge when they share damage type, source and target ent.
    // ---------------------------------------------------------------------------------------------

    // Master switch: when false numbers never merge
    public static readonly bool cfgDamageNumberPoolingEnabled = false;
    // Age at which a number tries to merge into an existing number
    public const float cfgDamageNumberPoolDelay = 0.5f;
    // After receiving a merge, how long a pooled number lives before fading (restarts on every merge)
    public const float cfgDamageNumberPooledLifetime = 1f;
    // How long a pooled number takes to fade out completely
    public const float cfgDamageNumberPooledFadeDuration = 0.5f;

    // ---------------------------------------------------------------------------------------------
    // Damage numbers: overlap avoidance (new numbers choose a free start position)
    // ---------------------------------------------------------------------------------------------

    // Extra space kept between numbers
    public const float cfgDamageNumberOverlapPadding = 0.00f;
    // Side-by-side positions tried per row (centre, then alternating right and left)
    public const int cfgDamageNumberOverlapSlotsPerRow = 5;
    // Rows tried upwards when a row has no free position
    public const int cfgDamageNumberOverlapMaxRows = 4;

    // ---------------------------------------------------------------------------------------------
    // Draw order
    // ---------------------------------------------------------------------------------------------

    // Sorting order added on top of the ent's highest sprite for damage numbers and health bars
    public const int cfgDamageNumberSortingOrderOffset = 100;
    public const int cfgHealthBarSortingOrderOffset = 90;

    // ---------------------------------------------------------------------------------------------
    // Health bar (drawn at the centre of the ent's sprite while health is below 100%)
    // ---------------------------------------------------------------------------------------------

    // Size of the bar at full health
    public const float cfgHealthBarWidth = 0.6f;
    public const float cfgHealthBarHeight = 0.05f;
    // Bar colour per health band
    public static readonly Color32 cfgHealthBarColourHigh = F_Utility_Config_Colours.cfgDurabilityBarGood;
    public static readonly Color32 cfgHealthBarColourMedium = F_Utility_Config_Colours.cfgDurabilityBarYellow;
    public static readonly Color32 cfgHealthBarColourLow = F_Utility_Config_Colours.cfgDurabilityBarOrange;
    public static readonly Color32 cfgHealthBarColourCritical = F_Utility_Config_Colours.cfgDurabilityBarBad;
    // Black background drawn behind the bar at full width, one sorting step below the fill
    public static readonly Color32 cfgHealthBarColourBackground = F_Utility_Config_Colours.cfgDurabilityBarLost;
    public const int cfgHealthBarBackgroundSortingOrderOffset = 89;
    // Health fraction at or above which each colour applies (below Low uses Critical)
    public const float cfgHealthBarThresholdHigh = 0.75f;
    public const float cfgHealthBarThresholdMedium = 0.5f;
    public const float cfgHealthBarThresholdLow = 0.25f;
}
