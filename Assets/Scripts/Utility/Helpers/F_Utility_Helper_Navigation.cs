using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

public static class F_Utility_Helper_Navigation
{
    /// <summary>Configures a NavMesh modifier on every collider object of an ent from its features, so walls carve the NavMesh and movers never do.</summary>
    /// <param name="ent">The ent whose colliders are configured.</param>
    public static void ApplyEntNavigationFeatures(F_Ent ent)
    {
        if (ent == null)
        {
            return;
        }

        foreach (Collider2D collider in ent.GetComponentsInChildren<Collider2D>(true))
        {
            NavMeshModifier modifier = collider.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = collider.gameObject.AddComponent<NavMeshModifier>();
            }

            // Characters move around, so they must never carve holes in the NavMesh
            modifier.ignoreFromBuild = ent.IsNavigationMover;
            modifier.overrideArea = !ent.IsNavigationMover;
            modifier.area = ent.BlocksNavigation && !collider.isTrigger
                ? F_Utility_Config_Navigation.cfgNavigationAreaNotWalkable
                : F_Utility_Config_Navigation.cfgNavigationAreaWalkable;
        }
    }

    /// <summary>Finds a random point on the NavMesh within a radius of an origin.</summary>
    /// <param name="origin">World position to search around.</param>
    /// <param name="radius">Max distance from the origin.</param>
    /// <param name="point">The point found, or the origin when none is found.</param>
    /// <returns>True when a point on the NavMesh was found.</returns>
    public static bool TryGetRandomPointOnNavMesh(Vector2 origin, float radius, out Vector2 point)
    {
        for (int attempt = 0; attempt < F_Utility_Config_Navigation.cfgNavigationRandomPointAttempts; attempt++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * radius;
            if (TrySamplePosition(candidate, out point))
            {
                return true;
            }
        }

        point = origin;
        return false;
    }

    /// <summary>Whether a position is on, or within the sample distance of, the NavMesh.</summary>
    /// <param name="position">World position to check.</param>
    /// <returns>True when the NavMesh is close enough to the position.</returns>
    public static bool IsOnNavMesh(Vector2 position)
    {
        return TrySamplePosition(position, out _);
    }

    // Snaps a position to the nearest NavMesh point within the configured sample distance.
    private static bool TrySamplePosition(Vector2 position, out Vector2 point)
    {
        if (NavMesh.SamplePosition(new Vector3(position.x, position.y, 0f), out NavMeshHit hit,
            F_Utility_Config_Navigation.cfgNavigationSampleDistance, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = position;
        return false;
    }
}
