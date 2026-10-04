using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Loading screen controller. Loads the persistent init scene, then the gameplay scene, then removes the temporary scenes.
/// </summary>
public class F_Logic_Loading : MonoBehaviour
{
    [Header("Object Refs")]
    public RectTransform loadingBarFill;

    [Header("Constants Private")]
    private const float initSceneProgressShare = 0.5f;
    private const float asyncLoadReadyProgress = 0.9f;

    private IEnumerator Start()
    {
        ApplyBarColours();
        SetProgress(0f);

        // Init scene holds the game manager and persistent objects, which mark themselves DontDestroyOnLoad.
        AsyncOperation initLoad = SceneManager.LoadSceneAsync(F_Utility_Config_Scenes.initSceneName, LoadSceneMode.Additive);
        while (!initLoad.isDone)
        {
            SetProgress(Mathf.Clamp01(initLoad.progress / asyncLoadReadyProgress) * initSceneProgressShare);
            yield return null;
        }

        // Let Awake/Start on the persistent objects run before the gameplay scene triggers player placement.
        yield return null;
        SetProgress(initSceneProgressShare);

        AsyncOperation gameplayLoad = SceneManager.LoadSceneAsync(F_Utility_Config_Scenes.initialGameplaySceneName, LoadSceneMode.Additive);
        while (!gameplayLoad.isDone)
        {
            SetProgress(initSceneProgressShare + Mathf.Clamp01(gameplayLoad.progress / asyncLoadReadyProgress) * (1f - initSceneProgressShare));
            yield return null;
        }

        SetProgress(1f);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(F_Utility_Config_Scenes.initialGameplaySceneName));

        SceneManager.UnloadSceneAsync(F_Utility_Config_Scenes.initSceneName);
        SceneManager.UnloadSceneAsync(F_Utility_Config_Scenes.loadingSceneName);
    }

    // Applies the central config colours to the loading bar fill and its background (the fill's parent).
    private void ApplyBarColours()
    {
        if (loadingBarFill == null)
        {
            return;
        }

        Image fillImage = loadingBarFill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = F_Utility_Config_Colours.cfgColourLoadingBarFill;
        }

        Image backgroundImage = loadingBarFill.parent != null ? loadingBarFill.parent.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.color = F_Utility_Config_Colours.cfgColourLoadingBarBackground;
        }
    }

    private void SetProgress(float progress)
    {
        if (loadingBarFill != null)
        {
            // The fill is stretched between its anchors, so the right anchor controls the visible width.
            loadingBarFill.anchorMax = new Vector2(progress, loadingBarFill.anchorMax.y);
        }
    }
}
