using UnityEngine;

/// <summary>Helpers for finding what material is under a world position.</summary>
public static class F_Utility_Helper_Ground
{
    /// <summary>
    /// Finds the material under a position: a floor-shaped world ent if present, then any ent on the floor layer,
    /// otherwise the active scene biome's ground material.
    /// </summary>
    /// <param name="position">World position to test.</param>
    /// <param name="groundMaterial">The material found.</param>
    /// <returns>True when a material could be determined.</returns>
    public static bool TryGetGroundMaterial(Vector2 position, out enumMaterialType groundMaterial)
    {
        Collider2D[] overlappingColliders = Physics2D.OverlapPointAll(position);
        int floorLayer = LayerMask.NameToLayer(F_Logic_Globals.layerFloor);

        // World floor prefabs use FloorSquare/FloorHalf and commonly remain on Default layer.
        foreach (Collider2D hit in overlappingColliders)
        {
            F_World floorWorld = hit.GetComponentInParent<F_World>();
            if (floorWorld != null && IsFloorShape(floorWorld.worldShape))
            {
                groundMaterial = floorWorld.entFeatureMaterial;
                return true;
            }
        }

        // Support floor entities that are assigned the dedicated floor layer but are not F_World objects.
        foreach (Collider2D hit in overlappingColliders)
        {
            if (hit.gameObject.layer != floorLayer)
            {
                continue;
            }

            F_Ent floorEnt = hit.GetComponentInParent<F_Ent>();
            if (floorEnt != null)
            {
                groundMaterial = floorEnt.entFeatureMaterial;
                return true;
            }
        }

        F_Scene_Settings sceneSettings = Object.FindFirstObjectByType<F_Scene_Settings>();
        if (sceneSettings != null)
        {
            return F_Utility_Config_Biomes.TryGetGroundMaterial(sceneSettings.sceneBiome, out groundMaterial);
        }

        groundMaterial = default;
        return false;
    }

    private static bool IsFloorShape(enumWorldShape worldShape)
    {
        return worldShape == enumWorldShape.FloorSquare || worldShape == enumWorldShape.FloorHalf;
    }
}
