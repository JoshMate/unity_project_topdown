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
            textureImporter.spriteImportMode == SpriteImportMode.Multiple)
        {
            // Preserve each authored slice and pivot while normalizing the shared importer settings.
            ApplySpritePresetSettingsPreservingSpriteSheet(textureImporter);
            ApplyTightMeshAndPhysicsShape(textureImporter);
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

        ApplyTightMeshAndPhysicsShape(textureImporter);
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

    private static void ApplyTightMeshAndPhysicsShape(TextureImporter textureImporter)
    {
        TextureImporterSettings importSettings = new TextureImporterSettings();
        textureImporter.ReadTextureSettings(importSettings);
        importSettings.spriteMeshType = SpriteMeshType.Tight;
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
