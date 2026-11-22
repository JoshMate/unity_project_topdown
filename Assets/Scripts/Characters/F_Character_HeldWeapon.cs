using System.Collections;
using UnityEngine;

/// <summary>
/// Shared held weapon logic for any character (player, NPCs): shows the held weapon, fires firearms (bursts, spread,
/// projectiles), swings melee weapons and starts reloads. Place on a child of the character so its local +X follows
/// the aim direction. Subclasses only decide which weapon is held; by default it is whatever SelectWeapon was given.
/// </summary>
public class F_Character_HeldWeapon : MonoBehaviour
{
    [Header("Object Refs")]
    public GameObject projectileObject;
    public GameObject projectileImpactObject;
    public F_playerHeldWeaponIndividual heldWeaponCurrentlySelected;
    public F_playerHeldWeaponIndividual heldWeaponGunBig;
    public F_playerHeldWeaponIndividual heldWeaponGunSmall;
    // Draws and swings the selected melee weapon's sprite, which is also its hitbox
    public F_Character_MeleeHitbox heldWeaponMeleeHitbox;

    [Header("Constants Private")]
    private const int minimumProjectilesPerShot = 1;
    private const int minimumBurstShotCount = 1;
    private const float minimumBurstDelay = 0f;
    // How many sorting orders below the holder's body sprite every held weapon sprite is drawn
    private const int heldWeaponSortingOrderBelowHolder = 1;

    [Header("Privates")]
    private bool isBurstFiring;
    private F_Item_Weapon heldWeaponItemTracked;
    private F_Item_Weapon heldWeaponSelectedItem;
    private F_Character heldWeaponHolder;
    // The projectile prefab's Rigidbody2D, cached for launch speed calculations
    private Rigidbody2D heldWeaponProjectileBody;

    /// <summary>Returns whether the held weapon can be used right now.</summary>
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

    /// <summary>Whether the held weapon should keep being used while the primary action remains held.</summary>
    public bool IsCurrentWeaponContinuous
    {
        get
        {
            F_Item_Weapon weaponItem = GetSelectedWeaponItem();
            return weaponItem != null && weaponItem.IsUseContinuous;
        }
    }

    /// <summary>The held weapon item, or null when no weapon is held.</summary>
    public F_Item_Weapon SelectedWeaponItem => GetSelectedWeaponItem();

    /// <summary>Where projectiles from the held firearm spawn, or null when it has no fire position.</summary>
    public Transform CurrentFirePosition => heldWeaponCurrentlySelected != null ? heldWeaponCurrentlySelected.firePosition : null;

    /// <summary>
    /// Speed in units per second the firearm's projectiles leave the barrel at: its launch impulse divided by the
    /// projectile prefab's Rigidbody2D mass. Returns 0 when the projectile has no Rigidbody2D, as it then never moves.
    /// </summary>
    /// <param name="firearmItem">The firearm whose projectile speed is wanted.</param>
    public float GetProjectileLaunchSpeed(F_Item_Weapon_Firearm firearmItem)
    {
        if (firearmItem == null || projectileObject == null)
        {
            return 0f;
        }

        if (heldWeaponProjectileBody == null)
        {
            heldWeaponProjectileBody = projectileObject.GetComponent<Rigidbody2D>();
        }

        if (heldWeaponProjectileBody == null || heldWeaponProjectileBody.mass <= 0f)
        {
            return 0f;
        }

        return firearmItem.weaponProjectileSpeed / heldWeaponProjectileBody.mass;
    }

    /// <summary>How held weapon sounds play: spatial by default so other characters' weapons fade with distance.</summary>
    protected virtual EnumSoundType HeldWeaponSoundType => EnumSoundType.Spatial;

    protected virtual void Start()
    {
        ApplyHolderSorting();
        UpdateHeldWeaponSprite();
    }

    protected virtual void Update()
    {
        SyncHeldWeaponItem();
        UpdateHeldWeaponSprite();
    }

    /// <summary>Holds the given weapon, starting its deploy delay when it changes. Pass null to hold nothing.</summary>
    /// <param name="weaponItem">The weapon to hold.</param>
    public void SelectWeapon(F_Item_Weapon weaponItem)
    {
        F_Item_Weapon previousWeaponItem = GetSelectedWeaponItem();
        heldWeaponSelectedItem = weaponItem;
        OnSelectedWeaponChanged(previousWeaponItem);
    }

    /// <summary>Starts the newly held weapon's deploy and refreshes the held state and visuals. Call after changing the held weapon.</summary>
    /// <param name="previousWeaponItem">The weapon held before the change.</param>
    protected void OnSelectedWeaponChanged(F_Item_Weapon previousWeaponItem)
    {
        F_Item_Weapon newWeaponItem = GetSelectedWeaponItem();
        if (newWeaponItem != null && newWeaponItem != previousWeaponItem)
        {
            newWeaponItem.BeginDeploy();
        }

        SyncHeldWeaponItem();
        UpdateHeldWeaponSprite();
    }

    /// <summary>Plays the dry-fire click when the held weapon cannot be used (broken, out of ammo, or not enough stamina).</summary>
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

