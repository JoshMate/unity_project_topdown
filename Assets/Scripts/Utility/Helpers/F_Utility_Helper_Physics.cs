using System.Collections.Generic;
using UnityEngine;

public static class F_Utility_Helper_Physics
{
    [Header("Constants Private")]
    private const int physicsLayerMissing = -1;
    // Fraction of a grid cell checked for blockers; under 1 so walls in neighbouring cells are not touched
    private const float physicsGridCellCheckScale = 0.8f;

    [Header("Privates")]
    private static readonly List<Collider2D> physicsGridOverlapResults = new List<Collider2D>();

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

    /// <summary>Finds free grid cell centres around an origin, nearest first. A cell is free when no solid collider blocks movers there and no floor item already occupies it.</summary>
    /// <param name="origin">World position to search around; snapped to the nearest grid cell first.</param>
    /// <param name="cellCount">How many free cells are wanted.</param>
    /// <param name="searchRadiusMax">How many rings of cells to search outward from the origin cell.</param>
    /// <param name="ignoredRoot">Colliders under this object are ignored (e.g. the object releasing the items); may be null.</param>
    /// <returns>Up to cellCount free cell centres; fewer when the search area runs out of space.</returns>
    public static List<Vector2> FindFreeGridCells(Vector2 origin, int cellCount, int searchRadiusMax, GameObject ignoredRoot)
    {
        List<Vector2> freeCells = new List<Vector2>();
        if (cellCount <= 0)
        {
            return freeCells;
        }

        float cellSize = F_Utility_Config_Editor.cfgEditorSnapCellSize;
        Vector2 originCell = SnapToGrid(origin);
        Vector2 cellCheckSize = Vector2.one * (cellSize * physicsGridCellCheckScale);
        int moverMask = GetPhysicsLayerMask(F_Utility_Config_Physics.cfgPhysicsLayerMover);
        List<Vector2Int> ringCells = new List<Vector2Int>();

        for (int ringRadius = 0; ringRadius <= searchRadiusMax && freeCells.Count < cellCount; ringRadius++)
        {
            GetGridRing(ringRadius, ringCells);
            foreach (Vector2Int ringCell in ringCells)
            {
                Vector2 cellCentre = originCell + (Vector2)ringCell * cellSize;
                if (IsGridCellFree(cellCentre, cellCheckSize, moverMask, ignoredRoot))
                {
                    freeCells.Add(cellCentre);
                    if (freeCells.Count >= cellCount)
                    {
                        break;
                    }
                }
            }
        }

        return freeCells;
    }

    // Snaps a world position to the centre of the nearest world grid cell.
    private static Vector2 SnapToGrid(Vector2 position)
    {
        float cellSize = F_Utility_Config_Editor.cfgEditorSnapCellSize;
        float cellOffset = F_Utility_Config_Editor.cfgEditorSnapCellOffset;
        return new Vector2(
            Mathf.Round((position.x - cellOffset) / cellSize) * cellSize + cellOffset,
            Mathf.Round((position.y - cellOffset) / cellSize) * cellSize + cellOffset);
    }

    // Fills the list with every cell offset on the square ring at the given radius, closest to the centre first.
    private static void GetGridRing(int ringRadius, List<Vector2Int> ringCells)
    {
        ringCells.Clear();
        for (int offsetX = -ringRadius; offsetX <= ringRadius; offsetX++)
        {
            for (int offsetY = -ringRadius; offsetY <= ringRadius; offsetY++)
            {
                if (Mathf.Max(Mathf.Abs(offsetX), Mathf.Abs(offsetY)) == ringRadius)
                {
                    ringCells.Add(new Vector2Int(offsetX, offsetY));
                }
            }
        }

        ringCells.Sort((first, second) => first.sqrMagnitude.CompareTo(second.sqrMagnitude));
    }

    // A cell is blocked by enabled solid colliders that movers cannot walk through, or by an item lying on the floor.
    private static bool IsGridCellFree(Vector2 cellCentre, Vector2 cellCheckSize, int moverMask, GameObject ignoredRoot)
    {
        physicsGridOverlapResults.Clear();
        Physics2D.OverlapBox(cellCentre, cellCheckSize, 0f, ContactFilter2D.noFilter, physicsGridOverlapResults);
        foreach (Collider2D overlapCollider in physicsGridOverlapResults)
        {
            if (overlapCollider == null || !overlapCollider.enabled ||
                (ignoredRoot != null && overlapCollider.transform.IsChildOf(ignoredRoot.transform)))
            {
                continue;
            }

            F_Item floorItem = overlapCollider.GetComponentInParent<F_Item>();
            if (floorItem != null && !floorItem.IsInInventorySlot)
            {
                return false;
            }

            bool blocksMovers = !overlapCollider.isTrigger && (overlapCollider.excludeLayers.value & moverMask) == 0;
            if (blocksMovers)
            {
                return false;
            }
        }

        return true;
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
