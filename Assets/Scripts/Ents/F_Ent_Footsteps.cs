using UnityEngine;

/// <summary>
/// Plays material-specific footsteps for any moving object. Movement speed directly controls both
/// the sound rate and playback volume, up to the configured maximum rate.
/// </summary>
public class F_Ent_Footsteps : MonoBehaviour
{
    [Header("Footstep Config")]
    // Movement speed at which playback volume reaches 100% and the sound rate reaches its cap.
    [Min(minimumFootstepsMaxRate)] public float footstepsMaxRate = defaultFootstepsMaxRate;

    [Header("Constants Private")]
    private const float defaultFootstepsMaxRate = 10f;
    private const float minimumFootstepsMaxRate = 0.01f;
    private const float cfgFootstepMaximumSoundsPerSecond = 5f;
    private const float cfgFootstepSneakLoudnessMaxSpeedRatio = 0.33f;
    private const float cfgFootstepWalkLoudnessMaxSpeedRatio = 0.66f;
    private const float maximumFootstepVolume = 100f;

    [Header("Privates")]
    private Rigidbody2D movementRigidbody;
    private Vector2 lastPosition;
    private float footstepRateAccumulator;

    private void Awake()
    {
        movementRigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        footstepRateAccumulator = 0f;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || footstepsMaxRate < minimumFootstepsMaxRate)
        {
            lastPosition = transform.position;
            footstepRateAccumulator = 0f;
            return;
        }

        Vector2 currentPosition = transform.position;
        float measuredSpeed = Vector2.Distance(currentPosition, lastPosition) / deltaTime;
        lastPosition = currentPosition;

        float currentSpeed = movementRigidbody != null
            ? movementRigidbody.linearVelocity.magnitude
            : measuredSpeed;
        if (currentSpeed <= 0f)
        {
            footstepRateAccumulator = 0f;
            return;
        }

        float cappedSpeed = Mathf.Min(currentSpeed, footstepsMaxRate);
        float speedRatio = cappedSpeed / footstepsMaxRate;
        float soundsPerSecond = speedRatio * cfgFootstepMaximumSoundsPerSecond;
        footstepRateAccumulator += soundsPerSecond * deltaTime;
        while (footstepRateAccumulator >= 1f)
        {
            footstepRateAccumulator -= 1f;
            PlayFootstep(speedRatio);
        }
    }

    // Selects the loudness enum from speed bands and scales playback volume linearly with speed.
    private void PlayFootstep(float speedRatio)
    {
        EnumSoundLoudness loudness = speedRatio <= cfgFootstepSneakLoudnessMaxSpeedRatio
            ? EnumSoundLoudness.FootstepSneak
            : speedRatio <= cfgFootstepWalkLoudnessMaxSpeedRatio
                ? EnumSoundLoudness.FootstepWalk
                : EnumSoundLoudness.FootstepRun;
        float volume = speedRatio * maximumFootstepVolume;

        if (!F_Utility_Helper_Ground.TryGetGroundMaterial(transform.position, out enumMaterialType groundMaterial))
        {
            return;
        }

        F_Ent_MaterialSettings materialSettings = F_Logic_Globals.GetEntMaterialSettings(groundMaterial);
        AudioClip[] footstepSounds = materialSettings?.materialFootStepSounds;
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            return;
        }

        AudioClip footstepClip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        F_Logic_Audio.PlaySound(footstepClip, EnumSoundType.Spatial, loudness, volume, transform.position);
    }
}
