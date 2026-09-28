using System.Collections.Generic;
using Bezi;
using UnityEditor;
using UnityEngine;

/// <summary>Provides editor migration actions for sprite-driven 2D colliders.</summary>
public static class JMSpriteColliderMigrationActions
{
    /// <summary>Refreshes existing sprite polygon colliders in project object prefabs from their sprites' imported physics shapes.</summary>
    [BeziAction("Refresh existing PolygonCollider2D paths in Assets/Objects prefabs from each attached SpriteRenderer sprite's imported physics shape. Preserves non-polygon colliders and does not add missing colliders.")]
    public static int RefreshExistingSpritePolygonColliders()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Objects" });
        int refreshedColliderCount = 0;

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                SpriteRenderer[] spriteRenderers = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer spriteRenderer in spriteRenderers)
                {
                    PolygonCollider2D polygonCollider = spriteRenderer.GetComponent<PolygonCollider2D>();
                    Sprite sprite = spriteRenderer.sprite;
                    if (polygonCollider == null || sprite == null)
                    {
                        continue;
                    }

                    int physicsShapeCount = sprite.GetPhysicsShapeCount();
                    if (physicsShapeCount == 0)
                    {
                        continue;
                    }

                    polygonCollider.pathCount = physicsShapeCount;
                    List<Vector2> physicsShape = new List<Vector2>();
                    for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
                    {
                        physicsShape.Clear();
                        sprite.GetPhysicsShape(shapeIndex, physicsShape);
                        polygonCollider.SetPath(shapeIndex, physicsShape);
                    }

                    polygonCollider.useDelaunayMesh = true;
                    refreshedColliderCount++;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return refreshedColliderCount;
    }
}
