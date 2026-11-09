using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Project-wide biome configuration stored as an asset in a Resources folder. Maps each biome to the
/// ground sprite tiled across scenes using that biome.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_Biomes", menuName = "Config/Biomes")]
public class F_Utility_Config_Biomes : ScriptableObject
{
    /// <summary>Links a biome to the ground sprite tiled when that biome is active.</summary>
    [Serializable]
    public class BiomeGround
    {
        public enumBiome biome;
        public Sprite groundSprite;
    }

    [Header("Constants Private")]
    private const string cfgBiomesResourcePath = "OB_Config_Biomes";

    [Header("Biome Config")]
    // One entry per enumBiome; add an entry for every new biome
    public List<BiomeGround> cfgBiomeGrounds = new List<BiomeGround>();

    [Header("Privates")]
    private static F_Utility_Config_Biomes instance;

    /// <summary>Raised in the editor when the asset is edited so scenes can refresh their ground.</summary>
    public static event Action OnConfigChanged;

    private void Reset()
    {
        EnsureBiomeEntries();
    }

    private void OnValidate()
    {
        EnsureBiomeEntries();
        OnConfigChanged?.Invoke();
    }

    // Keeps one entry per biome in the list, adding missing ones with no sprite.
    private void EnsureBiomeEntries()
    {
        foreach (enumBiome biome in Enum.GetValues(typeof(enumBiome)))
        {
            if (!cfgBiomeGrounds.Exists(entry => entry != null && entry.biome == biome))
            {
                cfgBiomeGrounds.Add(new BiomeGround { biome = biome });
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_Biomes Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_Biomes>(cfgBiomesResourcePath);
        }
        return instance;
    }

    /// <summary>Finds the configured ground sprite for a biome.</summary>
    /// <param name="biome">The biome to look up.</param>
    /// <returns>The ground sprite, or null when the asset, entry or sprite is missing.</returns>
    public static Sprite GetGroundSprite(enumBiome biome)
    {
        F_Utility_Config_Biomes config = Get();
        BiomeGround entry = config == null
            ? null
            : config.cfgBiomeGrounds.Find(ground => ground != null && ground.biome == biome);
        return entry?.groundSprite;
    }
}
