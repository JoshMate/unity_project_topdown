/// <summary>
/// Global footstep configuration. Gait is decided from movement speed relative to a walk speed,
/// and the stride distance sets how often a step plays (faster movement means more frequent steps).
/// </summary>
public static class F_Utility_Config_Footsteps
{
    [UnityEngine.Header("Constants Public Gait")]
    // Sneaking is half of the walk speed
    public const float cfgFootstepSneakSpeedRatio = 0.5f;
    // Running is any speed meaningfully above the walk speed
    public const float cfgFootstepRunSpeedRatio = 1.25f;
    // Midpoint between sneak and walk speed; at or below this the mover counts as sneaking
    public const float cfgFootstepSneakThresholdRatio = 0.75f;
    // Speeds below this fraction of walk speed are treated as standing still
    public const float cfgFootstepMinimumSpeedRatio = 0.1f;
    // Small physics/render timing gaps do not count as a full stop
    public const float cfgFootstepStopResetDelay = 0.15f;

    [UnityEngine.Header("Constants Public Stride")]
    // World distance travelled between two footsteps
    public const float cfgFootstepStrideDistance = 1f;

    [UnityEngine.Header("Constants Public Volume")]
    // Volumes are 0-100 as expected by F_Logic_Audio.PlaySound
    public const float cfgFootstepSneakVolume = 30f;
    public const float cfgFootstepWalkVolume = 60f;
    public const float cfgFootstepRunVolume = 80f;
}
