using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable weapon durability bar that builds its own child images and is sized by its host RectTransform.</summary>
/// <remarks>The coloured fill shows current durability of the full maximum, and a black segment at the opposite
/// end shows permanently lost durability. The fill colour runs green, yellow, orange, red as the effective durability drops.</remarks>
[RequireComponent(typeof(RectTransform))]
public class F_GUI_Durability_Bar : MonoBehaviour
{
    [Header("Constants Private")]
    private const string backgroundObjectName = "Durability_Background";
    private const string fillObjectName = "Durability_Fill";
    private const string lostObjectName = "Durability_Lost";
    private const float colourStopYellow = 0.66f;
    private const float colourStopOrange = 0.33f;
    private const float colourStopRed = 0f;
    private const float colourStopGood = 1f;

    private static readonly Color colourGood = F_Utility_Config_Colours.cfgDurabilityBarGood;
    private static readonly Color colourYellow = F_Utility_Config_Colours.cfgDurabilityBarYellow;
    private static readonly Color colourOrange = F_Utility_Config_Colours.cfgDurabilityBarOrange;
    private static readonly Color colourBad = F_Utility_Config_Colours.cfgDurabilityBarBad;
    private static readonly Color colourLost = F_Utility_Config_Colours.cfgDurabilityBarLost;
    private static readonly Color colourBackground = F_Utility_Config_Colours.cfgDurabilityBarBackground;

    [Header("Privates")]
    private RectTransform fillRect;
    private RectTransform lostRect;
    private Image fillImage;
    private bool isBuilt;

    /// <summary>Creates a durability bar child under the parent, positioned by the caller through the returned RectTransform.</summary>
    /// <param name="objectName">Name of the new bar object.</param>
    /// <param name="parent">Parent transform for the bar.</param>
    public static F_GUI_Durability_Bar Create(string objectName, Transform parent)
    {
        GameObject barObject = new GameObject(objectName, typeof(RectTransform));
        barObject.transform.SetParent(parent, false);
        return barObject.AddComponent<F_GUI_Durability_Bar>();
    }

    private void Awake()
    {
        BuildBar();
    }

    /// <summary>Shows the bar for the weapon's durability, or hides it when the weapon is null.</summary>
    /// <param name="weapon">The weapon to display.</param>
    public void SetDurability(F_Item_Weapon weapon)
    {
        if (weapon == null)
        {
            Hide();
            return;
        }

        BuildBar();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        float currentFraction = weapon.weaponDurabilityMax <= 0f
            ? 0f
            : Mathf.Clamp01(Mathf.Min(weapon.weaponDurabilityCurrent, weapon.GetDurabilityMaxEffective()) / weapon.weaponDurabilityMax);
        float lostFraction = weapon.GetDurabilityLostFraction();

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(currentFraction, 1f);
        lostRect.anchorMin = new Vector2(1f - lostFraction, 0f);
        lostRect.anchorMax = Vector2.one;
        fillImage.color = EvaluateDurabilityColour(weapon.GetDurabilityCurrentFraction());
    }

    /// <summary>Hides the bar.</summary>
    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private Color EvaluateDurabilityColour(float fraction)
    {
        if (fraction >= colourStopYellow)
        {
            return Color.Lerp(colourYellow, colourGood, Mathf.InverseLerp(colourStopYellow, colourStopGood, fraction));
        }

        if (fraction >= colourStopOrange)
        {
            return Color.Lerp(colourOrange, colourYellow, Mathf.InverseLerp(colourStopOrange, colourStopYellow, fraction));
        }

        return Color.Lerp(colourBad, colourOrange, Mathf.InverseLerp(colourStopRed, colourStopOrange, fraction));
    }

    private void BuildBar()
    {
        if (isBuilt)
        {
            return;
        }

        isBuilt = true;
        CreateBarImage(backgroundObjectName, colourBackground);
        fillImage = CreateBarImage(fillObjectName, colourGood);
        fillRect = fillImage.rectTransform;
        Image lostImage = CreateBarImage(lostObjectName, colourLost);
        lostRect = lostImage.rectTransform;
    }

    private Image CreateBarImage(string objectName, Color colour)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }
}
