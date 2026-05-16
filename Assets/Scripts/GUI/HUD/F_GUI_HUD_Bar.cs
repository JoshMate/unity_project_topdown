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
}
