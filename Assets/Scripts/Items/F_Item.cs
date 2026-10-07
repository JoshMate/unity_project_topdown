using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public struct F_ItemTooltipDetail
{
    public string Label { get; }
    public string Value { get; }

    public F_ItemTooltipDetail(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class F_Item : MonoBehaviour
{
    [Header("Constants Private")]
    private const string defaultItemSpriteAssetPath = "Assets/Art/Logic/SP_Sprite_Default.png";
    private const string defaultPickupSoundAssetPath = "Assets/Sound/Interface/SD_Interface_PickupItem.ogg";
    private const string defaultDropSoundAssetPath = "Assets/Sound/Interface/SD_Interface_JUSP_Slide.ogg";

    [Header("Object Refs")]
    public SpriteRenderer itemSpriteRenderer;

    [Header("Item Information")]
    public string itemName = "<Item Name>";
    public string itemDescription = "<Item Description>";
    public enumItemType itemType = enumItemType.MiscJunk;
    public enumItemRarity itemRarity = enumItemRarity.Rarity00Junk;
    public float itemWeight = 1.0f;
    public int itemValue = 1;
    public int itemCount = 1;
    public enumItemMaxCount itemCountMax = enumItemMaxCount.CountMaxSingle;

    [Header("Item Art")]
    public Sprite itemSprite;
    public AudioClip itemSoundPickup;
    public AudioClip itemSoundDrop;

    [Header("Privates")]
    private bool isInInventorySlot = false;

    /// <summary>Whether the item is currently stored in an inventory slot (managed by the inventory code).</summary>
    public bool IsInInventorySlot => isInInventorySlot;

    /// <summary>The maximum stack size as an integer, read from the item's max count enum.</summary>
    public int ItemCountMaxValue => (int)itemCountMax;

    private void OnEnable()
    {
        EnsureDefaultItemAssets();
        EnsureSpriteRendererReference();
        ApplyItemSpriteInEditorOrRuntime();
    }

    private void OnValidate()
    {
        EnsureDefaultItemAssets();
        EnsureSpriteRendererReference();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= ApplyItemSpriteAfterValidation;
        UnityEditor.EditorApplication.delayCall += ApplyItemSpriteAfterValidation;
#else
        ApplyItemSprite();
#endif
    }

#if UNITY_EDITOR
    private void ApplyItemSpriteAfterValidation()
    {
        if (this == null)
        {
            return;
        }

        EnsureSpriteRendererReference();
        ApplyItemSprite();
    }
#endif

    private void ApplyItemSpriteInEditorOrRuntime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= ApplyItemSpriteAfterValidation;
            UnityEditor.EditorApplication.delayCall += ApplyItemSpriteAfterValidation;
            return;
        }
#endif
        ApplyItemSprite();
    }

    /// <summary>Assigns default assets to any empty item references; subclasses extend this with their own defaults.</summary>
    protected virtual void EnsureDefaultItemAssets()
    {
#if UNITY_EDITOR
        if (itemSprite == null)
        {
            itemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(defaultItemSpriteAssetPath);
        }

        if (itemSoundPickup == null)
        {
            itemSoundPickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(defaultPickupSoundAssetPath);
        }

        if (itemSoundDrop == null)
        {
            itemSoundDrop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(defaultDropSoundAssetPath);
        }
#endif
    }

    private void EnsureSpriteRendererReference()
    {
        if (itemSpriteRenderer == null)
        {
            itemSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void ApplyItemSprite()
    {
        if (itemSpriteRenderer != null)
        {
            itemSpriteRenderer.sprite = itemSprite;
        }

        PolygonCollider2D itemPolygonCollider = GetComponent<PolygonCollider2D>();
        if (itemPolygonCollider == null)
        {
            return;
        }

        itemPolygonCollider.isTrigger = true;
        if (itemSprite == null)
        {
            return;
        }

        int physicsShapeCount = itemSprite.GetPhysicsShapeCount();
        if (physicsShapeCount == 0)
        {
            return;
        }

        itemPolygonCollider.pathCount = physicsShapeCount;
        List<Vector2> physicsShape = new List<Vector2>();
        for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
        {
            physicsShape.Clear();
            itemSprite.GetPhysicsShape(shapeIndex, physicsShape);
            itemPolygonCollider.SetPath(shapeIndex, physicsShape);
        }

        itemPolygonCollider.useDelaunayMesh = true;
    }

    /// <summary>Returns the item properties displayed by the generic item tooltip.</summary>
    public virtual List<F_ItemTooltipDetail> GetTooltipDetails()
    {
        return new List<F_ItemTooltipDetail>
        {
            new F_ItemTooltipDetail("Type", FormatEnumLabel(itemType.ToString(), false)),
            new F_ItemTooltipDetail("Rarity", FormatEnumLabel(itemRarity.ToString(), true)),
            new F_ItemTooltipDetail("Weight", itemWeight.ToString("0.##", CultureInfo.InvariantCulture)),
            new F_ItemTooltipDetail("Value", itemValue.ToString(CultureInfo.InvariantCulture)),
            new F_ItemTooltipDetail("Stack Size", itemCount.ToString(CultureInfo.InvariantCulture)),
            new F_ItemTooltipDetail("Max Stack Size", ItemCountMaxValue.ToString(CultureInfo.InvariantCulture))
        };
    }

    /// <summary>Returns the right-click context menu actions available for this item, in display order.</summary>
    /// <remarks>Override in item subclasses to add type-specific actions on top of the shared defaults.</remarks>
    public virtual List<enumItemContextAction> GetContextMenuActions()
    {
        return new List<enumItemContextAction>
        {
            enumItemContextAction.Drop
        };
    }

    /// <summary>Updates the item's inventory state and hides it while it is stored.</summary>
    internal void SetInventoryStoredState(bool isStored)
    {
        isInInventorySlot = isStored;
        if (isStored)
        {
            transform.position = new Vector3(-1000f, -1000f, -1000f);
        }
    }

    private static string FormatEnumLabel(string enumName, bool removeRarityPrefix)
    {
        string readableName = enumName;
        if (removeRarityPrefix && readableName.StartsWith("Rarity"))
        {
            int firstLetterIndex = 6;
            while (firstLetterIndex < readableName.Length && char.IsDigit(readableName[firstLetterIndex]))
            {
                firstLetterIndex++;
            }

            readableName = readableName.Substring(firstLetterIndex);
        }

        StringBuilder formattedName = new StringBuilder(readableName.Length + 8);
        for (int characterIndex = 0; characterIndex < readableName.Length; characterIndex++)
        {
            char currentCharacter = readableName[characterIndex];
            if (characterIndex > 0 && char.IsUpper(currentCharacter) && char.IsLower(readableName[characterIndex - 1]))
            {
                formattedName.Append(' ');
            }

            formattedName.Append(currentCharacter);
        }

        return formattedName.ToString();
    }
}
