using System.Collections.Generic;
using UnityEngine;

public class F_Logic_Audio : MonoBehaviour
{
    [Header("Constants Private")]
    private const float defaultSpatialFalloffDistance = 20f;
    private const float minimumSpatialAudioDistance = 0.01f;

    [Header("Privates")]
    private readonly List<SpatialSoundPlayback> activeSpatialSounds = new List<SpatialSoundPlayback>();

    private sealed class SpatialSoundPlayback
    {
        private readonly AudioSource audioSource;
        private readonly Vector2 soundPosition;
        private readonly float normalizedVolume;

        public SpatialSoundPlayback(AudioSource audioSource, Vector2 soundPosition, float normalizedVolume)
        {
            this.audioSource = audioSource;
            this.soundPosition = soundPosition;
            this.normalizedVolume = normalizedVolume;
        }

        public void UpdateVolume(Vector2 listenerPosition, float attenuationDistance)
        {
            float distance = Vector2.Distance(soundPosition, listenerPosition);
            float distanceAttenuation = 1f - Mathf.Clamp01(distance / attenuationDistance);
            audioSource.volume = normalizedVolume * distanceAttenuation;
        }

        public bool IsFinished => audioSource == null || !audioSource.isPlaying;
    }

    [SerializeField] private float spatialFalloffDistance = defaultSpatialFalloffDistance;
    private static F_Logic_Audio instance;
    private F_Logic_GameManager gameManager;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameManager = GetComponentInParent<F_Logic_GameManager>();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        Vector2 listenerPosition = GetListenerPosition();
        float attenuationDistance = GetSpatialAttenuationDistance();

        for (int soundIndex = activeSpatialSounds.Count - 1; soundIndex >= 0; soundIndex--)
        {
            SpatialSoundPlayback spatialSound = activeSpatialSounds[soundIndex];
            if (spatialSound.IsFinished)
            {
                activeSpatialSounds.RemoveAt(soundIndex);
                continue;
            }

            spatialSound.UpdateVolume(listenerPosition, attenuationDistance);
        }
    }

    /// <summary>
    /// Plays a direct 2D sound or a centered spatial sound at the supplied world position.
    /// The sound-loudness enum is converted to a world-distance radius for hearing systems, independently of playback volume.
    /// </summary>
    /// <param name="soundFile">Audio clip to play.</param>
    /// <param name="soundType">Playback type; invalid values default to Direct.</param>
    /// <param name="soundRadius">Reserved for stealth and hearing systems; it does not affect playback volume.</param>
    /// <param name="soundVolume">Volume from 0 to 100; invalid values default to 100.</param>
    /// <param name="soundSource">World position associated with the sound; used for spatial playback and hearing metadata.</param>
    public static void PlaySound(
        AudioClip soundFile,
        EnumSoundType soundType = EnumSoundType.Direct,
        EnumSoundLoudness soundRadius = EnumSoundLoudness.NoSound,
        float soundVolume = 100f,
        Vector3 soundSource = default)
    {
        if (soundFile == null)
        {
            Debug.LogWarning("F_Logic_Audio.PlaySound was called without an AudioClip.");
            return;
        }

        if (!System.Enum.IsDefined(typeof(EnumSoundType), soundType))
        {
            soundType = EnumSoundType.Direct;
        }

        float soundRadiusDistance = GetSoundRadiusDistance(soundRadius);

        if (float.IsNaN(soundVolume) || float.IsInfinity(soundVolume) || soundVolume < 0f || soundVolume > 100f)
        {
            soundVolume = 100f;
        }

        F_Logic_Audio audioManager = GetInstance();
        if (audioManager == null)
        {
            Debug.LogWarning("F_Logic_Audio.PlaySound could not find the Logic_Audio manager.");
            return;
        }

        audioManager.CreateAndPlaySound(soundFile, soundType, soundRadiusDistance, soundVolume / 100f, soundSource);
    }

    private static F_Logic_Audio GetInstance()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<F_Logic_Audio>();
        }

        return instance;
    }

    private static float GetSoundRadiusDistance(EnumSoundLoudness soundLoudness)
    {
        return System.Enum.IsDefined(typeof(EnumSoundLoudness), soundLoudness)
            ? (int)soundLoudness
            : 0f;
    }

    private void CreateAndPlaySound(
        AudioClip soundFile,
        EnumSoundType soundType,
        float soundRadius,
        float normalizedVolume,
        Vector3 soundSource)
    {
        // Radius is carried through sound generation for stealth/hearing systems, not playback attenuation.
        _ = soundRadius;

        GameObject soundObject = new GameObject($"Audio_{soundFile.name}");
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = soundFile;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = normalizedVolume;

        if (soundType == EnumSoundType.Spatial)
        {
            audioSource.spatialBlend = 0f;
            audioSource.panStereo = 0f;

            SpatialSoundPlayback spatialSound = new SpatialSoundPlayback(
                audioSource,
                new Vector2(soundSource.x, soundSource.y),
                normalizedVolume);
            spatialSound.UpdateVolume(GetListenerPosition(), GetSpatialAttenuationDistance());
            activeSpatialSounds.Add(spatialSound);
        }
        else
        {
            audioSource.spatialBlend = 0f;
        }

        audioSource.Play();
        Destroy(soundObject, soundFile.length);
    }

    private Vector2 GetListenerPosition()
    {
        if (gameManager != null &&
            gameManager.cameraObject != null &&
            gameManager.cameraObject.playerCamera != null)
        {
            Vector3 cameraPosition = gameManager.cameraObject.playerCamera.transform.position;
            return new Vector2(cameraPosition.x, cameraPosition.y);
        }

        AudioListener audioListener = FindFirstObjectByType<AudioListener>();
        if (audioListener != null)
        {
            Vector3 listenerPosition = audioListener.transform.position;
            return new Vector2(listenerPosition.x, listenerPosition.y);
        }

        return new Vector2(transform.position.x, transform.position.y);
    }

    private float GetSpatialAttenuationDistance()
    {
        return Mathf.Max(spatialFalloffDistance, minimumSpatialAudioDistance * 2f);
    }
}
