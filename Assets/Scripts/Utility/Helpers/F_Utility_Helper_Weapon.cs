/// <summary>Shared weapon classification rules, so item type checks live in one place.</summary>
public static class F_Utility_Helper_Weapon
{
    /// <summary>Returns whether the item type is a firearm type.</summary>
    public static bool IsFirearmItemType(enumItemType itemType)
    {
        return itemType == enumItemType.WeaponGunOneHanded ||
               itemType == enumItemType.WeaponGunTwoHanded;
    }

    /// <summary>Returns whether the item type is a melee weapon type.</summary>
    public static bool IsMeleeItemType(enumItemType itemType)
    {
        return itemType == enumItemType.WeaponMeleeOneHanded ||
               itemType == enumItemType.WeaponMeleeTwoHanded;
    }
}
