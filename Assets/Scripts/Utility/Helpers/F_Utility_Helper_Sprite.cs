using System.Collections.Generic;
using UnityEngine;

public static class F_Utility_Helper_Sprite
{
    [Header("Constants Private")]
    // Outline vertices within this many pixels of the rightmost one count as part of the right edge
    private const float spriteRightEdgeTolerancePixels = 0.5f;
    private const float spriteHalfDivisor = 2f;

    [Header("Privates")]
    private static readonly List<Vector2> spritePhysicsShapeBuffer = new List<Vector2>();
    private static readonly List<Vector2> spriteOutlineBuffer = new List<Vector2>();

    /// <summary>
    /// Finds the centre of a sprite's rightmost opaque edge in its local space, such as the tip of a barrel pointing right.
    /// Uses the sprite's physics shape (pixel exact), then its mesh outline, then the right middle of its bounds.
    /// </summary>
    /// <param name="sprite">The sprite to measure.</param>
    /// <returns>The right edge centre in sprite local units, or zero when the sprite is missing.</returns>
    public static Vector2 GetRightEdgeCentre(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector2.zero;
        }

        List<Vector2> outlinePoints = GetOutlinePoints(sprite);
        if (outlinePoints.Count == 0)
        {
            return new Vector2(sprite.bounds.max.x, sprite.bounds.center.y);
        }

        float rightEdgeX = float.MinValue;
        foreach (Vector2 outlinePoint in outlinePoints)
        {
            rightEdgeX = Mathf.Max(rightEdgeX, outlinePoint.x);
        }

        float edgeTolerance = spriteRightEdgeTolerancePixels / sprite.pixelsPerUnit;
        float edgeMinY = float.MaxValue;
        float edgeMaxY = float.MinValue;
        foreach (Vector2 outlinePoint in outlinePoints)
        {
            if (rightEdgeX - outlinePoint.x <= edgeTolerance)
            {
                edgeMinY = Mathf.Min(edgeMinY, outlinePoint.y);
                edgeMaxY = Mathf.Max(edgeMaxY, outlinePoint.y);
            }
        }

        return new Vector2(rightEdgeX, (edgeMinY + edgeMaxY) / spriteHalfDivisor);
    }

    /// <summary>
    /// How far a drawn sprite's opaque outline reaches along another transform's local +X, measured in that transform's
    /// space. Works whatever rotation, scale or offset the sprite has relative to the measuring transform.
    /// </summary>
    /// <param name="spriteRenderer">The renderer drawing the sprite to measure.</param>
    /// <param name="measureSpace">The transform whose local +X is the measuring direction, such as an aim rig.</param>
    /// <returns>The furthest local X reached, or zero when the renderer, sprite or transform is missing.</returns>
    public static float GetFurthestExtentAlongLocalX(SpriteRenderer spriteRenderer, Transform measureSpace)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || measureSpace == null)
        {
            return 0f;
        }

        List<Vector2> outlinePoints = GetOutlinePoints(spriteRenderer.sprite);
        if (outlinePoints.Count == 0)
        {
            return 0f;
        }

        Transform spriteTransform = spriteRenderer.transform;
        float furthestExtent = float.MinValue;
        foreach (Vector2 outlinePoint in outlinePoints)
        {
            Vector3 outlinePointWorld = spriteTransform.TransformPoint(outlinePoint);
            furthestExtent = Mathf.Max(furthestExtent, measureSpace.InverseTransformPoint(outlinePointWorld).x);
        }

        return furthestExtent;
    }

    // Collects the sprite's opaque outline in its local space: its physics shape (pixel exact), else its mesh outline
    // (which can be extruded slightly past the opaque pixels). The returned list is shared and reused between calls.
    private static List<Vector2> GetOutlinePoints(Sprite sprite)
    {
        spriteOutlineBuffer.Clear();
        for (int shapeIndex = 0; shapeIndex < sprite.GetPhysicsShapeCount(); shapeIndex++)
        {
            spritePhysicsShapeBuffer.Clear();
            sprite.GetPhysicsShape(shapeIndex, spritePhysicsShapeBuffer);
            spriteOutlineBuffer.AddRange(spritePhysicsShapeBuffer);
        }

        if (spriteOutlineBuffer.Count == 0 && sprite.vertices != null)
        {
            spriteOutlineBuffer.AddRange(sprite.vertices);
        }

        return spriteOutlineBuffer;
    }

    /// <summary>Rebuilds a polygon collider's paths from a sprite's physics shape.</summary>
    /// <param name="polygonCollider">The collider to rebuild.</param>
    /// <param name="sprite">The sprite whose physics shape is copied.</param>
    /// <returns>True when the collider was rebuilt, false when either argument is missing or the sprite has no physics shape.</returns>
    public static bool ApplyPhysicsShape(PolygonCollider2D polygonCollider, Sprite sprite)
    {
        if (polygonCollider == null || sprite == null)
        {
            return false;
        }

        int physicsShapeCount = sprite.GetPhysicsShapeCount();
        if (physicsShapeCount == 0)
        {
            return false;
        }

        bool colliderShapeMatches = polygonCollider.pathCount == physicsShapeCount && polygonCollider.useDelaunayMesh;
        for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
        {
            spritePhysicsShapeBuffer.Clear();
            sprite.GetPhysicsShape(shapeIndex, spritePhysicsShapeBuffer);
            if (!colliderShapeMatches)
            {
                continue;
            }

            Vector2[] existingPath = polygonCollider.GetPath(shapeIndex);
            if (existingPath.Length != spritePhysicsShapeBuffer.Count)
            {
                colliderShapeMatches = false;
                continue;
            }

            for (int pointIndex = 0; pointIndex < existingPath.Length; pointIndex++)
            {
                if (existingPath[pointIndex] != spritePhysicsShapeBuffer[pointIndex])
                {
                    colliderShapeMatches = false;
                    break;
                }
            }
        }

        if (colliderShapeMatches)
        {
            return true;
        }

        polygonCollider.pathCount = physicsShapeCount;
        for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
        {
            spritePhysicsShapeBuffer.Clear();
            sprite.GetPhysicsShape(shapeIndex, spritePhysicsShapeBuffer);
            polygonCollider.SetPath(shapeIndex, spritePhysicsShapeBuffer);
        }

        polygonCollider.useDelaunayMesh = true;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(polygonCollider);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(polygonCollider);
        }
#endif
        return true;
    }
}
