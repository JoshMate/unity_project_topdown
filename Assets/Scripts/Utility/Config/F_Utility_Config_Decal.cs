public static class F_Utility_Config_Decal
{
    // Constants Public: Ent destroy decals
    // Decal diameter as a multiple of the destroyed ent's largest visual extent
    public const float cfgDecalSizeMultiplier = 1.2f;
    // Smallest decal diameter in world units, so tiny ents still leave a visible mark
    public const float cfgDecalSizeMin = 0.4f;
    // Sorting order within the floor decal layer
    public const int cfgDecalSortingOrder = 0;
    // Pixel width and height of the generated decal texture
    public const int cfgDecalTextureSize = 64;
    // Pixels per world unit of the generated decal sprite
    public const float cfgDecalPixelsPerUnit = 64f;
    // Fraction of the radius that stays fully opaque before the edge fades out
    public const float cfgDecalSolidRadiusFraction = 0.6f;
    // Peak alpha of the decal
    public const float cfgDecalAlpha = 0.85f;
    // Edge irregularity, 0 is a perfect circle
    public const float cfgDecalEdgeNoise = 0.15f;
    // Frequency of the edge irregularity around the decal
    public const float cfgDecalEdgeNoiseFrequency = 5f;
    // Object name given to spawned decals
    public const string cfgDecalObjectName = "Decal_Destroy";
}
