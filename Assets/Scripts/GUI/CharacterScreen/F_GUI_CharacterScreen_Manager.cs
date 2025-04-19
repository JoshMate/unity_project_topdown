using UnityEngine;

public class F_GUI_CharacterScreen_Manager : MonoBehaviour
{
    [Header("Object Refs")]
    public GameObject inventoryObject;

    [Header("privates")]
    private bool isInventoryOpen = false;

    public void ToggleInventoryScreen()
    {
        if (inventoryObject.activeSelf == true)
        {
            inventoryObject.SetActive(false);
        }
        else
        {
            inventoryObject.SetActive(true);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
