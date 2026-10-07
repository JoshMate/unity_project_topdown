/// <summary>
/// Global configuration for the death shatter effect applied to ent sprites.
/// </summary>
public static class F_Utility_Config_Shatter
{
    // Constants Public: Piece generation
    public const int cfgShatterPieceCountMin = 8;
    public const int cfgShatterPieceCountMax = 16;

    // Constants Public: Piece motion (world units per second / degrees per second)
    public const float cfgShatterScatterSpeedMin = 0.5f;
    public const float cfgShatterScatterSpeedMax = 2.5f;
    public const float cfgShatterSpinSpeedMax = 360f;
    public const float cfgShatterDragPerSecond = 3f;

    // Constants Public: Piece lifetime (seconds)
    public const float cfgShatterLifetimeMin = 1.0f;
    public const float cfgShatterLifetimeMax = 2.0f;
    // Fraction of the lifetime, counted from the end, over which pieces fade out and shrink
    public const float cfgShatterFadeFraction = 0.5f;
    public const float cfgShatterShrinkEndScale = 0.3f;
}
