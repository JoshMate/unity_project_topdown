using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Weapon whose sprite is physically swung in front of its holder, costing stamina per swing. The swung sprite is the
/// hitbox (see <see cref="F_Character_MeleeHitbox"/>), hitting up to the cleave amount of separate targets it touches
/// and knocking them back. Durability, damage and deploy timing come from <see cref="F_Item_Weapon"/>.
/// </summary>
public class F_Item_Weapon_Melee : F_Item_Weapon
{
    [Header("Melee Art Sounds")]
    // Played at the swing position each time the weapon is swung
    public AudioClip meleeSwingSound;
    // How far around the holder the swing can be heard in the world (For use in the custom spatial audio manager system)
    public EnumSoundLoudness meleeSwingLoudness = EnumSoundLoudness.WeaponMelee;

    [Header("Melee Attack Stats")]
    // Seconds spent moving to the start of the swing before it can deal damage
    [Min(0f)]
    public float meleeWindUpDelay = 0.1f;
    // Seconds the weapon spends moving through its swing, the only time it deals damage
    [FormerlySerializedAs("meleeSwingDelay")]
    [Min(meleeSwingDurationMin)]
    public float meleeSwingDuration = 0.25f;
    // Seconds spent returning to the holding position after the swing stops dealing damage; the next swing is ready once it has returned
    [Min(0f)]
    public float meleeReturnDelay = 0.15f;
    // How far from the holder's centre the weapon reaches out to (close to the hold distance keeps a sweep tight to the body)
    public float meleeRange = 1.5f;
    // Width of the cone, in degrees, the weapon moves across while swinging (0 = a perfectly straight thrust)
    [Range(meleeSwingArcMin, meleeSwingArcMax)]
    public float meleeSwingArcDegrees = 90f;
    // How many separate targets a single swing can hit (first touched are hit first)
    [Min(meleeCleaveAmountMin)]
    public int meleeCleaveAmount = 1;
    // Starting speed, in world units per second, that hit targets are pushed away from the holder at
    [Min(0f)]
    public float meleeKnockbackAmount = 3f;

    [Header("Melee Stamina Stats")]
    // How much stamina each swing costs; the weapon can't be swung with less stamina than this
    [Min(0f)]
    public float meleeStaminaCost = 10f;

    [Header("Melee Attack Features")]
    // Whether holding the primary action repeatedly swings this weapon as soon as it is ready
    [InspectorName("Melee Is Fully Automatic")]
    public bool meleeIsFullyAutomatic = false;

    [Header("Constants Private")]
    private const float meleeSwingArcMin = 0f;
    private const float meleeSwingArcMax = 360f;
    private const int meleeCleaveAmountMin = 1;
    private const float meleeSwingDurationMin = 0.01f;
    private const float meleeSwingSoundVolume = 100f;
    private const enumItemType meleeDefaultItemType = enumItemType.WeaponMeleeOneHanded;

    [Header("Privates")]
    private float meleeNextSwingReadyTime;

    /// <summary>Whether holding the primary action keeps swinging when this melee weapon is fully automatic.</summary>
    public override bool IsUseContinuous => meleeIsFullyAutomatic;

    /// <summary>Seconds a full swing cycle takes: wind up, swing and return.</summary>
    public float MeleeSwingCycleDuration => Mathf.Max(0f, meleeWindUpDelay) + Mathf.Max(meleeSwingDurationMin, meleeSwingDuration) + Mathf.Max(0f, meleeReturnDelay);

    /// <summary>How many separate targets a single swing can hit, never less than one.</summary>
    public int MeleeCleaveAmountEffective => Mathf.Max(meleeCleaveAmountMin, meleeCleaveAmount);

    /// <summary>Melee weapons can be swung when not broken, deployed, off cooldown and the holder has enough stamina.</summary>
    public override bool CanUseWeapon(F_Character weaponHolder)
    {
        if (IsBroken() || IsDeploying || HeldTime < meleeNextSwingReadyTime)
        {
            return false;
        }

        return weaponHolder == null || weaponHolder.HasStamina(meleeStaminaCost);
    }

    /// <summary>Melee use is blocked when the weapon is broken or the holder lacks the stamina for a swing.</summary>
    public override bool IsUseBlocked(F_Character weaponHolder)
    {
        return IsBroken() || (weaponHolder != null && !weaponHolder.HasStamina(meleeStaminaCost));
    }

