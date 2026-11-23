using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Weapon that fires projectiles and uses ammo. Durability, damage and deploy timing come from <see cref="F_Item_Weapon"/>.</summary>
public class F_Item_Weapon_Firearm : F_Item_Weapon
{
    [Header("Firearm Draw Type")]
    // Which held weapon sprite is drawn in front of the character holding this firearm
    public enumWeaponHeldSprite weaponHeldSprite = enumWeaponHeldSprite.SmallArmMedium;

    [Header("Firearm Art Projectile")]
    public Sprite weaponProjectileSprite;

    [Header("Firearm Art Sounds")]
    public AudioClip weaponFireSound;
    public AudioClip weaponBoltActionSound;
    // Played when a full reload cycle begins
    public AudioClip weaponReloadStartSound;
    // Played when a full reload cycle is half way complete
    public AudioClip weaponReloadMiddleSound;
    // Played when a full reload cycle finishes
    public AudioClip weaponReloadEndSound;
    // Played once per round at the end of each round's delay when reloading one bullet at a time
    public AudioClip weaponReloadOneAtATimeSound;

    [Header("Firearm Damage Stats")]
    // How much delay there is between each shot
    public float weaponFireRateDelay = 0.5f;
    // How far the projectile moves before beining destroyed
    public float weaponRange = 10f;
    // How much the damage should be reduced to in a linear fashion (As a percent of starting damge) as the projectile travels across its range (Starting damage that grows if weaponIsRangeReverseFallOff is true)
    public float weaponRangeDamageFallOffMin = 0.30f;
    // The how fast the projectile moves
    public float weaponProjectileSpeed = 10f;
    // How many projectiles the weapon fires each time it shoots (Each projectile has random spread)
    public int weaponProjectileShotCount = 1;
    // How many times the weapon should fire when shot
    public int weaponProjectileBurstCount = 1;
    // The delay between the extra burst shots (Regular fire rate delay is then put in place after the burst is finished)
    public float weaponProjectileBurstDelay = 0.1f;

    
    [Header("Firearm Damage Features")]
    // Wether or not the weapon is automatic, continues to fire with the button held down or if one shot per press
    public bool weaponIsFullyAutomatic = false;
    // Wether or not the weapon loses or gains damage as the projectile moves through its range
    public bool weaponIsRangeReverseFallOff = false;
    // Wether or not the weapon should play a follow up sound after the shot has been fired (Plays weaponBoltActionSound and delays fire rate)
    public bool weaponIsBoltAction = false;
    // How long to wait in seconds before playing the bolt action sound after shooting, this delays the normal fire rate delay
    public float weaponBoltActionDelay = 1f;
    

    [Header("Firearm Accuracy Stats")]
    // The starting accuracy of the weapon as a cone
    public float weaponSpreadStart = 0.01f;
    // The maximum ammount of inaccuracy the weapon can reach from recoil
    public float weaponSpreadMax = 0.10f;
    // How much the current spread is increased by with each shot
    public float weaponSpreadRecoilImpulse = 0.02f;
    // How much accuracy is restored each cycle
    public float weaponSpreadRecoilRecoveryAmount = 0.01f;
    // How much time delay between each accuracy recovery cycle
    public float weaponSpreadRecoilRecoveryDelay = 0.01f;
    // How much to multiply the current spread by when the holder is moving
    public float weaponSpreadMovementMult = 1.5f;
    
    [Header("Firearm Accuracy Features")]
    // Wether or not the weapon gets more accurate with each shot (starting at max spread and improving to Spread Start)
    public bool weaponIsReverseRecoilImpulse = false;

