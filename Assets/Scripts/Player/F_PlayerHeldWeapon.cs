using System.Collections;
using UnityEngine;

public class F_PlayerHeldWeapon : MonoBehaviour
{
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
    private bool isBurstFiring;
    private F_Item_Weapon heldWeaponItemTracked;
    private F_PlayerController heldWeaponHolderController;

    /// <summary>Returns whether the selected slot currently contains a weapon that can be used right now.</summary>
    public bool CanUseCurrentWeapon
    {
        get
        {
            if (isBurstFiring)
            {
                return false;
            }

            F_Item_Weapon weaponItem = GetSelectedWeaponItem();
            return weaponItem != null && weaponItem.CanUseWeapon(GetWeaponHolder());
        }
    }

    /// <summary>Whether the selected weapon should keep being used while the primary action remains held.</summary>
    public bool IsCurrentWeaponContinuous
    {
        get
        {
            F_Item_Weapon weaponItem = GetSelectedWeaponItem();
            return weaponItem != null && weaponItem.IsUseContinuous;
        }
    }

    private void Start()
    {
        UpdateHeldWeaponSprite();
    }

    /// <summary>Plays the dry-fire click when the selected weapon cannot be used (broken, out of ammo, or not enough stamina).</summary>
    public void TryPlayDryFireSound()
    {
        if (isBurstFiring)
        {
            return;
        }

        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (weaponItem != null && !weaponItem.IsDeploying && weaponItem.IsUseBlocked(GetWeaponHolder()))
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
            F_PlayerController holderController = GetHolderController();
            heldWeaponItemTracked.SetHeld(true, holderController != null ? holderController.rb : null);
        }
    }

    /// <summary>Selects a weapon inventory slot and updates the visible held weapon.</summary>
    /// <param name="weaponSlot">The weapon slot to activate.</param>
    public void SelectWeaponSlot(F_GUI_Inventory_Slot weaponSlot)
    {
        F_Item_Weapon previousWeaponItem = GetSelectedWeaponItem();
        selectedWeaponSlot = weaponSlot;

        F_Item_Weapon newWeaponItem = GetSelectedWeaponItem();
        if (newWeaponItem != null && newWeaponItem != previousWeaponItem)
        {
            newWeaponItem.BeginDeploy();
        }
        SyncHeldWeaponItem();

        UpdateHeldWeaponSprite();
    }

    /// <summary>Uses the selected weapon: firearms fire, melee weapons swing.</summary>
    public void UseCurrentWeapon()
    {
        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (weaponItem is F_Item_Weapon_Firearm firearmItem)
        {
            FireWeapon(firearmItem);
        }
        else if (weaponItem is F_Item_Weapon_Melee meleeItem)
        {
            SwingMeleeWeapon(meleeItem);
        }
    }

    // Swings from the held arm's fire position when one is assigned, otherwise from the holder, in the aim direction.
    private void SwingMeleeWeapon(F_Item_Weapon_Melee meleeItem)
    {
        Transform swingTransform = heldWeaponCurrentlySelected != null && heldWeaponCurrentlySelected.firePosition != null
            ? heldWeaponCurrentlySelected.firePosition
            : GetHolderTransform();
        meleeItem.TrySwing(GetWeaponHolder(), swingTransform.position, swingTransform.right);
    }

    // Fires a burst of shots from the firearm, with each shot spawning the configured number of spread projectiles.
    private void FireWeapon(F_Item_Weapon_Firearm weaponItem)
    {
        if (isBurstFiring)
        {
            return;
        }

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

    private IEnumerator FireBurstRoutine(F_Item_Weapon_Firearm weaponItem, Transform firePosition, int burstShotCount)
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

    private void SpawnProjectilesForShot(F_Item_Weapon_Firearm weaponItem, Transform firePosition)
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
                    weaponItem.weaponIsRangeReverseFallOff,
                    gameObject);
            }
        }
    }

    /// <summary>Starts a reload cycle on the currently selected firearm, if any.</summary>
    /// <param name="playerInventory">The inventory the reload should draw ammo from.</param>
    public void ReloadCurrentWeapon(F_PlayerInventory playerInventory)
    {
        if (GetSelectedWeaponItem() is F_Item_Weapon_Firearm firearmItem)
        {
            firearmItem.StartReload(playerInventory);
        }
    }

    /// <summary>The weapon item in the currently selected weapon slot, or null when no weapon is selected.</summary>
    public F_Item_Weapon SelectedWeaponItem => GetSelectedWeaponItem();

    private F_Item_Weapon GetSelectedWeaponItem()
    {
        return selectedWeaponSlot != null ? selectedWeaponSlot.slotItemObj as F_Item_Weapon : null;
    }

    private F_PlayerController GetHolderController()
    {
        if (heldWeaponHolderController == null)
        {
            heldWeaponHolderController = GetComponentInParent<F_PlayerController>();
        }

        return heldWeaponHolderController;
    }

    // The character holding the weapon, used for stamina costs and to avoid hitting themselves.
    private F_Character GetWeaponHolder()
    {
        F_PlayerController holderController = GetHolderController();
        return holderController != null ? holderController.playerStats : null;
    }

    private Transform GetHolderTransform()
    {
        F_PlayerController holderController = GetHolderController();
        return holderController != null ? holderController.transform : transform;
    }

    /// <summary>Refreshes the held weapon visual from the selected inventory item's type.</summary>
    public void UpdateHeldWeaponSprite()
    {
        F_playerHeldWeaponIndividual weaponToShow = GetHeldWeaponForSelectedItem();

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
