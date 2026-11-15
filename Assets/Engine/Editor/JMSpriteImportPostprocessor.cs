using System;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Applies the project sprite import preset to images imported beneath Assets/Art.</summary>
public sealed class JMSpriteImportPostprocessor : AssetPostprocessor
{
    [Header("Constants Private")]
    private const string cfgSpriteArtFolder = "Assets/Art/";
    private const string spritePresetPath = "Assets/Engine/Editor/JMSpriteImport.preset";
    // Point sprites get a full-rect mesh so their whole area remains clickable in the Scene view.
    private const string cfgSpritePointArtFolder = "Assets/Art/Points/";
    private const float cfgSpriteMeshTessellationDetail = 1f;
    private const float cfgSpritePhysicsTessellationDetail = 1f;

    [Header("Privates")]
    private static Preset cachedSpritePreset;
    private static bool hasAttemptedPresetLoad;
    private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0 ||
            !assetPath.StartsWith(cfgSpriteArtFolder, StringComparison.Ordinal))
        {
            return;
        }

        string importedAssetPath = assetPath;
        EditorApplication.delayCall += () =>
            JMSpriteColliderMigrationActions.EnsureAlphaPhysicsShapesForTexture(importedAssetPath);
    }



    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(cfgSpriteArtFolder, StringComparison.Ordinal))
        {
            return;
        }

        if (assetImporter is not TextureImporter textureImporter)
        {
            return;
        }

        if (textureImporter.textureType == TextureImporterType.Sprite &&
            textureImporter.spriteImportMode == SpriteImportMode.Single)
        {
            // Preserve each authored slice and pivot while normalizing the shared importer settings.
            ApplySpritePresetSettingsPreservingSpriteSheet(textureImporter);
            ApplySpriteMeshAndPhysicsShape(textureImporter);
            return;
        }

        Preset spritePreset = GetSpritePreset();
        if (spritePreset == null)
        {
            Debug.LogError($"Could not load sprite import preset at '{spritePresetPath}'.");
            return;
        }

        if (!spritePreset.ApplyTo(textureImporter))
        {
            Debug.LogError($"Could not apply sprite import preset to '{assetPath}'.");
            return;
        }

        ApplySpriteMeshAndPhysicsShape(textureImporter);
    }

    private static void ApplySpritePresetSettingsPreservingSpriteSheet(TextureImporter textureImporter)
    {
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.sRGBTexture = true;
        textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
        textureImporter.alphaIsTransparency = true;
        textureImporter.mipmapEnabled = false;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.anisoLevel = 1;
        textureImporter.maxTextureSize = 2048;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.compressionQuality = 50;
        textureImporter.crunchedCompression = false;
        textureImporter.isReadable = false;
        textureImporter.streamingMipmaps = false;
        textureImporter.spritePixelsPerUnit = 64;
    }

    // Point sprites get a full-rect mesh so they are clickable across their whole area; all other art uses a tight mesh
    private void ApplySpriteMeshAndPhysicsShape(TextureImporter textureImporter)
    {
        TextureImporterSettings importSettings = new TextureImporterSettings();
        textureImporter.ReadTextureSettings(importSettings);
        importSettings.spriteMeshType = assetPath.StartsWith(cfgSpritePointArtFolder, StringComparison.Ordinal)
            ? SpriteMeshType.FullRect
            : SpriteMeshType.Tight;
        importSettings.spriteGenerateFallbackPhysicsShape = true;
        importSettings.spriteTessellationDetail = cfgSpriteMeshTessellationDetail;
        textureImporter.SetTextureSettings(importSettings);
        ApplySpritePhysicsTessellationDetail(textureImporter);
    }

    private static void ApplySpritePhysicsTessellationDetail(TextureImporter textureImporter)
    {
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

        bool hasUpdatedPhysicsDetail = false;
        SpriteRect[] spriteRects = spriteEditorDataProvider.GetSpriteRects();
        foreach (SpriteRect spriteRect in spriteRects)
        {
            float currentDetail = physicsOutlineDataProvider.GetTessellationDetail(spriteRect.spriteID);
            if (Mathf.Approximately(currentDetail, cfgSpritePhysicsTessellationDetail))
            {
                continue;
            }

            physicsOutlineDataProvider.SetTessellationDetail(spriteRect.spriteID, cfgSpritePhysicsTessellationDetail);
            hasUpdatedPhysicsDetail = true;
        }

        if (hasUpdatedPhysicsDetail)
        {
            spriteEditorDataProvider.Apply();
        }
    }

    private static Preset GetSpritePreset()
    {
        if (!hasAttemptedPresetLoad)
        {
            cachedSpritePreset = AssetDatabase.LoadAssetAtPath<Preset>(spritePresetPath);
            hasAttemptedPresetLoad = true;
        }

        return cachedSpritePreset;
    }
}
