/// <summary>
/// Global stealth configuration: sneak movement, the sneak screen vignette and the sound rings shown while sneaking.
/// Colours live in F_Utility_Config_Colours.
/// </summary>
public static class F_Utility_Config_Stealth
{
    // Constants Public: Sneak movement
    // Multiplier applied to the current move speed (walking and sprinting) while sneaking
    public const float cfgStealthSneakSpeedMultiplier = 0.5f;

    // Constants Public: Sneak vignette
    // Resolution of the generated vignette texture (pixels per side)
    public const int cfgStealthVignetteTextureSize = 256;
    // Normalised distance from the screen centre (1 = screen edge midpoint) where the darkening begins
    public const float cfgStealthVignetteInnerRadius = 0.55f;
    // Normalised distance from the screen centre where the darkening reaches full strength
    public const float cfgStealthVignetteOuterRadius = 1.35f;
    // Seconds taken to fade the vignette in or out when sneak is toggled
    public const float cfgStealthVignetteFadeDuration = 0.25f;

    // Constants Public: Sound rings
    // Seconds a ring takes to grow from the sound source to its maximum radius
    public const float cfgStealthSoundRingLifetime = 1f;
    // Multiplier converting a sound's hearing radius (world units) into the ring's maximum radius
    public const float cfgStealthSoundRingRadiusScale = 1f;
    // Ring radius used for spatial sounds without a hearing radius, and the smallest radius any ring can reach
    public const float cfgStealthSoundRingRadiusMin = 0.5f;
    // Largest radius any ring can reach, so very loud sounds stay readable on screen
    public const float cfgStealthSoundRingRadiusMax = 20f;
    // Ring line thickness in world units
    public const float cfgStealthSoundRingThickness = 0.05f;
    // Number of segments used to draw the ring
    public const int cfgStealthSoundRingSegmentCount = 64;
    // Fraction of the lifetime after which the ring starts fading out
    public const float cfgStealthSoundRingFadeStartRatio = 0.5f;
    // Exponent of the ease-out growth curve; higher values grow faster at the start
    public const float cfgStealthSoundRingGrowthEasePower = 3f;
    public const string cfgStealthSoundRingSortingLayerName = "Layer_Effect";
    public const int cfgStealthSoundRingSortingOrder = 0;
}
