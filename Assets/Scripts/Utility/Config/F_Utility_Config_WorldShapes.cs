using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Project-wide world shape configuration stored as an asset in a Resources folder, so it is available
/// in the editor, in any scene and in play mode without needing a scene object.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_WorldShapes", menuName = "Config/World Shapes")]
public class F_Utility_Config_WorldShapes : ScriptableObject
{
    [Header("Constants Private")]
    private const string cfgWorldShapesResourcePath = "OB_Config_WorldShapes";

    [Header("World Shape Config")]
    // One entry per enumWorldShape; world objects use the entry matching their shape
    public List<F_World_ShapeSettings> cfgWorldShapeSettings = new List<F_World_ShapeSettings>();

    [Header("Privates")]
    private static F_Utility_Config_WorldShapes instance;

    /// <summary>Raised in the editor when the asset is edited so visuals can refresh.</summary>
    public static event Action OnConfigChanged;

    private void Reset()
    {
        EnsureShapeEntries();
    }

    private void OnValidate()
    {
        EnsureShapeEntries();
        OnConfigChanged?.Invoke();
    }

    // Keeps one entry per shape in the list, adding missing ones with no sprite.
    private void EnsureShapeEntries()
    {
        foreach (enumWorldShape shape in Enum.GetValues(typeof(enumWorldShape)))
        {
            if (!cfgWorldShapeSettings.Exists(settings => settings != null && settings.shapeType == shape))
            {
                cfgWorldShapeSettings.Add(new F_World_ShapeSettings { shapeType = shape });
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_WorldShapes Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_WorldShapes>(cfgWorldShapesResourcePath);
        }
        return instance;
    }

    /// <summary>Finds the configured settings for a shape.</summary>
    /// <param name="shape">The shape to look up.</param>
    /// <returns>The matching settings, or null when the asset or entry is missing.</returns>
    public static F_World_ShapeSettings GetSettings(enumWorldShape shape)
    {
        F_Utility_Config_WorldShapes config = Get();
        return config == null
            ? null
            : config.cfgWorldShapeSettings.Find(settings => settings != null && settings.shapeType == shape);
    }
}
