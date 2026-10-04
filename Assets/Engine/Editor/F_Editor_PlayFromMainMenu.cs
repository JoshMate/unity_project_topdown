using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the editor Play button always start from the main menu scene, and keeps the boot scenes ordered in the build settings.
/// </summary>
[InitializeOnLoad]
public static class F_Editor_PlayFromMainMenu
{
    private const string scenesFolderPath = "Assets/Scenes/";
    private const string sceneExtension = ".unity";

    // Order matters: the first scene is the one a built game boots into.
    private static readonly string[] bootSceneNames =
    {
        F_Utility_Config_Scenes.mainMenuSceneName,
        F_Utility_Config_Scenes.loadingSceneName,
        F_Utility_Config_Scenes.initSceneName,
        F_Utility_Config_Scenes.initialGameplaySceneName
    };

    static F_Editor_PlayFromMainMenu()
    {
        string mainMenuScenePath = GetScenePath(F_Utility_Config_Scenes.mainMenuSceneName);
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuScenePath);
        if (mainMenuScene == null)
        {
            Debug.LogWarning($"Main menu scene not found at {mainMenuScenePath}; Play will use the open scene.");
        }

        EditorSceneManager.playModeStartScene = mainMenuScene;
        EnsureBootScenesInBuildSettings();
    }

    private static string GetScenePath(string sceneName)
    {
        return scenesFolderPath + sceneName + sceneExtension;
    }

    private static void EnsureBootScenesInBuildSettings()
    {
        var bootScenes = bootSceneNames
            .Select(GetScenePath)
            .Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            .Select(path => new EditorBuildSettingsScene(path, true));

        var otherScenes = EditorBuildSettings.scenes
            .Where(scene => !bootSceneNames.Any(name => scene.path == GetScenePath(name)));

        EditorBuildSettings.scenes = bootScenes.Concat(otherScenes).ToArray();
    }
}
