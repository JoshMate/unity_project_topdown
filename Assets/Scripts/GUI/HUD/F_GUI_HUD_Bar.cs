using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class F_GUI_HUD_Bar : MonoBehaviour
{
    [Header("Object Refs")]
    public Slider barSlider;
    public TextMeshProUGUI barText;
    public TextMeshProUGUI barTextMax;
    public bool barTextDisplayMax;

    [Header("Weight Threshold Colouring (Optional)")]
    [Tooltip("Enable to let this bar's fill colour and centre label react to a weight class threshold. Leave disabled for bars that do not represent carry weight.")]
    public bool useWeightThresholds;
    public Image barFillImage;
    public TextMeshProUGUI barCenterText;

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

    /// <summary>Sets the colour of this bar's slider fill image.</summary>
    public void SetFillColour(Color fillColour)
    {
        if (barSlider != null && barSlider.fillRect != null)
        {
            Image fillImage = barSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = fillColour;
            }
        }
    }

    // Returns the configured colour for the given weight class.
    private Color GetWeightClassColor(enumWeightClass weightClass)
    {
        switch (weightClass)
        {
            case enumWeightClass.FreeWeight:
                return F_Utility_Config_Colours.cfgColourHudWeightFree;
            case enumWeightClass.LightWeight:
                return F_Utility_Config_Colours.cfgColourHudWeightLight;
            case enumWeightClass.MediumWeight:
                return F_Utility_Config_Colours.cfgColourHudWeightMedium;
            case enumWeightClass.HeavyWeight:
                return F_Utility_Config_Colours.cfgColourHudWeightHeavy;
            case enumWeightClass.TooMuchWeight:
                return F_Utility_Config_Colours.cfgColourHudWeightTooMuch;
            default:
                return F_Utility_Config_Colours.cfgColourWhite;
        }
    }

    // Returns a plain-English label for the given weight class.
    private string GetWeightClassLabel(enumWeightClass weightClass)
    {
        switch (weightClass)
        {
            case enumWeightClass.FreeWeight:
                return "Weightless";
            case enumWeightClass.LightWeight:
                return "Light Weight";
            case enumWeightClass.MediumWeight:
                return "Medium Weight";
            case enumWeightClass.HeavyWeight:
                return "Heavy Weight";
            case enumWeightClass.TooMuchWeight:
                return "Over Weight";
            default:
                return string.Empty;
        }
    }
}
