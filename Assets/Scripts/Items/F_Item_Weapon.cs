using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shared base for every weapon type (firearm, melee, ...). Owns durability, damage, deploy timing and the held clock
/// so each weapon type only implements how it is used.
/// </summary>
public abstract class F_Item_Weapon : F_Item
{
    [Header("Weapon Art Sounds")]
    // Played once at the start of deploying the weapon
    public AudioClip weaponDeploySound;
    // Played once when the weapon finishes deploying and becomes usable
    public AudioClip weaponDeployEndSound;
    // Played directly when the weapon's durability reaches zero and it breaks
    public AudioClip weaponBreakSound;
    // Played directly when a use attempt is blocked (broken, out of ammo, not enough stamina...)
    public AudioClip weaponDryFireSound;

    [Header("Weapon Damage Stats")]
    // How much damage the weapon deals to each target it hits
    public float weaponDamage = 1f;
    // What type that damage is
    public enumDamageType weaponDamageType;

    [Header("Weapon Handling Stats")]
    // Deploy delay in hundredths of a second, selected from the weapon delay presets
    public enumWeaponDeployDelay weaponDeployDelay = enumWeaponDeployDelay.DeployDelayTwoHandedFast;

    [Header("Weapon Durability Stats")]
    // How much durability the weapon currently has left
    [Range(0f, 100f)]
    public float weaponDurabilityCurrent = 100f;
    // How much durability the weapon has permanently lost from its fixed maximum
    [FormerlySerializedAs("weaponDurabilityLost")]
    [InspectorName("Durability Max Lost")]
    [Range(0f, 100f)]
    public float weaponDurabilityMaxLost = 0f;
    // How much durability the weapon loses each time it is used (each shot or swing)
    [FormerlySerializedAs("weaponDurabilityLostPerShot")]
    public float weaponDurabilityLostPerUse = 10f;

    [Header("Constants Private")]
    private const float weaponDurabilityMax = 100f;
    private const float weaponDurabilityMin = 0f;
    private const float weaponDeployDelayHundredthsPerSecond = 100f;
    private const float weaponMinimumDelay = 0f;
    private const float weaponSoundVolume = 100f;
    private const string weaponDefaultBreakSoundAssetPath = "Assets/Sound/Interface/Weapon Handling/SD_Interface_Weapon_Break.ogg";
    private const string weaponDefaultDryFireSoundAssetPath = "Assets/Sound/Interface/Weapon Handling/SD_Interface_Weapon_Click.ogg";
    private const string weaponDefaultDeployStartSoundAssetPath = "Assets/Sound/Interface/Weapon Handling/SD_Interface_Deploy_Weapon_Start.wav";
    private const string weaponDefaultDeployEndSoundAssetPath = "Assets/Sound/Interface/Weapon Handling/SD_Interface_Deploy_Weapon_End.wav";

    [Header("Privates")]
    private float weaponDeployStartTime;
    private float weaponDeployEndTime;
    // True from the start of a deploy until its end sound has been played
    private bool weaponIsDeployEndSoundPending;
    // Held clock state: delays run on HeldTime so they only progress while this weapon is held
    private bool weaponIsHeld;
    private Rigidbody2D weaponHeldByRigidbody;
    // Direct for the player so their weapon sounds are always heard clearly, Spatial for NPCs so theirs fade with distance
    private EnumSoundType weaponHeldSoundType = EnumSoundType.Direct;
    private float weaponHeldSinceTime;
    private float weaponAccumulatedHeldTime;

    /// <summary>Whether this weapon is currently the held weapon.</summary>
    protected bool IsHeld => weaponIsHeld;

    /// <summary>The holder's Rigidbody2D while held, otherwise null.</summary>
    protected Rigidbody2D HeldByRigidbody => weaponHeldByRigidbody;

    /// <summary>How this weapon's sounds play for its current holder.</summary>
    protected EnumSoundType HeldSoundType => weaponHeldSoundType;

    /// <summary>World position this weapon's sounds play from: the holder while held, otherwise the weapon itself.</summary>
    protected Vector3 HeldSoundPosition => weaponHeldByRigidbody != null ? (Vector3)weaponHeldByRigidbody.position : transform.position;