    [Header("Firearm Ammo Stats")]
    // Wether or not the weapon requires ammo to fire
    public bool weaponUsesAmmo = true;
    // Enum selector for the ammo prefab used by this weapon
    public enumWeaponAmmoType weaponAmmoType;
    public F_Item WeaponAmmoItem => F_Utility_Config_WeaponAmmo.GetAmmoItem(weaponAmmoType);
    /// <summary>The sprite drawn in front of the character holding this firearm, from the held weapon config.</summary>
    public Sprite WeaponHeldSpriteAsset => F_Utility_Config_HeldWeapons.GetHeldSprite(weaponHeldSprite);
    // How much ammo this weapon can hold before it needs to reload
    public int weaponAmmoCapacity = 5;
    // How much ammo is used per shot (The gun can't fire if there isn't enough loaded)
    public int weaponAmmoTakenPerShot = 1;
    // How many seconds this weapon takes to perform a reload cycle
    public float weaponReloadDelay = 3.00f;
    // How far around the player the shot can be heard in the world (For use in the custom spatial audio manger system)
    public EnumSoundLoudness weaponShotLoudness;

    [Header("Firearm Ammo Features")]
    // Wether or not the weapon reloads its entire magazine per reload cycle or just one at a time in a loop (Like loading a shotgun with shells)
    public bool weaponIsReloadOneBulletPerReload = false;

    [Header("Constants Private")]
    private const float minimumSpreadRecoveryDelay = 0.01f;
    private const float minimumHolderMovementSpeedSqr = 0.0001f;
    private const float minimumFireDelay = 0f;
    private const float reloadMiddleFraction = 0.5f;
    private const float reloadCompletionDelay = 0.3f;
    private const float oneAtATimeFirstRoundDelayMultiplier = 2f;
    private const enumItemType firearmDefaultItemType = enumItemType.WeaponGunTwoHanded;

    [Header("Privates")]
    private int currentAmmoLoaded;
    private float currentAccuracySpread;
    private float currentMovementSpread;
    private float nextFireReadyTime;
    // Time at which the current bolt action cycle finishes; reloading is blocked until then
    private float boltActionEndTime;
    private float nextSpreadRecoveryTime;
    private bool isReloading;
    private bool wasHolderMoving;
    private float reloadSegmentStartTime;
    private float reloadSegmentDuration;
    // True during the completion delay at the end of a reload, when the reload can no longer be interrupted by firing
    private bool isFinishingReload;
    private Coroutine reloadRoutine;
    private readonly List<Collider2D> firedProjectileColliders = new List<Collider2D>();

    /// <summary>How many rounds are currently loaded in the weapon.</summary>
    public int CurrentAmmoLoaded => currentAmmoLoaded;

    /// <summary>Whether the weapon is currently mid-reload.</summary>
    public bool IsReloading => isReloading;

    /// <summary>Whether the weapon is in the short completion delay at the end of a reload.</summary>
    public bool IsFinishingReload => isFinishingReload;

    /// <summary>Progress from 0 to 1 of the current reload cycle segment (the whole reload, or a single round when loading one at a time).</summary>
    public float ReloadProgress
    {
        get
        {
            if (!isReloading)
            {
                return 0f;
            }

            if (reloadSegmentDuration <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((HeldTime - reloadSegmentStartTime) / reloadSegmentDuration);
        }
    }

    private void BeginReloadProgressSegment(float segmentDuration)
    {
        reloadSegmentStartTime = HeldTime;
        reloadSegmentDuration = segmentDuration;
    }

    /// <summary>Starts the deploy delay, during which the weapon cannot fire or reload. Cancels any in-progress reload.</summary>
    public override void BeginDeploy()
    {
        InterruptReload();
        base.BeginDeploy();
    }

    /// <summary>Whether holding the primary action keeps firing (fully automatic firearms).</summary>
    public override bool IsUseContinuous => weaponIsFullyAutomatic;

    /// <summary>Firearms can be used when they are able to fire a shot; the holder is not needed.</summary>
    public override bool CanUseWeapon(F_Character weaponHolder)
    {
        return CanFireWeapon();
    }

    /// <summary>Firearm use is blocked when the weapon is broken or out of ammo.</summary>
    public override bool IsUseBlocked(F_Character weaponHolder)
    {
        return IsFireBlockedByBrokenOrEmpty();
    }

    /// <summary>Firearms may only use the gun item types, so they only fit the matching weapon slots.</summary>
    protected override bool IsItemTypeAllowed(enumItemType candidateItemType)
    {
        return F_Utility_Helper_Weapon.IsFirearmItemType(candidateItemType);
    }

    /// <summary>The firearm item type applied when an invalid type is selected.</summary>
    protected override enumItemType DefaultItemType => firearmDefaultItemType;

    /// <summary>The weapon's current accuracy cone half-angle, in radians, including movement spread and its recovery.</summary>
    public float CurrentAccuracySpreadRadians => IsHolderMoving()
        ? currentAccuracySpread * Mathf.Max(0f, weaponSpreadMovementMult)
        : currentAccuracySpread + currentMovementSpread;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentAmmoLoaded = 0;
        currentAccuracySpread = GetRestingAccuracySpread();
        nextSpreadRecoveryTime = Time.time + weaponSpreadRecoilRecoveryDelay;
    }

