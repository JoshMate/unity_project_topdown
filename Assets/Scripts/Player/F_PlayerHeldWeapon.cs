using System.Collections;
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


    [Header("Constants Private")]
    private const int minimumProjectilesPerShot = 1;
    private const int minimumBurstShotCount = 1;
    private const float minimumBurstDelay = 0f;

    [Header("Privates")]
    private F_GUI_Inventory_Slot selectedWeaponSlot;
    private bool hasExplicitWeaponSelection;
    private bool isBurstFiring;
    private F_Item_Weapon heldWeaponItemTracked;

    /// <summary>Returns whether the selected slot currently contains a firearm that can fire.</summary>
    public bool CanFireCurrentWeapon
    {
        get
        {
            if (isBurstFiring)
            {
                return false;
            }

            if (!hasExplicitWeaponSelection)
            {
                return heldWeaponCurrentlySelected != null;
            }

            if (selectedWeaponSlot == null || selectedWeaponSlot.slotItemObj == null)
            {
                return false;
            }

            enumItemType selectedItemType = selectedWeaponSlot.slotItemObj.itemType;
            bool isFirearm = selectedItemType == enumItemType.WeaponGunOneHanded ||
                              selectedItemType == enumItemType.WeaponGunTwoHanded;
            if (!isFirearm)
            {
                return false;
            }

            F_Item_Weapon weaponItem = GetSelectedWeaponItem();
            return weaponItem != null && weaponItem.CanFireWeapon();
        }
    }

    /// <summary>Whether the selected weapon should keep firing while the primary action remains held.</summary>
    public bool IsCurrentWeaponFullyAutomatic
    {
        get
        {
            F_Item_Weapon weaponItem = GetSelectedWeaponItem();
            return weaponItem != null && weaponItem.weaponIsFullyAutomatic;
        }
    }

    private void Start()
    {
        UpdateHeldWeaponSprite();
    }

    /// <summary>Plays the dry-fire click when the selected firearm cannot fire because it is broken or out of ammo.</summary>
    public void TryPlayDryFireSound()
    {
        if (isBurstFiring || selectedWeaponSlot == null || selectedWeaponSlot.slotItemObj == null)
        {
            return;
        }

        enumItemType selectedItemType = selectedWeaponSlot.slotItemObj.itemType;
        bool isFirearm = selectedItemType == enumItemType.WeaponGunOneHanded ||
                          selectedItemType == enumItemType.WeaponGunTwoHanded;
        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (isFirearm && weaponItem != null && !weaponItem.IsDeploying && weaponItem.IsFireBlockedByBrokenOrEmpty())
        {
            weaponItem.PlayDryFireSound();
        }
    }

    private void Update()
    {
        SyncHeldWeaponItem();
        UpdateHeldWeaponSprite();
    }

    // Keeps weapon held state accurate so only the held weapon's delays progress
    private void SyncHeldWeaponItem()
    {
        F_Item_Weapon currentWeaponItem = GetSelectedWeaponItem();
        if (currentWeaponItem == heldWeaponItemTracked)
        {
            return;
        }

        if (heldWeaponItemTracked != null)
        {
            heldWeaponItemTracked.SetHeld(false);
        }

        heldWeaponItemTracked = currentWeaponItem;
        if (heldWeaponItemTracked != null)
        {
            heldWeaponItemTracked.SetHeld(true);
        }
    }

    /// <summary>Selects a weapon inventory slot and updates the visible held weapon.</summary>
    /// <param name="weaponSlot">The weapon slot to activate.</param>
    public void SelectWeaponSlot(F_GUI_Inventory_Slot weaponSlot)
    {
        F_Item_Weapon previousWeaponItem = GetSelectedWeaponItem();
        selectedWeaponSlot = weaponSlot;
        hasExplicitWeaponSelection = true;

        F_Item_Weapon newWeaponItem = GetSelectedWeaponItem();
        if (newWeaponItem != null && newWeaponItem != previousWeaponItem)
        {
            newWeaponItem.BeginDeploy();
        }
        SyncHeldWeaponItem();

        UpdateHeldWeaponSprite();
    }

    /// <summary>Fires a burst of shots from the selected firearm, with each shot spawning the configured number of spread projectiles.</summary>
    public void FireWeapon()
    {
        if (isBurstFiring)
        {
            return;
        }

        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (weaponItem == null || weaponItem.IsBroken() || projectileObject == null || heldWeaponCurrentlySelected == null ||
            heldWeaponCurrentlySelected.firePosition == null)
        {
            return;
        }

        Transform firePosition = heldWeaponCurrentlySelected.firePosition;
        if (!weaponItem.TryFireShot(firePosition.position))
        {
            return;
        }

        SpawnProjectilesForShot(weaponItem, firePosition);

        int burstShotCount = Mathf.Max(minimumBurstShotCount, weaponItem.weaponProjectileBurstCount);
        if (burstShotCount > minimumBurstShotCount)
        {
            isBurstFiring = true;
            StartCoroutine(FireBurstRoutine(weaponItem, firePosition, burstShotCount));
        }
        else
        {
            weaponItem.CompleteFireSequence(firePosition.position);
        }
    }

    private IEnumerator FireBurstRoutine(F_Item_Weapon weaponItem, Transform firePosition, int burstShotCount)
    {
        for (int burstShotIndex = minimumBurstShotCount; burstShotIndex < burstShotCount; burstShotIndex++)
        {
            float burstDelay = Mathf.Max(minimumBurstDelay, weaponItem.weaponProjectileBurstDelay);
            if (burstDelay > minimumBurstDelay)
            {
                yield return new WaitForSeconds(burstDelay);
            }

            if (weaponItem == null || firePosition == null || weaponItem.IsBroken() ||
                !weaponItem.TryFireBurstFollowupShot(firePosition.position))
            {
                break;
            }

            SpawnProjectilesForShot(weaponItem, firePosition);
        }

        if (weaponItem != null)
        {
            weaponItem.CompleteFireSequence(firePosition != null ? firePosition.position : transform.position);
        }

        isBurstFiring = false;
    }

    private void SpawnProjectilesForShot(F_Item_Weapon weaponItem, Transform firePosition)
    {
        int projectileShotCount = Mathf.Max(minimumProjectilesPerShot, weaponItem.weaponProjectileShotCount);
        for (int projectileIndex = 0; projectileIndex < projectileShotCount; projectileIndex++)
        {
            Vector2 spreadAimDirection = weaponItem.ApplyAccuracySpreadToDirection(firePosition.right);
            float spreadAimAngleDegrees = Mathf.Atan2(spreadAimDirection.y, spreadAimDirection.x) * Mathf.Rad2Deg;
            Quaternion projectileRotation = Quaternion.Euler(0f, 0f, spreadAimAngleDegrees);

            GameObject projectile = Instantiate(projectileObject, firePosition.position, projectileRotation);
            weaponItem.IgnoreCollisionsWithPreviouslyFiredProjectiles(projectile.GetComponentsInChildren<Collider2D>());

            Rigidbody2D projectileBody = projectile.GetComponent<Rigidbody2D>();
            if (projectileBody != null)
            {
                projectileBody.AddForce(spreadAimDirection * weaponItem.weaponProjectileSpeed, ForceMode2D.Impulse);
            }

            SpriteRenderer projectileSpriteRenderer = projectile.GetComponent<SpriteRenderer>();
            if (projectileSpriteRenderer != null && weaponItem.weaponProjectileSprite != null)
            {
                projectileSpriteRenderer.sprite = weaponItem.weaponProjectileSprite;
            }

            F_Effects_Projectile projectileEffects = projectile.GetComponent<F_Effects_Projectile>();
            if (projectileEffects != null)
            {
                projectileEffects.projectileImpactObject = projectileImpactObject;
                projectileEffects.ConfigureCombatData(
                    weaponItem.weaponDamage,
                    weaponItem.weaponDamageType,
                    weaponItem.weaponRange,
                    weaponItem.weaponRangeDamageFallOffMin,
                    weaponItem.weaponIsRangeReverseFallOff);
            }
        }
    }

    /// <summary>Starts a reload cycle on the currently selected weapon item, if any.</summary>
    /// <param name="playerInventory">The inventory the reload should draw ammo from.</param>
    public void ReloadCurrentWeapon(F_PlayerInventory playerInventory)
    {
        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (weaponItem != null)
        {
            weaponItem.StartReload(playerInventory);
        }
    }

    /// <summary>The weapon item in the currently selected weapon slot, or null when no weapon is selected.</summary>
    public F_Item_Weapon SelectedWeaponItem => GetSelectedWeaponItem();

    private F_Item_Weapon GetSelectedWeaponItem()
    {
        return selectedWeaponSlot != null ? selectedWeaponSlot.slotItemObj as F_Item_Weapon : null;
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
