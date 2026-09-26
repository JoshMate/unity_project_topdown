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

public class F_Item : MonoBehaviour
{
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
    public int itemCountMax = 1;

    [Header("Item Flags")]
    public bool isInInventorySlot = false;

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
            new F_ItemTooltipDetail("Max Stack Size", itemCountMax.ToString(CultureInfo.InvariantCulture))
        };
    }

    /// <summary>Moves a picked-up item off the ground while it is in the inventory.</summary>
    public void MoveItemToInventory()
    {
        isInInventorySlot = true;
        transform.position = new Vector3(-1000, -1000, -1000);
    }

    /// <summary>Marks the item as no longer being stored in an inventory slot.</summary>
    public void MoveItemOutOfInventory()
    {
        isInInventorySlot = false;
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