    protected override void Update()
    {
        base.Update();
        UpdateMovementSpread();
        RecoverAccuracySpread();
    }

    /// <summary>Whether the weapon cannot fire because it is broken or has too little ammo loaded.</summary>
    public bool IsFireBlockedByBrokenOrEmpty()
    {
        return IsBroken() || (weaponUsesAmmo && currentAmmoLoaded < weaponAmmoTakenPerShot);
    }

    /// <summary>Returns whether this weapon is currently able to fire a shot.</summary>
    public bool CanFireWeapon()
    {
        if (IsBroken() || IsDeploying || isFinishingReload || HeldTime < nextFireReadyTime)
        {
            return false;
        }

        // An in-progress reload does not block firing as long as enough ammo is already loaded
        return !weaponUsesAmmo || currentAmmoLoaded >= weaponAmmoTakenPerShot;
    }

    /// <summary>Stops an in-progress reload without loading further ammo; rounds already loaded are kept.</summary>
    private void InterruptReload()
    {
        if (!isReloading)
        {
            return;
        }

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }

        isReloading = false;
        isFinishingReload = false;
    }

    /// <summary>Consumes ammo and cooldown for a single shot, applies recoil, and plays the fire sound.</summary>
    /// <param name="firePositionWorld">World position where the shot was fired.</param>
    /// <returns>True when the shot was successfully fired.</returns>
    public bool TryFireShot(Vector3 firePositionWorld)
    {
        if (!CanFireWeapon() || !TryConsumeAmmoForShot())
        {
            return false;
        }

        InterruptReload();
        ApplyDurabilityLossForUse();
        ProcessShot(firePositionWorld);
        return true;
    }

    /// <summary>Consumes ammo and processes a follow-up shot in an active burst without applying the regular fire-rate cooldown.</summary>
    /// <param name="firePositionWorld">World position where the shot was fired.</param>
    /// <returns>True when the follow-up shot was successfully fired.</returns>
    public bool TryFireBurstFollowupShot(Vector3 firePositionWorld)
    {
        if (IsBroken() || IsDeploying || isFinishingReload || !TryConsumeAmmoForShot())
        {
            return false;
        }

        InterruptReload();
        ApplyDurabilityLossForUse();
        ProcessShot(firePositionWorld);
        return true;
    }

    /// <summary>Completes a firing sequence, playing any configured bolt-action sound and applying cooldowns.</summary>
    /// <param name="firePositionWorld">World position where the weapon fired.</param>
    public void CompleteFireSequence(Vector3 firePositionWorld)
    {
        float boltActionDelay = weaponIsBoltAction
            ? Mathf.Max(minimumFireDelay, weaponBoltActionDelay)
            : minimumFireDelay;
        float fireRateDelay = Mathf.Max(minimumFireDelay, weaponFireRateDelay);
        nextFireReadyTime = HeldTime + boltActionDelay + fireRateDelay;
        boltActionEndTime = HeldTime + boltActionDelay;

        if (!weaponIsBoltAction || weaponBoltActionSound == null)
        {
            return;
        }

        if (boltActionDelay <= minimumFireDelay)
        {
            PlayBoltActionSound(firePositionWorld);
            return;
        }

        StartCoroutine(PlayBoltActionSoundRoutine(firePositionWorld, boltActionDelay));
    }

    private IEnumerator PlayBoltActionSoundRoutine(Vector3 firePositionWorld, float delay)
    {
        yield return WaitForHeldSeconds(delay);
        PlayBoltActionSound(firePositionWorld);
    }

    private void PlayBoltActionSound(Vector3 firePositionWorld)
    {
        F_Logic_Audio.PlaySound(
            weaponBoltActionSound,
            HeldSoundType,
            weaponShotLoudness,
            100f,
            firePositionWorld);
    }

    private bool TryConsumeAmmoForShot()
    {
        if (!weaponUsesAmmo)
        {
            return true;
        }

        if (currentAmmoLoaded < weaponAmmoTakenPerShot)
        {
            return false;
        }

        currentAmmoLoaded -= weaponAmmoTakenPerShot;
        return true;
    }

    private void ProcessShot(Vector3 firePositionWorld)
    {
        ApplyRecoilImpulse();

        if (weaponFireSound != null)
        {
            F_Logic_Audio.PlaySound(
                weaponFireSound,
                HeldSoundType,
                weaponShotLoudness,
                100f,
                firePositionWorld);
        }
    }

    /// <summary>Prevents this weapon's newly fired projectile colliders from colliding with its active projectiles.</summary>
    /// <param name="projectileColliders">The colliders belonging to a newly spawned projectile.</param>
    public void IgnoreCollisionsWithPreviouslyFiredProjectiles(Collider2D[] projectileColliders)
    {
        if (projectileColliders == null || projectileColliders.Length == 0)
        {
            return;
        }

        for (int existingIndex = firedProjectileColliders.Count - 1; existingIndex >= 0; existingIndex--)
        {
            Collider2D existingCollider = firedProjectileColliders[existingIndex];
            if (existingCollider == null)
            {
                firedProjectileColliders.RemoveAt(existingIndex);
                continue;
            }

            for (int newIndex = 0; newIndex < projectileColliders.Length; newIndex++)
            {
                Collider2D newCollider = projectileColliders[newIndex];
                if (newCollider != null)
                {
                    Physics2D.IgnoreCollision(existingCollider, newCollider);
                }
            }
        }

        for (int newIndex = 0; newIndex < projectileColliders.Length; newIndex++)
        {
            Collider2D newCollider = projectileColliders[newIndex];
            if (newCollider != null)
            {
                firedProjectileColliders.Add(newCollider);
            }
        }
    }

    /// <summary>Rotates an aim direction randomly within the weapon's current accuracy cone.</summary>
    /// <param name="aimDirection">The unmodified aim direction, e.g. the fire point's forward vector.</param>
    public Vector2 ApplyAccuracySpreadToDirection(Vector2 aimDirection)
    {
        float currentSpread = CurrentAccuracySpreadRadians;
        float randomSpreadRadians = Random.Range(-currentSpread, currentSpread);
        float randomSpreadDegrees = randomSpreadRadians * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, randomSpreadDegrees) * aimDirection;
    }

    /// <summary>Returns the damage this weapon's projectile should deal after range falloff (or buildup, if reversed).</summary>
    /// <param name="distanceTraveled">How far the projectile has traveled from its firing position.</param>
    public float GetDamageAtDistance(float distanceTraveled)
    {
        float rangeFraction = weaponRange > 0f ? Mathf.Clamp01(distanceTraveled / weaponRange) : 1f;
        float falloffMultiplier = weaponIsRangeReverseFallOff
            ? Mathf.Lerp(weaponRangeDamageFallOffMin, 1f, rangeFraction)
            : Mathf.Lerp(1f, weaponRangeDamageFallOffMin, rangeFraction);

        return weaponDamage * falloffMultiplier;
    }

    /// <summary>Starts the weapon's reload cycle, drawing rounds from the ammo reserve as needed.</summary>
    /// <param name="ammoReserve">Where the reload draws ammo from (the player inventory, or an NPC's virtual ammo).</param>
    public void StartReload(IF_Item_Weapon_AmmoReserve ammoReserve)
    {
        if (ammoReserve == null || IsBroken() || isReloading || IsDeploying || HeldTime < boltActionEndTime || !weaponUsesAmmo || currentAmmoLoaded >= weaponAmmoCapacity || !HasReserveAmmo(ammoReserve))
        {
            return;
        }

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
        }

        reloadRoutine = StartCoroutine(ReloadRoutine(ammoReserve));
    }

    /// <summary>Whether the weapon could start a reload from the given reserve right now, ignoring timing (deploy, bolt action).</summary>
    /// <param name="ammoReserve">Where the reload would draw ammo from.</param>
    public bool CanReloadFrom(IF_Item_Weapon_AmmoReserve ammoReserve)
    {
        return ammoReserve != null && !IsBroken() && !isReloading && weaponUsesAmmo && currentAmmoLoaded < weaponAmmoCapacity && HasReserveAmmo(ammoReserve);
    }

    private bool HasReserveAmmo(IF_Item_Weapon_AmmoReserve ammoReserve)
    {
        return ammoReserve.GetReserveAmmoCount(WeaponAmmoItem) > 0;
    }

    private void PlayReloadSound(AudioClip reloadSound)
    {
        PlayWeaponSound(reloadSound);
    }

    private IEnumerator ReloadRoutine(IF_Item_Weapon_AmmoReserve ammoReserve)
    {
        isReloading = true;

        if (weaponIsReloadOneBulletPerReload)
        {
            PlayReloadSound(weaponReloadStartSound);

            int roundsLoadedThisCycle = 0;

            while (currentAmmoLoaded < weaponAmmoCapacity)
            {
                if (!HasReserveAmmo(ammoReserve))
                {
                    break;
                }

                float roundDelay = roundsLoadedThisCycle == 0
                    ? weaponReloadDelay * oneAtATimeFirstRoundDelayMultiplier
                    : weaponReloadDelay;
                BeginReloadProgressSegment(roundDelay);
                yield return WaitForHeldSeconds(roundDelay);

                int consumedAmmo = ammoReserve.ConsumeReserveAmmo(WeaponAmmoItem, 1);
                if (consumedAmmo <= 0)
                {
                    break;
                }

                PlayReloadSound(weaponReloadOneAtATimeSound);

                currentAmmoLoaded += consumedAmmo;
                roundsLoadedThisCycle++;
            }

            if (roundsLoadedThisCycle > 0)
            {
                PlayReloadSound(weaponReloadEndSound);
                isFinishingReload = true;
                BeginReloadProgressSegment(0f);
                yield return WaitForHeldSeconds(reloadCompletionDelay);
            }
        }
        else
        {
            PlayReloadSound(weaponReloadStartSound);
            BeginReloadProgressSegment(weaponReloadDelay);
            yield return WaitForHeldSeconds(weaponReloadDelay * reloadMiddleFraction);
            if (HasReserveAmmo(ammoReserve))
            {
                PlayReloadSound(weaponReloadMiddleSound);
            }

            yield return WaitForHeldSeconds(weaponReloadDelay * (1f - reloadMiddleFraction));

            int neededAmmo = weaponAmmoCapacity - currentAmmoLoaded;
            int availableAmmo = ammoReserve.GetReserveAmmoCount(WeaponAmmoItem);
            int ammoToLoad = Mathf.Min(neededAmmo, availableAmmo);

            if (ammoToLoad > 0)
            {
                PlayReloadSound(weaponReloadEndSound);
                int consumedAmmo = ammoReserve.ConsumeReserveAmmo(WeaponAmmoItem, ammoToLoad);
                currentAmmoLoaded += consumedAmmo;
                isFinishingReload = true;
                BeginReloadProgressSegment(0f);
                yield return WaitForHeldSeconds(reloadCompletionDelay);
            }
        }

        isReloading = false;
        isFinishingReload = false;
        reloadRoutine = null;
    }

    private void UpdateMovementSpread()
    {
        bool holderIsMoving = IsHolderMoving();
        if (holderIsMoving || wasHolderMoving)
        {
            currentMovementSpread = currentAccuracySpread * (Mathf.Max(0f, weaponSpreadMovementMult) - 1f);
        }

        wasHolderMoving = holderIsMoving;
    }

    private bool IsHolderMoving()
    {
        return IsHeld && HeldByRigidbody != null &&
            HeldByRigidbody.linearVelocity.sqrMagnitude > minimumHolderMovementSpeedSqr;
    }

    private void RecoverAccuracySpread()
    {
        if (!Application.isPlaying || Time.time < nextSpreadRecoveryTime)
        {
            return;
        }

        nextSpreadRecoveryTime = Time.time + Mathf.Max(weaponSpreadRecoilRecoveryDelay, minimumSpreadRecoveryDelay);
        float recoveryAmount = Mathf.Max(0f, weaponSpreadRecoilRecoveryAmount);
        currentAccuracySpread = Mathf.MoveTowards(
            currentAccuracySpread,
            GetRestingAccuracySpread(),
            recoveryAmount);
        if (!IsHolderMoving())
        {
            currentMovementSpread = Mathf.MoveTowards(currentMovementSpread, 0f, recoveryAmount);
        }
    }

    private void ApplyRecoilImpulse()
    {
        if (weaponIsReverseRecoilImpulse)
        {
            currentAccuracySpread = Mathf.Max(currentAccuracySpread - weaponSpreadRecoilImpulse, weaponSpreadStart);
        }
        else
        {
            currentAccuracySpread = Mathf.Min(currentAccuracySpread + weaponSpreadRecoilImpulse, weaponSpreadMax);
        }
    }

    private float GetRestingAccuracySpread()
    {
        return weaponIsReverseRecoilImpulse ? weaponSpreadMax : weaponSpreadStart;
    }

    /// <summary>Whether the weapon currently holds ammo that can be removed (not possible mid-reload).</summary>
    public bool CanUnloadAmmo => weaponUsesAmmo && WeaponAmmoItem != null && !isReloading && currentAmmoLoaded > 0;

    /// <summary>Empties the weapon's magazine and returns how many rounds were removed.</summary>
    /// <remarks>Only changes the weapon's own state; the caller is responsible for giving the rounds to an inventory.</remarks>
    public int TakeLoadedAmmo()
    {
        if (!CanUnloadAmmo)
        {
            return 0;
        }

        int removedAmmo = currentAmmoLoaded;
        currentAmmoLoaded = 0;
        return removedAmmo;
    }

    /// <summary>Adds the Unload action for weapons that use ammo, on top of the shared item actions.</summary>
    public override List<enumItemContextAction> GetContextMenuActions()
    {
        List<enumItemContextAction> actions = base.GetContextMenuActions();
        if (weaponUsesAmmo && WeaponAmmoItem != null)
        {
            actions.Insert(0, enumItemContextAction.Unload);
        }

        return actions;
    }

    /// <summary>Adds weapon-specific stats on top of the shared item tooltip details.</summary>
    public override List<F_ItemTooltipDetail> GetTooltipDetails()
    {
        List<F_ItemTooltipDetail> tooltipDetails = base.GetTooltipDetails();
        tooltipDetails.Add(new F_ItemTooltipDetail("Fire Rate", weaponFireRateDelay.ToString("0.##", CultureInfo.InvariantCulture) + "s"));
        tooltipDetails.Add(new F_ItemTooltipDetail("Range", weaponRange.ToString("0.##", CultureInfo.InvariantCulture)));

        if (weaponUsesAmmo)
        {
            tooltipDetails.Add(new F_ItemTooltipDetail(
                "Ammo",
                currentAmmoLoaded.ToString(CultureInfo.InvariantCulture) + "/" + weaponAmmoCapacity.ToString(CultureInfo.InvariantCulture)));
        }

        return tooltipDetails;
    }
}
