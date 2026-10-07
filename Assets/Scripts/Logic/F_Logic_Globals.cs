using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Logic_Globals : MonoBehaviour
{

    [Header("Constants Public")]

    // Tag Names
    public const string tagEnt = "Tag_Ent";

    // Layers
    public const string layerDebugAlwaysTop = "Layer_DebugAlwaysTop";
    public const string layerLogic = "Layer_Logic";
    public const string layerGui = "Layer_Gui";
    public const string layerPopUps = "Layer_PopUps";
    public const string layerHud = "Layer_HudBottom";
    public const string layerProjectile = "Layer_Projectile";
    public const string layerEffect = "Layer_Effect";
    public const string layerParticle = "Layer_Particle";
    public const string layerPlayerOver = "Layer_PlayerOver";
    public const string layerPlayerBody = "Layer_PlayerBody";
    public const string layerPlayerUnder = "Layer_PlayerUnder";
    public const string layerNPCBoss = "Layer_NPCBoss";
    public const string layerNPC = "Layer_NPC";
    public const string layerItem = "Layer_Item";
    public const string layerEntDynamic = "Layer_EntDynamic";
    public const string layerEntUsable = "Layer_EntUsable";
    public const string layerEntDecoration = "Layer_EntDecoration";
    public const string layerWallDecal = "Layer_WallDecal";
    public const string layerWall = "Layer_Wall";
    public const string layerFloorDecal = "Layer_FloorDecal";
    public const string layerFloorDecoration = "Layer_FloorDecoration";
    public const string layerFloor = "Layer_Floor";
    public const string layerBackground = "Layer_Background";

    // Strings
    public const string stringGameName = "Project Top Down";

    [Header("Ent Material Config")]
    // One entry per enumMaterialType; ents copy the entry matching their material
    public List<F_Ent_MaterialSettings> entMaterialSettings = new List<F_Ent_MaterialSettings>();

    [Header("Privates")]
    private static F_Logic_Globals instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {

    }

    // Keeps one entry per material in the list, adding missing ones with default colours.
    void Reset()
    {
        EnsureMaterialEntries();
    }

    void OnValidate()
    {
        EnsureMaterialEntries();
    }

    private void EnsureMaterialEntries()
    {
        foreach (enumMaterialType material in Enum.GetValues(typeof(enumMaterialType)))
        {
            if (!entMaterialSettings.Exists(settings => settings != null && settings.materialType == material))
            {
                entMaterialSettings.Add(new F_Ent_MaterialSettings
                {
                    materialType = material,
                    materialBloodColour = F_Utility_Helper_Damage.GetBloodColour(material)
                });
            }
        }
    }

    /// <summary>Finds the configured settings for a material.</summary>
    /// <param name="material">The material to look up.</param>
    /// <returns>The matching settings, or null when no globals object or entry exists.</returns>
    public static F_Ent_MaterialSettings GetEntMaterialSettings(enumMaterialType material)
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<F_Logic_Globals>();
        }

        return instance == null
            ? null
            : instance.entMaterialSettings.Find(settings => settings != null && settings.materialType == material);
    }
}
