using UnityEngine;

public class F_PlayerHeldWeapon : MonoBehaviour
{
    [Header("Art")]
    public Sprite weaponSprite;

    [Header("Object Refs")]
    public GameObject projectileObject;
    public GameObject projectileImpactObject;
    public F_playerHeldWeaponIndividual heldWeaponCurrentlySelected;
    public F_playerHeldWeaponIndividual heldWeaponGunBig;
    public F_playerHeldWeaponIndividual heldWeaponGunSmall;
    public F_playerHeldWeaponIndividual heldWeaponMeleeBig;
    public F_playerHeldWeaponIndividual heldWeaponMeleeSmall;

    public float projectileSpeed;

    private F_GUI_Inventory_Slot selectedWeaponSlot;
    private bool hasExplicitWeaponSelection;

    /// <summary>Returns whether the selected slot currently contains a firearm that can fire.</summary>
    public bool CanFireCurrentWeapon
    {
        get
        {
            if (!hasExplicitWeaponSelection)
            {
                return heldWeaponCurrentlySelected != null;
            }

            if (selectedWeaponSlot == null || selectedWeaponSlot.slotItemObj == null)
            {
                return false;
            }

            enumItemType selectedItemType = selectedWeaponSlot.slotItemObj.itemType;
            return selectedItemType == enumItemType.WeaponGunOneHanded ||
                   selectedItemType == enumItemType.WeaponGunTwoHanded;
        }
    }

    private void Start()
    {
        UpdateHeldWeaponSprite();
    }

    private void Update()
    {
        UpdateHeldWeaponSprite();
    }

    /// <summary>Selects a weapon inventory slot and updates the visible held weapon.</summary>
    /// <param name="weaponSlot">The weapon slot to activate.</param>
    public void SelectWeaponSlot(F_GUI_Inventory_Slot weaponSlot)
    {
        selectedWeaponSlot = weaponSlot;
        hasExplicitWeaponSelection = true;
        UpdateHeldWeaponSprite();
    }

    /// <summary>Fires the selected firearm when the current slot contains a gun.</summary>
    public void FireWeapon()
    {
        if (!CanFireCurrentWeapon || projectileObject == null || heldWeaponCurrentlySelected == null ||
            heldWeaponCurrentlySelected.firePosition == null)
        {
            return;
        }

        GameObject projectile = Instantiate(
            projectileObject,
            heldWeaponCurrentlySelected.firePosition.position,
            heldWeaponCurrentlySelected.firePosition.rotation);
        Rigidbody2D projectileBody = projectile.GetComponent<Rigidbody2D>();
        if (projectileBody != null)
        {
            projectileBody.AddForce(
                heldWeaponCurrentlySelected.firePosition.right * projectileSpeed,
                ForceMode2D.Impulse);
        }

        F_Effects_Projectile projectileEffects = projectile.GetComponent<F_Effects_Projectile>();
        if (projectileEffects != null)
        {
            projectileEffects.projectileImpactObject = projectileImpactObject;
        }
    }

    /// <summary>Refreshes the held weapon visual from the selected inventory item's type.</summary>
    public void UpdateHeldWeaponSprite()
    {
        F_playerHeldWeaponIndividual weaponToShow = heldWeaponGunBig;
        if (hasExplicitWeaponSelection)
        {
            weaponToShow = GetHeldWeaponForSelectedItem();
        }

        heldWeaponCurrentlySelected = weaponToShow;
        SetWeaponVisible(heldWeaponGunBig, weaponToShow == heldWeaponGunBig);
        SetWeaponVisible(heldWeaponGunSmall, weaponToShow == heldWeaponGunSmall);
        SetWeaponVisible(heldWeaponMeleeBig, weaponToShow == heldWeaponMeleeBig);
        SetWeaponVisible(heldWeaponMeleeSmall, weaponToShow == heldWeaponMeleeSmall);
    }

    private F_playerHeldWeaponIndividual GetHeldWeaponForSelectedItem()
    {
        if (selectedWeaponSlot == null || selectedWeaponSlot.slotItemObj == null)
        {
            return null;
        }

        switch (selectedWeaponSlot.slotItemObj.itemType)
        {
            case enumItemType.WeaponGunTwoHanded:
                return heldWeaponGunBig;
            case enumItemType.WeaponGunOneHanded:
                return heldWeaponGunSmall;
            case enumItemType.WeaponMeleeTwoHanded:
                return heldWeaponMeleeBig;
            case enumItemType.WeaponMeleeOneHanded:
                return heldWeaponMeleeSmall;
            default:
                return null;
        }
    }

    private static void SetWeaponVisible(F_playerHeldWeaponIndividual heldWeapon, bool isVisible)
    {
        if (heldWeapon != null && heldWeapon.spriteRenderer != null)
        {
            heldWeapon.spriteRenderer.enabled = isVisible;
        }
    }
}