    /// <summary>Uses the held weapon: firearms fire, melee weapons swing.</summary>
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

    /// <summary>Starts a reload cycle on the held firearm, if any.</summary>
    /// <param name="ammoReserve">Where the reload draws ammo from.</param>
    public void ReloadCurrentWeapon(IF_Item_Weapon_AmmoReserve ammoReserve)
    {
        if (GetSelectedWeaponItem() is F_Item_Weapon_Firearm firearmItem)
        {
            firearmItem.StartReload(ammoReserve);
        }
    }

    /// <summary>Returns the held weapon item; override to read it from somewhere else, such as an inventory slot.</summary>
    protected virtual F_Item_Weapon GetSelectedWeaponItem()
    {
        return heldWeaponSelectedItem;
    }

    /// <summary>The character holding the weapon, used for stamina costs and to avoid hitting themselves.</summary>
    protected F_Character GetWeaponHolder()
    {
        if (heldWeaponHolder == null)
        {
            heldWeaponHolder = GetComponentInParent<F_Character>();
        }

        return heldWeaponHolder;
    }

    // Draws every held weapon sprite just underneath the holder's body, whatever sorting layer the holder uses.
    private void ApplyHolderSorting()
    {
        F_Character weaponHolder = GetWeaponHolder();
        if (weaponHolder == null || !TryGetHolderBodySorting(weaponHolder, out int bodySortingLayerId, out int bodySortingOrder))
        {
            return;
        }

        int weaponSortingOrder = bodySortingOrder - heldWeaponSortingOrderBelowHolder;
        foreach (SpriteRenderer weaponRenderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            weaponRenderer.sortingLayerID = bodySortingLayerId;
            weaponRenderer.sortingOrder = weaponSortingOrder;
        }
    }

    // Finds the lowest drawn sprite on the holder outside this rig, which is the body (overlays such as health bars draw above it).
    private bool TryGetHolderBodySorting(F_Character weaponHolder, out int bodySortingLayerId, out int bodySortingOrder)
    {
        bodySortingLayerId = 0;
        bodySortingOrder = 0;
        int lowestLayerValue = 0;
        bool hasFoundBody = false;

        foreach (SpriteRenderer holderRenderer in weaponHolder.GetComponentsInChildren<SpriteRenderer>())
        {
            if (holderRenderer.transform.IsChildOf(transform))
            {
                continue;
            }

            int layerValue = SortingLayer.GetLayerValueFromID(holderRenderer.sortingLayerID);
            bool isLower = layerValue < lowestLayerValue || (layerValue == lowestLayerValue && holderRenderer.sortingOrder < bodySortingOrder);
            if (!hasFoundBody || isLower)
            {
                lowestLayerValue = layerValue;
                bodySortingLayerId = holderRenderer.sortingLayerID;
                bodySortingOrder = holderRenderer.sortingOrder;
                hasFoundBody = true;
            }
        }

        return hasFoundBody;
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
            F_Character weaponHolder = GetWeaponHolder();
            Rigidbody2D holderRigidbody = weaponHolder != null ? weaponHolder.GetComponent<Rigidbody2D>() : null;
            heldWeaponItemTracked.SetHeld(true, holderRigidbody, HeldWeaponSoundType);
        }
    }

    // Starts the swing on the weapon (stamina, durability, sound), then physically swings the held sprite hitbox.
    private void SwingMeleeWeapon(F_Item_Weapon_Melee meleeItem)
    {
        if (heldWeaponMeleeHitbox == null)
        {
            return;
        }

        F_Character weaponHolder = GetWeaponHolder();
        if (meleeItem.TrySwing(weaponHolder, heldWeaponMeleeHitbox.transform.position))
        {
            heldWeaponMeleeHitbox.BeginSwing(weaponHolder);
        }
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

    /// <summary>Refreshes the held weapon visual: firearms show their held arms, melee weapons show their own sprite in front of the holder.</summary>
    public void UpdateHeldWeaponSprite()
    {
        F_playerHeldWeaponIndividual weaponToShow = GetHeldWeaponForSelectedItem();

        heldWeaponCurrentlySelected = weaponToShow;
        SetWeaponVisible(heldWeaponGunBig, weaponToShow == heldWeaponGunBig);
        SetWeaponVisible(heldWeaponGunSmall, weaponToShow == heldWeaponGunSmall);

        if (heldWeaponMeleeHitbox != null)
        {
            heldWeaponMeleeHitbox.SetHeldWeapon(GetSelectedWeaponItem() as F_Item_Weapon_Melee);
        }
    }

    private F_playerHeldWeaponIndividual GetHeldWeaponForSelectedItem()
    {
        F_Item_Weapon weaponItem = GetSelectedWeaponItem();
        if (weaponItem == null)
        {
            return null;
        }

        switch (weaponItem.itemType)
        {
            case enumItemType.WeaponGunTwoHanded:
                return heldWeaponGunBig;
            case enumItemType.WeaponGunOneHanded:
                return heldWeaponGunSmall;
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
