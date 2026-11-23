using System;
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

    // Events (Header attributes are not valid on events)
    // Fired for every spatial sound with a loudness radius: world position, loudness radius in world units, the character
    // that made it (null when no character owns it). Hearing systems such as F_NPC listen to this.
    public static event Action<Vector2, float, F_Character> OnWorldSoundPlayed;

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
    /// Plays a direct 2D sound (GUI and non-diegetic sounds) or a spatial sound at the supplied world position (anything
    /// made in the world). Spatial sounds with a loudness radius are also broadcast through OnWorldSoundPlayed so hearing
    /// systems can react, credited to the character that owns them.
    /// </summary>
    /// <param name="soundFile">Audio clip to play.</param>
    /// <param name="soundType">Playback type; invalid values default to Direct.</param>
    /// <param name="soundRadius">How far hearing systems can hear the sound; it does not affect playback volume.</param>
    /// <param name="soundVolume">Volume from 0 to 100; invalid values default to 100.</param>
    /// <param name="soundSource">World position the sound is made at; used for spatial playback and hearing.</param>
    /// <param name="soundOwner">Object credited with making the sound (a character, or anything held or fired by one); null when nobody made it.</param>
    public static void PlaySound(
        AudioClip soundFile,
        EnumSoundType soundType = EnumSoundType.Direct,
        EnumSoundLoudness soundRadius = EnumSoundLoudness.NoSound,
        float soundVolume = 100f,
        Vector3 soundSource = default,
        GameObject soundOwner = null)
    {
        if (soundFile == null)
        {
            Debug.LogWarning("F_Logic_Audio.PlaySound was called without an AudioClip.");
            return;
        }

        if (!Enum.IsDefined(typeof(EnumSoundType), soundType))
        {
            soundType = EnumSoundType.Direct;
        }

        if (float.IsNaN(soundVolume) || float.IsInfinity(soundVolume) || soundVolume < 0f || soundVolume > 100f)
        {
            soundVolume = 100f;
        }

        if (soundType == EnumSoundType.Spatial)
        {
            BroadcastWorldSound(soundSource, GetSoundRadiusDistance(soundRadius), soundOwner);
        }

        F_Logic_Audio audioManager = GetInstance();
        if (audioManager == null)
        {
            Debug.LogWarning("F_Logic_Audio.PlaySound could not find the Logic_Audio manager.");
            return;
        }

        audioManager.CreateAndPlaySound(soundFile, soundType, soundVolume / 100f, soundSource);
    }

    // Tells hearing systems about a sound made in the world; silent sounds are never heard.
    private static void BroadcastWorldSound(Vector3 soundSource, float soundRadiusDistance, GameObject soundOwner)
    {
        if (soundRadiusDistance <= 0f)
        {
            return;
        }

        F_Character soundOwnerCharacter = F_Utility_Helper_Faction.GetSourceCharacter(soundOwner);
        OnWorldSoundPlayed?.Invoke(new Vector2(soundSource.x, soundSource.y), soundRadiusDistance, soundOwnerCharacter);
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
        return Enum.IsDefined(typeof(EnumSoundLoudness), soundLoudness)
            ? (int)soundLoudness
            : 0f;
    }

    private void CreateAndPlaySound(
        AudioClip soundFile,
        EnumSoundType soundType,
        float normalizedVolume,
        Vector3 soundSource)
    {
        GameObject soundObject = new GameObject($"Audio_{soundFile.name}");
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = soundFile;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = normalizedVolume;
        audioSource.spatialBlend = 0f;

        if (soundType == EnumSoundType.Spatial)
        {
            audioSource.panStereo = 0f;

            SpatialSoundPlayback spatialSound = new SpatialSoundPlayback(
                audioSource,
                new Vector2(soundSource.x, soundSource.y),
                normalizedVolume);
            spatialSound.UpdateVolume(GetListenerPosition(), GetSpatialAttenuationDistance());
            activeSpatialSounds.Add(spatialSound);
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
