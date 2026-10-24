using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class F_World : F_Ent
{
    [Header("World Features")]
    // Outline and collision shape; the sprite for each shape is set in OB_Config_WorldShapes
    public enumWorldShape worldShape = enumWorldShape.WallSquare;
    // When enabled, health and resistances come from the material's entry in OB_Config_EntMaterials; disable to use this object's own values
    public bool worldUseMaterialStats = true;

    protected override void Awake()
    {
        if (worldUseMaterialStats)
        {
            ApplyMaterialStats();
        }
        base.Awake();
        ApplyWorldAppearance();
    }

    /// <summary>Copies the default max health and resistances configured for this object's material, and starts it at full health.</summary>
    public void ApplyMaterialStats()
    {
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(entFeatureMaterial);
        if (settings == null)
        {
            return;
        }

        entStatHealthMax = settings.materialHealthMax;
        entStatHealth = settings.materialHealthMax;
        entResistanceMelee = settings.materialResistanceMelee;
        entResistanceBullet = settings.materialResistanceBullet;
        entResistanceEnergy = settings.materialResistanceEnergy;
        entResistanceFire = settings.materialResistanceFire;
        entResistanceExplosive = settings.materialResistanceExplosive;
        entResistanceToxic = settings.materialResistanceToxic;
    }

    // Keeps the editor preview in sync when the shape or material is changed in the Inspector
    private void OnValidate()
    {
        ApplyWorldAppearance();
    }

    /// <summary>Applies the configured shape (sprite outline and collider) and then the material's world texture.</summary>
    public void ApplyWorldAppearance()
    {
        ApplyWorldShape();
        F_Utility_Helper_EntMaterial.ApplyWorldTexture(this);
    }

    // Swaps in the shape's sprite and rebuilds the collider from its physics shape.
    // In edit mode the collider is only rebuilt when the shape changes, so opening a scene does not dirty it.
    private void ApplyWorldShape()
    {
        F_World_ShapeSettings settings = F_Utility_Config_WorldShapes.GetSettings(worldShape);
        if (settings == null || settings.shapeSprite == null)
        {
            return;
        }

        SpriteRenderer worldSpriteRenderer = GetComponent<SpriteRenderer>();
        PolygonCollider2D worldPolygonCollider = GetComponent<PolygonCollider2D>();
        bool shapeChanged = worldSpriteRenderer.sprite != settings.shapeSprite;
        if (shapeChanged)
        {
            worldSpriteRenderer.sprite = settings.shapeSprite;
        }

        if (shapeChanged || Application.isPlaying)
        {
            F_Utility_Helper_Sprite.ApplyPhysicsShape(worldPolygonCollider, settings.shapeSprite);
        }

#if UNITY_EDITOR
        if (shapeChanged && !Application.isPlaying)
        {
            RecordEditorChange(worldSpriteRenderer);
            RecordEditorChange(worldPolygonCollider);
        }
#endif
    }

#if UNITY_EDITOR
    // Marks an edit-mode change so it is saved with the scene or prefab
    private static void RecordEditorChange(Object changedObject)
    {
        UnityEditor.EditorUtility.SetDirty(changedObject);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(changedObject);
    }

    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditorRefresh()
    {
        F_Utility_Config_EntMaterials.OnConfigChanged += RefreshAllWorldAppearances;
        F_Utility_Config_WorldShapes.OnConfigChanged += RefreshAllWorldAppearances;
    }

    // Re-applies shape and texture to every world object, so config edits show immediately
    private static void RefreshAllWorldAppearances()
    {
        F_Utility_Helper_EntMaterial.ClearRuntimeMaterials();
        foreach (F_World world in FindObjectsByType<F_World>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            world.ApplyWorldAppearance();
        }
    }
#endif
}
