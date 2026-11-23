using UnityEngine;

/// <summary>
/// Purely visual hollow ring that grows from a sound source to its maximum radius, fades and destroys itself.
/// It has no collider or physics, so it never affects the world.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class F_Effects_SoundRing : MonoBehaviour
{
    [Header("Constants Private")]
    private const string soundRingShaderName = "Sprites/Default";
    private const string soundRingObjectNamePrefix = "Effect_SoundRing_";
    private const float soundRingMinimumLifetime = 0.01f;
    private const float soundRingFullCircleRadians = Mathf.PI * 2f;

    [Header("Privates")]
    private static Material soundRingSharedMaterial;
    private LineRenderer soundRingLineRenderer;
    private Vector3[] soundRingPoints;
    private float soundRingMaxRadius;
    private float soundRingAge;

    /// <summary>Spawns a sound ring at a world position, sized from the sound's hearing radius.</summary>
    /// <param name="soundPosition">World position the sound was made at.</param>
    /// <param name="soundRadiusDistance">Hearing radius of the sound in world units (0 for sounds nobody can hear).</param>
    public static void Spawn(Vector2 soundPosition, float soundRadiusDistance)
    {
        GameObject soundRingObject = new GameObject(soundRingObjectNamePrefix + soundRadiusDistance);
        soundRingObject.transform.position = soundPosition;
        soundRingObject.AddComponent<F_Effects_SoundRing>().Initialise(soundRadiusDistance);
    }

    private void Initialise(float soundRadiusDistance)
    {
        soundRingMaxRadius = Mathf.Clamp(
            soundRadiusDistance * F_Utility_Config_Stealth.cfgStealthSoundRingRadiusScale,
            F_Utility_Config_Stealth.cfgStealthSoundRingRadiusMin,
            F_Utility_Config_Stealth.cfgStealthSoundRingRadiusMax);

        int segmentCount = Mathf.Max(3, F_Utility_Config_Stealth.cfgStealthSoundRingSegmentCount);
        soundRingPoints = new Vector3[segmentCount];

        soundRingLineRenderer = GetComponent<LineRenderer>();
        soundRingLineRenderer.sharedMaterial = GetSharedMaterial();
        soundRingLineRenderer.useWorldSpace = false;
        soundRingLineRenderer.loop = true;
        soundRingLineRenderer.positionCount = segmentCount;
        soundRingLineRenderer.widthMultiplier = F_Utility_Config_Stealth.cfgStealthSoundRingThickness;
        soundRingLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        soundRingLineRenderer.receiveShadows = false;
        soundRingLineRenderer.sortingLayerName = F_Utility_Config_Stealth.cfgStealthSoundRingSortingLayerName;
        soundRingLineRenderer.sortingOrder = F_Utility_Config_Stealth.cfgStealthSoundRingSortingOrder;

        UpdateRing(0f);
    }

    private void Update()
    {
        float lifetime = Mathf.Max(soundRingMinimumLifetime, F_Utility_Config_Stealth.cfgStealthSoundRingLifetime);
        soundRingAge += Time.deltaTime;
        if (soundRingAge >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        UpdateRing(soundRingAge / lifetime);
    }

    // Grows the ring with an ease-out curve (fast start) and fades it out over the end of its lifetime.
    private void UpdateRing(float lifetimeProgress)
    {
        float easedProgress = 1f - Mathf.Pow(1f - lifetimeProgress, F_Utility_Config_Stealth.cfgStealthSoundRingGrowthEasePower);
        float currentRadius = soundRingMaxRadius * easedProgress;
        for (int pointIndex = 0; pointIndex < soundRingPoints.Length; pointIndex++)
        {
            float angle = soundRingFullCircleRadians * pointIndex / soundRingPoints.Length;
            soundRingPoints[pointIndex] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;
        }

        soundRingLineRenderer.SetPositions(soundRingPoints);

        float fadeStart = F_Utility_Config_Stealth.cfgStealthSoundRingFadeStartRatio;
        float fadeProgress = fadeStart < 1f ? Mathf.Clamp01((lifetimeProgress - fadeStart) / (1f - fadeStart)) : 0f;
        Color ringColour = F_Utility_Config_Colours.cfgColourStealthSoundRing;
        ringColour.a *= 1f - fadeProgress;
        soundRingLineRenderer.startColor = ringColour;
        soundRingLineRenderer.endColor = ringColour;
    }

    // One material is shared by every ring; per-ring colour and fade come from vertex colours.
    private static Material GetSharedMaterial()
    {
        if (soundRingSharedMaterial == null)
        {
            soundRingSharedMaterial = new Material(Shader.Find(soundRingShaderName));
        }

        return soundRingSharedMaterial;
    }
}
