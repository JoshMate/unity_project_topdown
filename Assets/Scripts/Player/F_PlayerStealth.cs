using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player-only sneak presentation: fades a dim vignette behind the HUD while sneaking and shows a sound ring for every
/// spatial sound made while sneaking. It only reads the sneak state on F_PlayerStats and never affects the world.
/// </summary>
[RequireComponent(typeof(F_PlayerStats))]
public class F_PlayerStealth : MonoBehaviour
{
    [Header("Constants Private")]
    private const string playerStealthVignetteObjectName = "HUD_SneakVignette";
    private const float playerStealthVignetteHalf = 0.5f;

    [Header("Privates")]
    private F_PlayerStats playerStealthStats;
    private RawImage playerStealthVignetteImage;
    private Texture2D playerStealthVignetteTexture;
    private float playerStealthVignetteStrength;

    private void Awake()
    {
        playerStealthStats = GetComponent<F_PlayerStats>();
    }

    private void OnEnable()
    {
        F_Logic_Audio.OnSpatialSoundPlayed += HandleSpatialSoundPlayed;
    }

    private void OnDisable()
    {
        F_Logic_Audio.OnSpatialSoundPlayed -= HandleSpatialSoundPlayed;
    }

    private void OnDestroy()
    {
        if (playerStealthVignetteImage != null)
        {
            Destroy(playerStealthVignetteImage.gameObject);
        }

        if (playerStealthVignetteTexture != null)
        {
            Destroy(playerStealthVignetteTexture);
        }
    }

    private void Update()
    {
        UpdateVignette();
    }

    private void HandleSpatialSoundPlayed(Vector2 soundPosition, float soundRadiusDistance)
    {
        if (playerStealthStats != null && playerStealthStats.IsSneaking)
        {
            F_Effects_SoundRing.Spawn(soundPosition, soundRadiusDistance);
        }
    }

    // Fades the vignette towards visible while sneaking and hidden otherwise.
    private void UpdateVignette()
    {
        if (playerStealthVignetteImage == null && !TryBuildVignette())
        {
            return;
        }

        float targetStrength = playerStealthStats.IsSneaking ? 1f : 0f;
        float fadeStep = F_Utility_Config_Stealth.cfgStealthVignetteFadeDuration > 0f
            ? Time.deltaTime / F_Utility_Config_Stealth.cfgStealthVignetteFadeDuration
            : 1f;
        playerStealthVignetteStrength = Mathf.MoveTowards(playerStealthVignetteStrength, targetStrength, fadeStep);

        Color vignetteColour = F_Utility_Config_Colours.cfgColourStealthVignette;
        vignetteColour.a *= playerStealthVignetteStrength;
        playerStealthVignetteImage.color = vignetteColour;
        playerStealthVignetteImage.enabled = playerStealthVignetteStrength > 0f;
    }

    // Creates a full-screen vignette as the first child of the HUD canvas so it draws behind the HUD bars.
    private bool TryBuildVignette()
    {
        F_GUI_HUD_Manager hudManager = playerStealthStats != null ? playerStealthStats.playerRefHudManager : null;
        if (hudManager == null)
        {
            return false;
        }

        GameObject vignetteObject = new GameObject(playerStealthVignetteObjectName, typeof(RectTransform));
        RectTransform vignetteRect = vignetteObject.GetComponent<RectTransform>();
        vignetteRect.SetParent(hudManager.transform, false);
        vignetteRect.SetAsFirstSibling();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        playerStealthVignetteTexture = CreateVignetteTexture();
        playerStealthVignetteImage = vignetteObject.AddComponent<RawImage>();
        playerStealthVignetteImage.texture = playerStealthVignetteTexture;
        playerStealthVignetteImage.raycastTarget = false;
        playerStealthVignetteImage.enabled = false;
        return true;
    }

    // White texture whose alpha rises from the inner to the outer radius; tinted by the vignette colour.
    private static Texture2D CreateVignetteTexture()
    {
        int textureSize = Mathf.Max(2, F_Utility_Config_Stealth.cfgStealthVignetteTextureSize);
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        float lastPixelIndex = textureSize - 1;
        for (int pixelY = 0; pixelY < textureSize; pixelY++)
        {
            for (int pixelX = 0; pixelX < textureSize; pixelX++)
            {
                // Normalised so the screen edge midpoints sit at distance 1 from the centre
                Vector2 offsetFromCentre = new Vector2(pixelX / lastPixelIndex - playerStealthVignetteHalf, pixelY / lastPixelIndex - playerStealthVignetteHalf) / playerStealthVignetteHalf;
                float darkness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    F_Utility_Config_Stealth.cfgStealthVignetteInnerRadius,
                    F_Utility_Config_Stealth.cfgStealthVignetteOuterRadius,
                    offsetFromCentre.magnitude));
                pixels[pixelY * textureSize + pixelX] = new Color(1f, 1f, 1f, darkness);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
