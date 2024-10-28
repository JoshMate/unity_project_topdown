using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class F_GUI_Inventory_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    [Header("Object Refs")]
    public UnityEngine.UI.Image slotBorderArt;
    public UnityEngine.UI.Image slotItemArt;

    [Header("InventorySlotFlags")]
    public bool isSlotLocked;
    public bool isSlotHovered;

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }

   public void OnPointerEnter(PointerEventData eventData)
   {
        isSlotHovered = true;
   }

   public void OnPointerExit(PointerEventData eventData)
   {
        isSlotHovered = false;
   }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Update Border Colour For Hover
        if (isSlotHovered == true) 
        {
            slotBorderArt.color = Color.white;
        }
        if (isSlotHovered == false) 
        {
            slotBorderArt.color = Color.gray;
        }
    }
}
