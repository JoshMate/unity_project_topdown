using UnityEngine;

/// <summary>
/// Stores data and configuration about a scene. The biome selects a ground sprite (configured in
/// OB_Config_Biomes) which is tiled across the scene background on the deepest sorting layer,
/// both in the editor and in game.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class F_Scene_Settings : MonoBehaviour
{
    [Header("Scene")]
    // Environment type of this scene; decides the ground texture
    public enumBiome sceneBiome = enumBiome.Grass;
    // World size in units covered by the tiled ground, centred on this object
    public Vector2 sceneGroundSize = new Vector2(
        F_Utility_Config_Scenes.cfgSceneGroundDefaultWidth,
        F_Utility_Config_Scenes.cfgSceneGroundDefaultHeight);

    [Header("Privates")]
    private SpriteRenderer groundRenderer;

    private void OnEnable()
    {
        F_Utility_Config_Biomes.OnConfigChanged += ApplyGround;
        ApplyGround();
    }

    private void OnDisable()
    {
        F_Utility_Config_Biomes.OnConfigChanged -= ApplyGround;
    }

    private void OnValidate()
    {
        ApplyGround();
    }

    private void Reset()
    {
        ApplyGround();
    }

    /// <summary>Tiles the ground sprite of the current biome across the scene on the deepest sorting layer.</summary>
    public void ApplyGround()
    {
        if (groundRenderer == null)
        {
            groundRenderer = GetComponent<SpriteRenderer>();
        }

        if (groundRenderer == null)
        {
            return;
        }

        groundRenderer.sprite = F_Utility_Config_Biomes.GetGroundSprite(sceneBiome);
        groundRenderer.drawMode = SpriteDrawMode.Tiled;
        groundRenderer.tileMode = SpriteTileMode.Continuous;
        groundRenderer.size = sceneGroundSize;
        groundRenderer.sortingLayerName = F_Utility_Config_Scenes.cfgSceneGroundSortingLayerName;
        groundRenderer.sortingOrder = F_Utility_Config_Scenes.cfgSceneGroundSortingOrder;
    }
}
