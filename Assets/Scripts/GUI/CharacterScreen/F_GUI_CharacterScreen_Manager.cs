using UnityEngine;

public class F_GUI_CharacterScreen_Manager : MonoBehaviour
{
    [Header("Object Refs")]
    public GameObject inventoryObject;
    // Panel showing the open container's slots; closed whenever the character screen closes
    public F_GUI_Container containerScreen;

    [Header("Public Checks")]
    public bool isMenuOpen = false;

    /// <summary>Opens the character screen if it is closed, otherwise closes it (and any open container).</summary>
    public void ToggleInventoryScreen()
    {
        if (isMenuOpen)
        {
            CloseInventoryScreen();
        }
        else
        {
            OpenInventoryScreen();
        }
    }

    /// <summary>Shows the character screen.</summary>
    public void OpenInventoryScreen()
    {
        inventoryObject.SetActive(true);
        isMenuOpen = true;
    }

    /// <summary>Hides the character screen and closes any open container.</summary>
    public void CloseInventoryScreen()
    {
        if (containerScreen != null)
        {
            containerScreen.CloseContainer();
        }

        inventoryObject.SetActive(false);
        isMenuOpen = false;
    }

    /// <summary>Opens the character screen (if needed) with the given container's slots shown.</summary>
    /// <param name="container">The container to open.</param>
    public void OpenContainerScreen(F_Ent_Container container)
    {
        if (containerScreen == null || container == null)
        {
            return;
        }

        OpenInventoryScreen();
        containerScreen.OpenContainer(container);
    }

    private void Awake()
    {
        CloseInventoryScreen();
    }
}
