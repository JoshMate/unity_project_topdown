using System;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

/// <summary>Applies the project sprite import preset to images imported beneath Assets/Art.</summary>
public sealed class JMSpriteImportPostprocessor : AssetPostprocessor
{
    [Header("Constants Private")]
    private const string spriteArtFolder = "Assets/Art/";
    private const string spritePresetPath = "Assets/Engine/Editor/JMSpriteImport.preset";
    // Point markers are mostly hollow frames; a full-rect mesh makes the whole square clickable in the Scene view
    private const string spritePointArtFolder = "Assets/Art/Points/";

    [Header("Privates")]
    private static Preset cachedSpritePreset;
    private static bool hasAttemptedPresetLoad;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(spriteArtFolder, StringComparison.Ordinal))
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
        importSettings.spriteMeshType = assetPath.StartsWith(spritePointArtFolder, StringComparison.Ordinal)
            ? SpriteMeshType.FullRect
            : SpriteMeshType.Tight;
        importSettings.spriteGenerateFallbackPhysicsShape = true;
        textureImporter.SetTextureSettings(importSettings);
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
