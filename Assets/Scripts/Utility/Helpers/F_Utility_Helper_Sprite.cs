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

        polygonCollider.pathCount = physicsShapeCount;
        for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
        {
            spritePhysicsShapeBuffer.Clear();
            sprite.GetPhysicsShape(shapeIndex, spritePhysicsShapeBuffer);
            polygonCollider.SetPath(shapeIndex, spritePhysicsShapeBuffer);
        }

        polygonCollider.useDelaunayMesh = true;
        return true;
    }
}
