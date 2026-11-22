public static class F_Utility_Config_Melee
{
    // Constants Public: Held melee weapon placement (holder local space, +X is the aim direction)
    // How far in front of the holder's centre the melee sprite rests while held and not swinging
    public const float cfgMeleeHoldDistance = 0.45f;
    // Rotation added to the melee sprite so its tip points along the aim (item art is drawn with the tip at the top right)
    public const float cfgMeleeSpriteForwardAngleOffset = -45f;
    // Sorting layer the held melee sprite is drawn on, so it sits underneath the holder's body
    public const string cfgMeleeSortingLayerName = "Layer_PlayerUnder";
}
