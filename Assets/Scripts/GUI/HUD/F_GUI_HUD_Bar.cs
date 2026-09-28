using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class F_GUI_HUD_Bar : MonoBehaviour
{
    public Slider barSlider;
    public TextMeshProUGUI barText;
    public TextMeshProUGUI barTextMax;
    public bool barTextDisplayMax;

    [Header("Weight Threshold Colouring (Optional)")]
    [Tooltip("Enable to let this bar's fill colour and centre label react to a weight class threshold. Leave disabled for bars that do not represent carry weight.")]
    public bool useWeightThresholds;
    public Image barFillImage;
    public TextMeshProUGUI barCenterText;
    public Color weightClassColorFreeWeight = new Color(0.2f, 0.5f, 1f);
    public Color weightClassColorLightWeight = new Color(0.2f, 0.8f, 0.2f);
    public Color weightClassColorMediumWeight = new Color(1f, 0.92f, 0.2f);
    public Color weightClassColorHeavyWeight = new Color(1f, 0.55f, 0f);
    public Color weightClassColorTooMuchWeight = new Color(0.9f, 0.1f, 0.1f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Only show the max value if enabled
        if (barTextDisplayMax == true)
        {
            barTextMax.enabled = true;
        }
        if (barTextDisplayMax == false)
        {
            barTextMax.enabled = false;
        }
    }

    /// <summary>Applies the fill colour and centre label for the given weight class. No-op unless useWeightThresholds is enabled for this bar.</summary>
    public void SetWeightClass(enumWeightClass weightClass)
    {
        if (useWeightThresholds == false)
        {
            return;
        }

        if (barFillImage != null)
        {
            barFillImage.color = GetWeightClassColor(weightClass);
        }
        if (barCenterText != null)
        {
            barCenterText.text = GetWeightClassLabel(weightClass);
        }
    }

    // Returns the configured colour for the given weight class.
    private Color GetWeightClassColor(enumWeightClass weightClass)
    {
        switch (weightClass)
        {
            case enumWeightClass.FreeWeight:
                return weightClassColorFreeWeight;
            case enumWeightClass.LightWeight:
                return weightClassColorLightWeight;
            case enumWeightClass.MediumWeight:
                return weightClassColorMediumWeight;
            case enumWeightClass.HeavyWeight:
                return weightClassColorHeavyWeight;
            case enumWeightClass.TooMuchWeight:
                return weightClassColorTooMuchWeight;
            default:
                return Color.white;
        }
    }

    // Returns a plain-English label for the given weight class.
    private string GetWeightClassLabel(enumWeightClass weightClass)
    {
        switch (weightClass)
        {
            case enumWeightClass.FreeWeight:
                return "Free";
            case enumWeightClass.LightWeight:
                return "Light";
            case enumWeightClass.MediumWeight:
                return "Medium";
            case enumWeightClass.HeavyWeight:
                return "Heavy";
            case enumWeightClass.TooMuchWeight:
                return "Over";
            default:
                return string.Empty;
        }
    }
}
