using UnityEngine;

/// <summary>
/// Small health bar with a black background drawn just above an ent while it is damaged.
/// Added automatically by F_Ent; all values come from F_Utility_Config_Damage.
/// </summary>
[RequireComponent(typeof(F_Ent))]
public class F_Ent_HealthBar : MonoBehaviour
{
    [Header("Constants Private")]
    private const string healthBarObjectName = "HealthBar_Fill";
    private const string healthBarBackgroundObjectName = "HealthBar_Background";
    private const float healthBarFullFraction = 1f;
    private const int whiteTexturePixelSize = 1;
    private const float whiteSpritePixelsPerUnit = 1f;
    private const float healthBarHalf = 0.5f;

    [Header("Privates")]
    private F_Ent healthBarEnt;
    private Transform healthBarTransform;
    private SpriteRenderer healthBarRenderer;
    private Transform healthBarBackgroundTransform;
    private SpriteRenderer healthBarBackgroundRenderer;
    private static Sprite whiteSprite;

    private void Awake()
    {
        healthBarEnt = GetComponent<F_Ent>();
        CreateBar();
    }

    private void LateUpdate()
    {
        EnsureBarExists();

        float fraction = healthBarEnt.entStatHealthMax > 0f
            ? Mathf.Clamp01(healthBarEnt.entStatHealth / healthBarEnt.entStatHealthMax)
            : 0f;

        bool visible = !healthBarEnt.IsDead && fraction < healthBarFullFraction;
        if (healthBarRenderer.enabled != visible)
        {
            healthBarRenderer.enabled = visible;
            healthBarBackgroundRenderer.enabled = visible;
        }

        if (!visible)
        {
            return;
        }

        Bounds bounds = F_Utility_Helper_Damage.GetEntBounds(healthBarEnt);
        F_Utility_Helper_Damage.GetEntSorting(healthBarEnt, out int sortingLayerId, out int sortingOrder);

        // The bar is anchored at its left edge, centred on the ent's sprite and never rotates with it
        float barWidth = F_Utility_Config_Damage.cfgHealthBarWidth;
        healthBarTransform.rotation = Quaternion.identity;
        healthBarTransform.position = new Vector3(
            bounds.center.x - barWidth * healthBarHalf,
            bounds.center.y,
            healthBarEnt.transform.position.z);
        healthBarTransform.localScale = new Vector3(
            barWidth * fraction / whiteSpritePixelsPerUnit,
            F_Utility_Config_Damage.cfgHealthBarHeight,
            1f);

        healthBarBackgroundTransform.rotation = Quaternion.identity;
        healthBarBackgroundTransform.position = healthBarTransform.position;
        healthBarBackgroundTransform.localScale = new Vector3(
            barWidth / whiteSpritePixelsPerUnit,
            F_Utility_Config_Damage.cfgHealthBarHeight,
            1f);
        healthBarBackgroundRenderer.color = F_Utility_Config_Damage.cfgHealthBarColourBackground;
        healthBarBackgroundRenderer.sortingLayerID = sortingLayerId;
        healthBarBackgroundRenderer.sortingOrder = sortingOrder + F_Utility_Config_Damage.cfgHealthBarBackgroundSortingOrderOffset;

        healthBarRenderer.color = GetBarColour(fraction);
        healthBarRenderer.sortingLayerID = sortingLayerId;
        healthBarRenderer.sortingOrder = sortingOrder + F_Utility_Config_Damage.cfgHealthBarSortingOrderOffset;
    }

    // Builds the fill sprite as an unparented object so the ent's rotation and scale never distort it
    private void CreateBar()
    {
        CreateBarPart(healthBarObjectName, out healthBarTransform, out healthBarRenderer);
        CreateBarPart(healthBarBackgroundObjectName, out healthBarBackgroundTransform, out healthBarBackgroundRenderer);
    }

    // Creates one disabled white sprite object for the bar
    private void CreateBarPart(string partName, out Transform partTransform, out SpriteRenderer partRenderer)
    {
        GameObject partObject = new GameObject($"{partName}_{name}");
        partTransform = partObject.transform;
        partRenderer = partObject.AddComponent<SpriteRenderer>();
        partRenderer.sprite = GetWhiteSprite();
        partRenderer.enabled = false;
    }

    // Bar objects are unparented, so a scene unload can destroy them while a persistent ent lives on
    private void EnsureBarExists()
    {
        if (healthBarRenderer == null || healthBarBackgroundRenderer == null)
        {
            DestroyBar();
            CreateBar();
        }
    }

    private void DestroyBar()
    {
        if (healthBarBackgroundTransform != null)
        {
            Destroy(healthBarBackgroundTransform.gameObject);
        }

        if (healthBarTransform != null)
        {
            Destroy(healthBarTransform.gameObject);
        }
    }

    private void OnDestroy()
    {
        DestroyBar();
    }

    // Left-pivoted white 1x1 sprite shared by every bar
    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D texture = new Texture2D(whiteTexturePixelSize, whiteTexturePixelSize);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(
                texture,
                new Rect(0, 0, whiteTexturePixelSize, whiteTexturePixelSize),
                new Vector2(0f, healthBarHalf),
                whiteSpritePixelsPerUnit);
        }

        return whiteSprite;
    }

    // Picks the colour of the highest threshold the fraction reaches: green, yellow, orange then red
    private static Color GetBarColour(float fraction)
    {
        if (fraction >= F_Utility_Config_Damage.cfgHealthBarThresholdHigh)
        {
            return F_Utility_Config_Damage.cfgHealthBarColourHigh;
        }
        if (fraction >= F_Utility_Config_Damage.cfgHealthBarThresholdMedium)
        {
            return F_Utility_Config_Damage.cfgHealthBarColourMedium;
        }
        if (fraction >= F_Utility_Config_Damage.cfgHealthBarThresholdLow)
        {
            return F_Utility_Config_Damage.cfgHealthBarColourLow;
        }

        return F_Utility_Config_Damage.cfgHealthBarColourCritical;
    }
}
