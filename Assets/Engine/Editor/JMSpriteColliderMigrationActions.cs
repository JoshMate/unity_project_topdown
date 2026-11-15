using System.Collections.Generic;
using Bezi;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Provides editor migration actions for sprite-driven 2D colliders.</summary>
public static class JMSpriteColliderMigrationActions
{
    [Header("Constants Private")]
    private const string cfgSpriteArtFolder = "Assets/Art";
    private const float cfgSpritePhysicsTessellationDetail = 1f;
    private const byte cfgSpritePhysicsAlphaTolerance = 200;

    /// <summary>Regenerates alpha-derived Sprite Physics Shapes for Art assets, then refreshes existing object prefab polygon colliders.</summary>
    [BeziAction("Generate precise alpha-derived Physics Shapes for every Sprite under Assets/Art, then refresh existing PolygonCollider2D paths in Assets/Objects prefabs from those shapes.")]
    public static string RegenerateSpritePhysicsShapesAndRefreshExistingSpritePolygonColliders()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { cfgSpriteArtFolder });
        int reimportedTextureCount = 0;
        int generatedPhysicsOutlineCount = 0;
        int configuredSpriteCount = 0;
        int unconfiguredSpriteCount = 0;

        foreach (string textureGuid in textureGuids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter textureImporter)
            {
                continue;
            }

            textureImporter.SaveAndReimport();
            reimportedTextureCount++;
            if (EnsureAlphaPhysicsShapesForTexture(texturePath, true))
            {
                generatedPhysicsOutlineCount++;
            }

            CountPhysicsTessellationDetails(texturePath, ref configuredSpriteCount, ref unconfiguredSpriteCount);
        }

        int refreshedColliderCount = RefreshExistingSpritePolygonColliders();
        return $"Reimported {reimportedTextureCount} Art textures; generated alpha-based Physics outlines on {generatedPhysicsOutlineCount} textures; {configuredSpriteCount} sprites use physics tessellation detail 1.0; {unconfiguredSpriteCount} sprites do not; refreshed {refreshedColliderCount} prefab polygon colliders.";
    }

    internal static bool EnsureAlphaPhysicsShapesForTexture(string texturePath, bool overwriteExisting = false)
    {
        if (AssetImporter.GetAtPath(texturePath) is not TextureImporter textureImporter ||
            textureImporter.textureType != TextureImporterType.Sprite)
        {
            return false;
        }

        SpriteDataProviderFactories spriteDataProviderFactories = new SpriteDataProviderFactories();
        spriteDataProviderFactories.Init();
        ISpriteEditorDataProvider spriteEditorDataProvider =
            spriteDataProviderFactories.GetSpriteEditorDataProviderFromObject(textureImporter);
        if (spriteEditorDataProvider == null)
        {
            return false;
        }

        spriteEditorDataProvider.InitSpriteEditorDataProvider();
        ISpritePhysicsOutlineDataProvider physicsOutlineDataProvider =
            spriteEditorDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        if (physicsOutlineDataProvider == null)
        {
            return false;
        }

        SpriteRect[] spriteRects = spriteEditorDataProvider.GetSpriteRects();
        UnityEngine.Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        int generatedOutlineCount = 0;

        foreach (SpriteRect spriteRect in spriteRects)
        {
            if (!overwriteExisting)
            {
                List<Vector2[]> existingOutlines = physicsOutlineDataProvider.GetOutlines(spriteRect.spriteID);
                if (existingOutlines != null && existingOutlines.Count > 0)
                {
                    continue;
                }
            }

            Sprite importedSprite = null;
            foreach (UnityEngine.Object importedAsset in importedAssets)
            {
                if (importedAsset is Sprite candidateSprite && candidateSprite.name == spriteRect.name)
                {
                    importedSprite = candidateSprite;
                    break;
                }
            }

            if (importedSprite == null)
            {
                continue;
            }

            GameObject temporaryColliderObject = new GameObject("Sprite Alpha Physics Shape Generator")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                PolygonCollider2D temporaryCollider = temporaryColliderObject.AddComponent<PolygonCollider2D>();
                bool generatedFromAlpha = temporaryCollider.CreateFromSprite(
                    importedSprite,
                    cfgSpritePhysicsTessellationDetail,
                    cfgSpritePhysicsAlphaTolerance,
                    true,
                    false);
                if (!generatedFromAlpha || temporaryCollider.pathCount == 0)
                {
                    continue;
                }

                List<Vector2[]> pixelSpaceOutlines = new List<Vector2[]>(temporaryCollider.pathCount);
                for (int pathIndex = 0; pathIndex < temporaryCollider.pathCount; pathIndex++)
                {
                    Vector2[] localPath = temporaryCollider.GetPath(pathIndex);
                    Vector2[] pixelPath = new Vector2[localPath.Length];
                    for (int pointIndex = 0; pointIndex < localPath.Length; pointIndex++)
                    {
                        pixelPath[pointIndex] = localPath[pointIndex] * importedSprite.pixelsPerUnit;
                    }

                    pixelSpaceOutlines.Add(pixelPath);
                }

                physicsOutlineDataProvider.SetOutlines(spriteRect.spriteID, pixelSpaceOutlines);
                physicsOutlineDataProvider.SetTessellationDetail(
                    spriteRect.spriteID, cfgSpritePhysicsTessellationDetail);
                generatedOutlineCount++;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryColliderObject);
            }
        }

        if (generatedOutlineCount == 0)
        {
            return false;
        }

        spriteEditorDataProvider.Apply();
        textureImporter.SaveAndReimport();
        return true;
    }

    private static void CountPhysicsTessellationDetails(string texturePath, ref int configuredSpriteCount, ref int unconfiguredSpriteCount)
    {
        if (AssetImporter.GetAtPath(texturePath) is not TextureImporter textureImporter)
        {
            return;
        }

        SpriteDataProviderFactories spriteDataProviderFactories = new SpriteDataProviderFactories();
        spriteDataProviderFactories.Init();
        ISpriteEditorDataProvider spriteEditorDataProvider =
            spriteDataProviderFactories.GetSpriteEditorDataProviderFromObject(textureImporter);
        if (spriteEditorDataProvider == null)
        {
            return;
        }

        spriteEditorDataProvider.InitSpriteEditorDataProvider();
        ISpritePhysicsOutlineDataProvider physicsOutlineDataProvider =
            spriteEditorDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        if (physicsOutlineDataProvider == null)
        {
            return;
        }

        foreach (SpriteRect spriteRect in spriteEditorDataProvider.GetSpriteRects())
        {
            if (Mathf.Approximately(
                    physicsOutlineDataProvider.GetTessellationDetail(spriteRect.spriteID), cfgSpritePhysicsTessellationDetail))
            {
                configuredSpriteCount++;
            }
            else
            {
                unconfiguredSpriteCount++;
            }
        }
    }

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