    /// <summary>Seconds this weapon has spent held. All weapon delays run on this clock so they only progress while the weapon is held.</summary>
    public float HeldTime => weaponAccumulatedHeldTime + (weaponIsHeld ? Time.time - weaponHeldSinceTime : 0f);

    /// <summary>Whether the weapon is still being deployed after being selected, blocking all use.</summary>
    public bool IsDeploying => HeldTime < weaponDeployEndTime;

    /// <summary>Progress from 0 to 1 of the current deploy delay, or 0 when not deploying.</summary>
    public float DeployProgress
    {
        get
        {
            if (!IsDeploying)
            {
                return 0f;
            }

            float deployDuration = weaponDeployEndTime - weaponDeployStartTime;
            return deployDuration <= 0f ? 1f : Mathf.Clamp01((HeldTime - weaponDeployStartTime) / deployDuration);
        }
    }

    /// <summary>Whether holding the primary action keeps using the weapon, rather than one use per press.</summary>
    public virtual bool IsUseContinuous => false;

    /// <summary>Returns whether this weapon can be used right now by the given holder.</summary>
    /// <param name="weaponHolder">The character holding the weapon, or null when unknown.</param>
    public abstract bool CanUseWeapon(F_Character weaponHolder);

    /// <summary>Returns whether a use attempt is blocked by a condition that should play the dry-fire feedback.</summary>
    /// <param name="weaponHolder">The character holding the weapon, or null when unknown.</param>
    public abstract bool IsUseBlocked(F_Character weaponHolder);

    /// <summary>Returns whether this weapon class may use the given item type (the item type decides which slots and held arms it uses).</summary>
    protected abstract bool IsItemTypeAllowed(enumItemType candidateItemType);

    /// <summary>The item type applied when an item type this weapon class doesn't allow is selected.</summary>
    protected abstract enumItemType DefaultItemType { get; }

    /// <summary>Keeps the item type valid for this weapon class, so it can only be equipped in its matching slots.</summary>
    protected override void ValidateItemData()
    {
        base.ValidateItemData();
        if (!IsItemTypeAllowed(itemType))
        {
            itemType = DefaultItemType;
        }
    }

    protected virtual void Update()
    {
        PlayDeployEndSoundWhenFinished();
    }

    /// <summary>Marks the weapon as held or holstered and assigns the holder's rigidbody and sound playback type.</summary>
    /// <param name="isNowHeld">True when the weapon becomes the held weapon.</param>
    /// <param name="holderRigidbody">The holder's Rigidbody2D, or null when unavailable.</param>
    /// <param name="holderSoundType">How the weapon's sounds play for this holder.</param>
    public void SetHeld(bool isNowHeld, Rigidbody2D holderRigidbody = null, EnumSoundType holderSoundType = EnumSoundType.Direct)
    {
        weaponHeldByRigidbody = isNowHeld ? holderRigidbody : null;
        if (isNowHeld)
        {
            weaponHeldSoundType = holderSoundType;
        }

        if (weaponIsHeld == isNowHeld)
        {
            return;
        }

        if (isNowHeld)
        {
            weaponHeldSinceTime = Time.time;
        }
        else
        {
            weaponAccumulatedHeldTime += Time.time - weaponHeldSinceTime;
        }

        weaponIsHeld = isNowHeld;
    }

    /// <summary>Waits for the given number of held seconds; time spent holstered does not count.</summary>
    protected IEnumerator WaitForHeldSeconds(float duration)
    {
        float targetHeldTime = HeldTime + duration;
        while (HeldTime < targetHeldTime)
        {
            yield return null;
        }
    }

    /// <summary>Starts the deploy delay, during which the weapon cannot be used.</summary>
    public virtual void BeginDeploy()
    {
        weaponDeployStartTime = HeldTime;
        PlayWeaponSound(weaponDeploySound);

        weaponDeployEndTime = HeldTime + Mathf.Max(weaponMinimumDelay, (float)weaponDeployDelay / weaponDeployDelayHundredthsPerSecond);
        weaponIsDeployEndSoundPending = true;
    }

    // Plays the deploy end sound once the weapon is held and its deploy delay has finished
    private void PlayDeployEndSoundWhenFinished()
    {
        if (!weaponIsDeployEndSoundPending || !weaponIsHeld || IsDeploying)
        {
            return;
        }

        weaponIsDeployEndSoundPending = false;
        PlayWeaponSound(weaponDeployEndSound);
    }

