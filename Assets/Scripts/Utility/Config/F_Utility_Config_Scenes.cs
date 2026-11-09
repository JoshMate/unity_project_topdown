/// <summary>
/// Global scene name configuration used by the menu, loading and init flow,
/// plus defaults for the per-scene settings object.
/// </summary>
public static class F_Utility_Config_Scenes
{
    [UnityEngine.Header("Constants Public")]
    public const string mainMenuSceneName = "Scene_MainMenu";
    public const string loadingSceneName = "Scene_Loading";
    public const string initSceneName = "Scene_Logic_Init";
    public const string initialGameplaySceneName = "Scene_TestRoom";

    [UnityEngine.Header("Constants Public Scene Settings")]
    public const string cfgSceneGroundSortingLayerName = "Layer_Background";
    public const int cfgSceneGroundSortingOrder = short.MinValue;
    public const float cfgSceneGroundDefaultWidth = 200f;
    public const float cfgSceneGroundDefaultHeight = 200f;
}
