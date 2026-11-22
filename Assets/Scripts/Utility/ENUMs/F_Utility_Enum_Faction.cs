/// <summary>Which side a character fights for. Each value gets its own entry in F_Utility_Config_Factions.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumFaction
{
    Player,
    Nature,
    Undead,
    Bandits,
    Military,
    Robot,
    Civilian
}

/// <summary>How one faction treats another.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumFactionRelationship
{
    // Never targeted and can't be hurt by melee attacks
    Friendly,
    // Targeted on sight
    Hostile,
    // Ignored: neither targeted nor protected
    Neutral
}
