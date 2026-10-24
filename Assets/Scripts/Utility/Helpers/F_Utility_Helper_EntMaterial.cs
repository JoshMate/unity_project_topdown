using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies the world texture configured for an ent's material to its sprites. Play mode uses one cached
/// material per material type so sprites keep batching; edit mode previews through a property block so
/// nothing temporary is saved into scenes or prefabs.
/// </summary>
public static class F_Utility_Helper_EntMaterial
{
    [Header("Constants Private")]
    private const string entMaterialRuntimeNamePrefix = "Mat_World_Runtime_";
    private const float entMaterialMeshCoordsEnabled = 1f;
    private static readonly int entMaterialShaderIdWorldTexture = Shader.PropertyToID("_WorldTex");
    private static readonly int entMaterialShaderIdPixelsPerUnit = Shader.PropertyToID("_WorldTexPixelsPerUnit");
    private static readonly int entMaterialShaderIdUseMeshCoords = Shader.PropertyToID("_WorldTexUseMeshCoords");

    [Header("Privates")]
    private static readonly Dictionary<enumMaterialType, Material> entMaterialRuntimeMaterials = new Dictionary<enumMaterialType, Material>();
    private static MaterialPropertyBlock entMaterialPropertyBlock;

    /// <summary>Applies the world texture of the ent's material to every sprite renderer on the ent and its children.</summary>
    /// <param name="ent">The ent to texture.</param>
    public static void ApplyWorldTexture(F_Ent ent)
    {
        if (ent == null)
        {
            return;
        }

        F_Utility_Config_EntMaterials config = F_Utility_Config_EntMaterials.Get();
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(ent.entFeatureMaterial);
        if (config == null || config.cfgEntMaterialWorldMaterial == null || settings == null)
        {
            return;
        }

        bool isPlaying = Application.isPlaying;
        Material material = isPlaying ? GetRuntimeMaterial(config, settings) : config.cfgEntMaterialWorldMaterial;
        foreach (SpriteRenderer spriteRenderer in ent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (spriteRenderer.sharedMaterial != material)
            {
                spriteRenderer.sharedMaterial = material;
            }

            if (!isPlaying)
            {
                ApplyEditorPreview(spriteRenderer, config, settings);
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

    // Returns the shared runtime material for a material type, creating it on first use.
    private static Material GetRuntimeMaterial(F_Utility_Config_EntMaterials config, F_Ent_MaterialSettings settings)
    {
        if (entMaterialRuntimeMaterials.TryGetValue(settings.materialType, out Material material) && material != null)
        {
            return material;
        }

        material = new Material(config.cfgEntMaterialWorldMaterial)
        {
            name = entMaterialRuntimeNamePrefix + settings.materialType,
            hideFlags = HideFlags.DontSave
        };
        material.SetTexture(entMaterialShaderIdWorldTexture, settings.materialTexture);
        material.SetFloat(entMaterialShaderIdPixelsPerUnit, config.cfgEntMaterialTexturePixelsPerUnit);
        entMaterialRuntimeMaterials[settings.materialType] = material;
        return material;
    }

    // Shows the texture in the editor without creating or saving material instances.
    private static void ApplyEditorPreview(SpriteRenderer spriteRenderer, F_Utility_Config_EntMaterials config, F_Ent_MaterialSettings settings)
    {
        entMaterialPropertyBlock ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(entMaterialPropertyBlock);
        if (settings.materialTexture != null)
        {
            entMaterialPropertyBlock.SetTexture(entMaterialShaderIdWorldTexture, settings.materialTexture);
        }
        entMaterialPropertyBlock.SetFloat(entMaterialShaderIdPixelsPerUnit, config.cfgEntMaterialTexturePixelsPerUnit);
        spriteRenderer.SetPropertyBlock(entMaterialPropertyBlock);
    }
}
