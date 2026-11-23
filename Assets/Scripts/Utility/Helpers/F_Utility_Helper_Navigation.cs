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

    /// <summary>
    /// Whether a complete NavMesh route links two points. When it doesn't, routeEnd is where the closest partial route
    /// stops. A destination too far from the NavMesh to judge counts as reachable.
    /// </summary>
    /// <param name="from">Where the route starts; must be on the NavMesh.</param>
    /// <param name="to">Where the route should end.</param>
    /// <param name="areaMask">NavMesh areas the route may use, such as an agent's areaMask.</param>
    /// <param name="path">Reusable path the route is calculated into, so checks don't allocate a new one.</param>
    /// <param name="routeEnd">Where the calculated route ends, or the start when there is no route at all.</param>
    /// <returns>True when the destination can be fully reached.</returns>
    public static bool HasCompletePath(Vector2 from, Vector2 to, int areaMask, NavMeshPath path, out Vector2 routeEnd)
    {
        routeEnd = from;
        if (!TrySamplePosition(to, out Vector2 navTo))
        {
            return true;
        }

        if (!NavMesh.CalculatePath(new Vector3(from.x, from.y, 0f), new Vector3(navTo.x, navTo.y, 0f), areaMask, path))
        {
            return false;
        }

        Vector3[] routeCorners = path.corners;
        if (routeCorners.Length > 0)
        {
            routeEnd = routeCorners[routeCorners.Length - 1];
        }

        return path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>Whether a position is on, or within the sample distance of, the NavMesh.</summary>
    /// <param name="position">World position to check.</param>
    /// <returns>True when the NavMesh is close enough to the position.</returns>
    public static bool IsOnNavMesh(Vector2 position)
    {
        return TrySamplePosition(position, out _);
    }

    /// <summary>Snaps a position to the nearest NavMesh point within the configured sample distance.</summary>
    /// <param name="position">World position to snap.</param>
    /// <param name="point">The NavMesh point found, or the original position when none is close enough.</param>
    /// <returns>True when a NavMesh point was found.</returns>
    public static bool TrySamplePosition(Vector2 position, out Vector2 point)
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
