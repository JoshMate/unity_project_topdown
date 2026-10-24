using System;
using UnityEngine;

/// <summary>
/// Per-material ent configuration. One entry exists for every enumMaterialType value on F_Logic_Globals,
/// and ents copy the entry matching their entFeatureMaterial.
/// </summary>
[Serializable]
public class F_Ent_MaterialSettings
{
    [Header("Material")]
    public enumMaterialType materialType;

    [Header("Sounds")]
    // One clip is picked at random when an ent of this material gets hit
    public AudioClip[] materialHitSounds;
    // One clip is picked at random when an ent of this material dies
    public AudioClip[] materialDestroySounds;
    // One clip is picked at random when an ent of this material is walked on by the player or an npc
    public AudioClip[] materialFootStepSounds;

    [Header("Textures")]
    // Texture tiled seamlessly in world space across every world object of this material
    public Texture2D materialTexture;

    [Header("Colours")]
    // Colour of the on-hit particle effects for this material
    public Color materialBloodColour = Color.white;
    // Colour of the on-death decal effects for this material
    public Color materialDecalColour = Color.white;

    [Header("Health Stats")]
    // Default max health for world objects of this material; they start at full health
    public float materialHealthMax = 100f;

    [Header("Resistance Stats")]
    // Default resistances for world objects of this material. 1.0 fully immune, 0.0 takes full damage.
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceMelee = 0f;
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceBullet = 0f;
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceEnergy = 0f;
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceFire = 0f;
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceExplosive = 0f;
    [Range(materialResistanceMin, materialResistanceMax)] public float materialResistanceToxic = 0f;

    [Header("Constants Private")]
    private const float materialResistanceMin = 0f;
    private const float materialResistanceMax = 1f;
}
