using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Project-wide ent material configuration stored as an asset in a Resources folder, so it is available
/// in the editor, in any scene and in play mode without needing a scene object.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_EntMaterials", menuName = "Config/Ent Materials")]
public class F_Utility_Config_EntMaterials : ScriptableObject
{
    [Header("Constants Private")]
    private const string cfgEntMaterialsResourcePath = "OB_Config_EntMaterials";

    [Header("Ent Material Config")]
    // One entry per enumMaterialType; ents copy the entry matching their material
    public List<F_Ent_MaterialSettings> cfgEntMaterialSettings = new List<F_Ent_MaterialSettings>();

    [Header("Privates")]
    private static F_Utility_Config_EntMaterials instance;

    /// <summary>Raised in the editor when the asset is edited so visuals can refresh.</summary>
    public static event Action OnConfigChanged;

    private void Reset()
    {
        EnsureMaterialEntries();
    }

    private void OnValidate()
    {
        EnsureMaterialEntries();
        OnConfigChanged?.Invoke();
    }

    // Keeps one entry per material in the list, adding missing ones with default colours.
    private void EnsureMaterialEntries()
    {
        foreach (enumMaterialType material in Enum.GetValues(typeof(enumMaterialType)))
        {
            if (!cfgEntMaterialSettings.Exists(settings => settings != null && settings.materialType == material))
            {
                cfgEntMaterialSettings.Add(new F_Ent_MaterialSettings { materialType = material });
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_EntMaterials Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_EntMaterials>(cfgEntMaterialsResourcePath);
        }
        return instance;
    }

    /// <summary>Finds the configured settings for a material.</summary>
    /// <param name="material">The material to look up.</param>
    /// <returns>The matching settings, or null when the asset or entry is missing.</returns>
    public static F_Ent_MaterialSettings GetSettings(enumMaterialType material)
    {
        F_Utility_Config_EntMaterials config = Get();
        return config == null
            ? null
            : config.cfgEntMaterialSettings.Find(settings => settings != null && settings.materialType == material);
    }
}
