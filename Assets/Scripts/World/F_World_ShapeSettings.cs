using System;
using UnityEngine;

/// <summary>
/// Per-shape world configuration. One entry exists for every enumWorldShape value in OB_Config_WorldShapes,
/// and world objects use the entry matching their worldShape.
/// </summary>
[Serializable]
public class F_World_ShapeSettings
{
    [Header("Shape")]
    public enumWorldShape shapeType;

    [Header("Sprites")]
    // Sprite whose alpha defines the visual outline and whose physics shape defines the collider
    public Sprite shapeSprite;
}
