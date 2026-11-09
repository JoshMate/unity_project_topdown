using System.Collections.Generic;
using UnityEngine;

public static class F_Utility_Helper_Sprite
{
    [Header("Privates")]
    private static readonly List<Vector2> spritePhysicsShapeBuffer = new List<Vector2>();

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
