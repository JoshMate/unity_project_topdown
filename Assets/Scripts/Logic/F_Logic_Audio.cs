using UnityEngine;

public class F_Logic_Audio : MonoBehaviour
{
    [Header("Constants Private")]
    private const float defaultSpatialFalloffDistance = 20f;
    private const float minimumSpatialAudioDistance = 0.01f;

    [Header("Privates")]
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

    /// <summary>
    /// Plays a direct 2D sound or a spatial sound at the supplied world position.
    /// Spatial playback uses a linear falloff distance independent of soundRadius.
    /// </summary>
    /// <param name="soundFile">Audio clip to play.</param>
    /// <param name="soundType">Playback type; invalid values default to Direct.</param>
    /// <param name="soundRadius">Reserved for stealth and hearing systems; it does not affect playback volume.</param>
    /// <param name="soundVolume">Volume from 0 to 100; invalid values default to 100.</param>
    /// <param name="soundSource">World position used only for Spatial playback.</param>
    public static void PlaySound(
        AudioClip soundFile,
        EnumSoundType soundType = EnumSoundType.Direct,
        float soundRadius = 0f,
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

        audioManager.CreateAndPlaySound(soundFile, soundType, soundRadius, soundVolume / 100f, soundSource);
    }

    private static F_Logic_Audio GetInstance()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<F_Logic_Audio>();
        }

        return instance;
    }

    private void CreateAndPlaySound(
        AudioClip soundFile,
        EnumSoundType soundType,
        float soundRadius,
        float normalizedVolume,
        Vector3 soundSource)
    {
        // soundRadius is intentionally not used for playback attenuation; it is reserved for stealth/hearing.
        _ = soundRadius;

        GameObject soundObject = new GameObject($"Audio_{soundFile.name}");
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = soundFile;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = normalizedVolume;

        if (soundType == EnumSoundType.Spatial)
        {
            Vector3 playbackPosition = soundSource;
            playbackPosition.z = GetListenerDepth(soundSource.z);
            soundObject.transform.position = playbackPosition;

            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = minimumSpatialAudioDistance;
            audioSource.maxDistance = Mathf.Max(
                spatialFalloffDistance,
                minimumSpatialAudioDistance * 2f);
            audioSource.dopplerLevel = 0f;
        }
        else
        {
            audioSource.spatialBlend = 0f;
        }

        audioSource.Play();
        Destroy(soundObject, soundFile.length);
    }

    private float GetListenerDepth(float fallbackDepth)
    {
        if (gameManager != null &&
            gameManager.cameraObject != null &&
            gameManager.cameraObject.playerCamera != null)
        {
            return gameManager.cameraObject.playerCamera.transform.position.z;
        }

        return fallbackDepth;
    }
}
