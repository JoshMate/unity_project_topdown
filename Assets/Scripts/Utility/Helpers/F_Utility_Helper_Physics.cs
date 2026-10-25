using UnityEngine;

public static class F_Utility_Helper_Physics
{
    [Header("Constants Private")]
    private const int physicsLayerMissing = -1;

    /// <summary>Puts every collider object under the root (including the root) on the named physics layer.</summary>
    /// <param name="root">The object whose colliders are assigned.</param>
    /// <param name="layerName">Physics layer name, normally from F_Utility_Config_Physics.</param>
    public static void AssignPhysicsLayer(GameObject root, string layerName)
    {
        int layerIndex = GetPhysicsLayerIndex(layerName);
        if (layerIndex == physicsLayerMissing)
        {
            return;
        }

        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
        {
            collider.gameObject.layer = layerIndex;
        }
    }

    /// <summary>Makes an ent's colliders ignore movers and/or projectiles according to its walk-through and shoot-through features.</summary>
    /// <param name="ent">The ent whose colliders are updated.</param>
    public static void ApplyEntPassThroughFeatures(F_Ent ent)
    {
        int moverMask = GetPhysicsLayerMask(F_Utility_Config_Physics.cfgPhysicsLayerMover);
        int projectileMask = GetPhysicsLayerMask(F_Utility_Config_Physics.cfgPhysicsLayerProjectile);

        int passThroughMask = (ent.entFeatureCanWalkThrough ? moverMask : 0)
            | (ent.entFeatureCanShootThrough ? projectileMask : 0);

        foreach (Collider2D collider in ent.GetComponentsInChildren<Collider2D>(true))
        {
            // Clear only the bits this system owns, so any other authored exclusions are preserved
            int otherExcludedLayers = collider.excludeLayers.value & ~(moverMask | projectileMask);
            collider.excludeLayers = otherExcludedLayers | passThroughMask;
        }
    }

    // Returns the layer's bit mask, or 0 when the layer has not been created.
    private static int GetPhysicsLayerMask(string layerName)
    {
        int layerIndex = GetPhysicsLayerIndex(layerName);
        return layerIndex == physicsLayerMissing ? 0 : 1 << layerIndex;
    }

    // Looks up a physics layer index and warns when it is missing from the project's layer list.
    private static int GetPhysicsLayerIndex(string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex == physicsLayerMissing)
        {
            Debug.LogWarning($"Physics layer '{layerName}' is missing. Add it under Project Settings > Tags and Layers.");
        }

        return layerIndex;
    }
}