    /// <summary>Plays a weapon sound for the current holder (direct for the player, spatial for NPCs) when one is assigned.</summary>
    /// <param name="weaponSound">The clip to play.</param>
    /// <param name="weaponSoundLoudness">How far away the sound can be heard by hearing systems.</param>
    protected void PlayWeaponSound(AudioClip weaponSound, EnumSoundLoudness weaponSoundLoudness = EnumSoundLoudness.NoSound)
    {
        if (weaponSound != null)
        {
            F_Logic_Audio.PlaySound(weaponSound, weaponHeldSoundType, weaponSoundLoudness, weaponSoundVolume, HeldSoundPosition);
        }
    }

    /// <summary>Maximum durability currently available after permanent losses are removed.</summary>
    public float GetDurabilityMaxEffective()
    {
        float maxLost = Mathf.Clamp(weaponDurabilityMaxLost, weaponDurabilityMin, weaponDurabilityMax);
        return weaponDurabilityMax - maxLost;
    }

    /// <summary>Current durability as a fraction of the effective maximum (0-1).</summary>
    public float GetDurabilityCurrentFraction()
    {
        float effectiveMax = GetDurabilityMaxEffective();
        if (effectiveMax <= weaponDurabilityMin)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Min(weaponDurabilityCurrent, effectiveMax) / effectiveMax);
    }

    /// <summary>Current durability as a fraction of the fixed full maximum (0-1).</summary>
    public float GetDurabilityCurrentFullFraction()
    {
        return Mathf.Clamp01(Mathf.Min(weaponDurabilityCurrent, GetDurabilityMaxEffective()) / weaponDurabilityMax);
    }

    /// <summary>Permanently lost durability as a fraction of the full maximum (0-1).</summary>
    public float GetDurabilityLostFraction()
    {
        return Mathf.Clamp01(Mathf.Clamp(weaponDurabilityMaxLost, weaponDurabilityMin, weaponDurabilityMax) / weaponDurabilityMax);
    }

    /// <summary>Whether the weapon has no durability left and cannot be used.</summary>
    public bool IsBroken()
    {
        return weaponDurabilityCurrent <= weaponDurabilityMin;
    }

    /// <summary>Applies the configured durability wear for a single use (one shot or one swing).</summary>
    public void ApplyDurabilityLossForUse()
    {
        bool wasBroken = IsBroken();
        float clampedCurrent = Mathf.Min(weaponDurabilityCurrent, GetDurabilityMaxEffective());
        weaponDurabilityCurrent = Mathf.Max(weaponDurabilityMin, clampedCurrent - weaponDurabilityLostPerUse);

        if (!wasBroken && IsBroken())
        {
            PlayWeaponSound(weaponBreakSound);
        }
    }

    /// <summary>Plays the dry-fire click used when a use attempt is blocked.</summary>
    public void PlayDryFireSound()
    {
        PlayWeaponSound(weaponDryFireSound);
    }

    /// <summary>Assigns the default weapon sounds in the editor when none are set, alongside the base item defaults.</summary>
    protected override void EnsureDefaultItemAssets()
    {
        base.EnsureDefaultItemAssets();
#if UNITY_EDITOR
        if (weaponBreakSound == null)
        {
            weaponBreakSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(weaponDefaultBreakSoundAssetPath);
        }

        if (weaponDryFireSound == null)
        {
            weaponDryFireSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(weaponDefaultDryFireSoundAssetPath);
        }

        if (weaponDeploySound == null)
        {
            weaponDeploySound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(weaponDefaultDeployStartSoundAssetPath);
        }

        if (weaponDeployEndSound == null)
        {
            weaponDeployEndSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(weaponDefaultDeployEndSoundAssetPath);
        }
#endif
    }

    /// <summary>Adds the shared weapon stats on top of the generic item tooltip details.</summary>
    public override List<F_ItemTooltipDetail> GetTooltipDetails()
    {
        List<F_ItemTooltipDetail> tooltipDetails = base.GetTooltipDetails();
        tooltipDetails.Add(new F_ItemTooltipDetail("Damage", weaponDamage.ToString("0.##", CultureInfo.InvariantCulture)));
        return tooltipDetails;
    }
}
