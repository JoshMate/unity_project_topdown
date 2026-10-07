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
    public AudioClip[] materialDeathSounds;

    [Header("Colours")]
    // Colour of the on-hit particle effects for this material
    public Color materialBloodColour = Color.white;
}
