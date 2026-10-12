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

    [Header("Colours")]
    // Colour of interior fill of the material
    public Color materialFillColour = Color.white;
    // Colour of thin outline around the material
    public Color materialOutlineColour = Color.white;
    // Colour of the on-hit particle effects for this material
    public Color materialBloodColour = Color.white;
    // Colour of the on-death decal effects for this material
    public Color materialDecalColour = Color.white;
}