    /// <summary>Starts a swing: spends stamina and durability, starts the swing delay and plays the swing sound. Hits are applied by the hitbox.</summary>
    /// <param name="weaponHolder">The character swinging, who pays the stamina.</param>
    /// <param name="swingPosition">World position the swing sound plays from.</param>
    /// <returns>True when the swing happened.</returns>
    public bool TrySwing(F_Character weaponHolder, Vector2 swingPosition)
    {
        if (!CanUseWeapon(weaponHolder))
        {
            return false;
        }

        if (weaponHolder != null && !weaponHolder.TryConsumeStamina(meleeStaminaCost))
        {
            return false;
        }

        meleeNextSwingReadyTime = HeldTime + MeleeSwingCycleDuration;
        ApplyDurabilityLossForUse();
        PlaySwingSound(swingPosition);
        return true;
    }

    /// <summary>Works out the weapon's pose relative to its holder at the given point of a swing.</summary>
    /// <param name="swingElapsed">Seconds since the swing started.</param>
    /// <param name="localPosition">Weapon position relative to the holder.</param>
    /// <param name="localAngleDegrees">Direction the weapon points, relative to the aim direction.</param>
    /// <returns>True while the weapon is in its damaging swing phase (not winding up or returning).</returns>
    public bool EvaluateSwingPose(float swingElapsed, out Vector2 localPosition, out float localAngleDegrees)
    {
        return F_Utility_Helper_Weapon.EvaluateMeleeSwingPose(swingElapsed, meleeWindUpDelay,
            Mathf.Max(meleeSwingDurationMin, meleeSwingDuration), meleeReturnDelay, meleeRange, meleeSwingArcDegrees,
            out localPosition, out localAngleDegrees);
    }

    /// <summary>Damages a target touched by the swung weapon and knocks it away from the holder.</summary>
    /// <param name="hitTarget">The ent the weapon connected with.</param>
    /// <param name="weaponHolder">The character swinging, credited as the damage source.</param>
    /// <param name="knockbackOrigin">World position the target is pushed away from.</param>
    public void ApplyHit(F_Ent hitTarget, F_Character weaponHolder, Vector2 knockbackOrigin)
    {
        if (hitTarget == null)
        {
            return;
        }

        GameObject damageSource = weaponHolder != null ? weaponHolder.gameObject : gameObject;
        hitTarget.TakeDamage(weaponDamage, weaponDamageType, damageSource);
        hitTarget.ApplyKnockback((Vector2)hitTarget.transform.position - knockbackOrigin, meleeKnockbackAmount);
    }

    private void PlaySwingSound(Vector2 swingPosition)
    {
        if (meleeSwingSound == null)
        {
            return;
        }

        F_Logic_Audio.PlaySound(meleeSwingSound, EnumSoundType.Direct, meleeSwingLoudness, meleeSwingSoundVolume, swingPosition);
    }

    /// <summary>Melee weapons may only use the melee item types, so they only fit the matching melee weapon slots.</summary>
    protected override bool IsItemTypeAllowed(enumItemType candidateItemType)
    {
        return F_Utility_Helper_Weapon.IsMeleeItemType(candidateItemType);
    }

    /// <summary>The melee item type applied when an invalid type is selected.</summary>
    protected override enumItemType DefaultItemType => meleeDefaultItemType;

    /// <summary>Adds melee-specific stats on top of the shared weapon tooltip details.</summary>
    public override List<F_ItemTooltipDetail> GetTooltipDetails()
    {
        List<F_ItemTooltipDetail> tooltipDetails = base.GetTooltipDetails();
        tooltipDetails.Add(new F_ItemTooltipDetail("Swing Rate", MeleeSwingCycleDuration.ToString("0.##", CultureInfo.InvariantCulture) + "s"));
        tooltipDetails.Add(new F_ItemTooltipDetail("Range", meleeRange.ToString("0.##", CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Stamina Cost", meleeStaminaCost.ToString("0.##", CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Cleave", meleeCleaveAmount.ToString(CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Knockback", meleeKnockbackAmount.ToString("0.##", CultureInfo.InvariantCulture)));
        return tooltipDetails;
    }
}
