using UnityEngine;

public class F_GUI_CharacterScreen_Manager : MonoBehaviour
{
    [Header("Object Refs")]
    public GameObject inventoryObject;

    [Header("Public Checks")]
    public bool isMenuOpen = false;

    public void ToggleInventoryScreen()
    {
        if (inventoryObject.activeSelf == true)
        {
            inventoryObject.SetActive(false);
            isMenuOpen = false;
        }
        else
        {
            inventoryObject.SetActive(true);
            isMenuOpen = true;
        }
    }

    private void Awake()
    {
        inventoryObject.SetActive(false);
        isMenuOpen = false;
    }
}
