using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies the configured wall or floor texture for an ent's material to its sprites. Play mode uses one cached
/// material per material type and world orientation so sprites keep batching; edit mode previews through a property block.
/// </summary>
public static class F_Utility_Helper_EntMaterial
{
    [Header("Constants Private")]
    private const string entMaterialRuntimeNamePrefix = "Mat_World_Runtime_";
    private const string entMaterialWallKeySuffix = "_Wall";
    private const string entMaterialFloorKeySuffix = "_Floor";
    private const float entMaterialMeshCoordsEnabled = 1f;
    private static readonly int entMaterialShaderIdWorldTexture = Shader.PropertyToID("_WorldTex");
    private static readonly int entMaterialShaderIdPixelsPerUnit = Shader.PropertyToID("_WorldTexPixelsPerUnit");
    private static readonly int entMaterialShaderIdGridOffset = Shader.PropertyToID("_WorldTexGridOffset");
    private static readonly int entMaterialShaderIdUseMeshCoords = Shader.PropertyToID("_WorldTexUseMeshCoords");

    [Header("Privates")]
    private static readonly Dictionary<string, Material> entMaterialRuntimeMaterials = new Dictionary<string, Material>();
    private static MaterialPropertyBlock entMaterialPropertyBlock;

    /// <summary>Applies the configured wall or floor texture of the ent's material to its sprites.</summary>
    /// <param name="world">The world object to texture.</param>
    public static void ApplyWorldTexture(F_World world)
    {
        if (world == null)
        {
            return;
        }

        F_Utility_Config_EntMaterials config = F_Utility_Config_EntMaterials.Get();
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(world.entFeatureMaterial);
        if (config == null || config.cfgEntMaterialWorldMaterial == null || settings == null)
        {
            return;
        }

        bool isFloor = IsFloorShape(world.worldShape);
        Texture2D worldTexture = isFloor ? settings.materialFloorTexture : settings.materialWallTexture;
        bool isPlaying = Application.isPlaying;
        Material material = isPlaying ? GetRuntimeMaterial(config, settings, worldTexture, isFloor) : config.cfgEntMaterialWorldMaterial;
        foreach (SpriteRenderer spriteRenderer in world.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (spriteRenderer.sharedMaterial != material)
            {
                spriteRenderer.sharedMaterial = material;
            }

            if (!isPlaying)
            {
                ApplyEditorPreview(spriteRenderer, config, worldTexture);
            }
        }
    }

    /// <summary>Makes a material sample its world texture from the mesh's UV0.zw instead of its live world position, locking the texture to the mesh.</summary>
    /// <param name="material">A material instance, ignored when its shader has no world texture.</param>
    public static void LockWorldTextureToMesh(Material material)
    {
        if (material != null && material.HasProperty(entMaterialShaderIdUseMeshCoords))
        {
            material.SetFloat(entMaterialShaderIdUseMeshCoords, entMaterialMeshCoordsEnabled);
        }
    }

    /// <summary>Destroys the cached runtime materials so the next apply rebuilds them from the current config.</summary>
    public static void ClearRuntimeMaterials()
    {
        foreach (Material material in entMaterialRuntimeMaterials.Values)
        {
            if (material != null)
            {
                Object.DestroyImmediate(material);
            }
        }
        entMaterialRuntimeMaterials.Clear();
    }

    // Returns the shared runtime material for a material type and world orientation, creating it on first use.
    private static Material GetRuntimeMaterial(F_Utility_Config_EntMaterials config, F_Ent_MaterialSettings settings, Texture2D worldTexture, bool isFloor)
    {
        string orientationSuffix = isFloor ? entMaterialFloorKeySuffix : entMaterialWallKeySuffix;
        string materialKey = settings.materialType + orientationSuffix;
        if (entMaterialRuntimeMaterials.TryGetValue(materialKey, out Material material) && material != null)
        {
            return material;
        }

        material = new Material(config.cfgEntMaterialWorldMaterial)
        {
            name = entMaterialRuntimeNamePrefix + materialKey,
            hideFlags = HideFlags.DontSave
        };
        material.SetTexture(entMaterialShaderIdWorldTexture, worldTexture);
        material.SetFloat(entMaterialShaderIdPixelsPerUnit, config.cfgEntMaterialTexturePixelsPerUnit);
        material.SetVector(entMaterialShaderIdGridOffset, new Vector4(config.cfgEntMaterialTextureGridOffset.x, config.cfgEntMaterialTextureGridOffset.y, 0f, 0f));
        entMaterialRuntimeMaterials[materialKey] = material;
        return material;
    }

    // Shows the selected wall or floor texture in the editor without creating or saving material instances.
    private static void ApplyEditorPreview(SpriteRenderer spriteRenderer, F_Utility_Config_EntMaterials config, Texture2D worldTexture)
    {
        entMaterialPropertyBlock ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(entMaterialPropertyBlock);
        if (worldTexture != null)
        {
            entMaterialPropertyBlock.SetTexture(entMaterialShaderIdWorldTexture, worldTexture);
        }
        entMaterialPropertyBlock.SetFloat(entMaterialShaderIdPixelsPerUnit, config.cfgEntMaterialTexturePixelsPerUnit);
        entMaterialPropertyBlock.SetVector(entMaterialShaderIdGridOffset, new Vector4(config.cfgEntMaterialTextureGridOffset.x, config.cfgEntMaterialTextureGridOffset.y, 0f, 0f));
        spriteRenderer.SetPropertyBlock(entMaterialPropertyBlock);
    }

    // Returns true for the configured floor shapes; all remaining world shapes are walls.
    private static bool IsFloorShape(enumWorldShape worldShape)
    {
        return worldShape == enumWorldShape.FloorSquare || worldShape == enumWorldShape.FloorHalf;
    }
}
