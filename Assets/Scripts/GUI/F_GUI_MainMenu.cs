using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Each entry binds a button to an action, so new options only need a new button and entry.
/// </summary>
public class F_GUI_MainMenu : MonoBehaviour
{
    [Serializable]
    public class MenuEntry
    {
        public Button menuButton;
        public EnumMainMenuAction menuAction;
    }

    [Header("Object Refs")]
    public GameObject menuMainPanel;
    public GameObject menuSettingsPanel;

    [Header("Menu Entries")]
    public MenuEntry[] menuEntries;

    private void Awake()
    {
        foreach (MenuEntry entry in menuEntries)
        {
            if (entry.menuButton == null)
            {
                continue;
            }

            EnumMainMenuAction action = entry.menuAction;
            entry.menuButton.onClick.AddListener(() => ExecuteAction(action));
        }

        ShowSettings(false);
    }

    /// <summary>
    /// Runs the behaviour for the given menu action.
    /// </summary>
    public void ExecuteAction(EnumMainMenuAction action)
    {
        switch (action)
        {
            case EnumMainMenuAction.Play:
                SceneManager.LoadScene(F_Utility_Config_Scenes.loadingSceneName, LoadSceneMode.Single);
                break;
            case EnumMainMenuAction.Settings:
                ShowSettings(true);
                break;
            case EnumMainMenuAction.Back:
                ShowSettings(false);
                break;
            case EnumMainMenuAction.Quit:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private void ShowSettings(bool showSettings)
    {
        if (menuMainPanel != null)
        {
            menuMainPanel.SetActive(!showSettings);
        }

        if (menuSettingsPanel != null)
        {
            menuSettingsPanel.SetActive(showSettings);
        }
    }
}
