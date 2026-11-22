using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Weapon that swings in an arc in front of its holder, costing stamina per swing. Hits up to the cleave amount of
/// separate targets and knocks them back. Durability, damage and deploy timing come from <see cref="F_Item_Weapon"/>.
/// </summary>
public class F_Item_Weapon_Melee : F_Item_Weapon
{
    [Header("Melee Art Sounds")]
    // Played at the swing position each time the weapon is swung
    public AudioClip meleeSwingSound;
    // How far around the holder the swing can be heard in the world (For use in the custom spatial audio manager system)
    public EnumSoundLoudness meleeSwingLoudness = EnumSoundLoudness.WeaponMelee;

    [Header("Melee Attack Stats")]
    // How much delay there is between each swing
    public float meleeSwingDelay = 0.5f;
    // How far from the swing position a target can be hit
    public float meleeRange = 1.5f;
    // Width of the swing in degrees, centred on the aim direction (360 hits all around the holder)
    [Range(meleeSwingArcMin, meleeSwingArcMax)]
    public float meleeSwingArcDegrees = 90f;
    // How many separate targets a single swing can hit (closest targets are hit first)
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
    private const float meleeSwingArcHalfDivisor = 2f;
    private const int meleeCleaveAmountMin = 1;
    private const float meleeMinimumDelay = 0f;
    private const float meleeSwingSoundVolume = 100f;
    private const float meleeMinimumTargetOffsetSqr = 0.0001f;
    private const enumItemType meleeDefaultItemType = enumItemType.WeaponMeleeOneHanded;

    [Header("Privates")]
    private float meleeNextSwingReadyTime;
    private readonly List<Collider2D> meleeOverlapResults = new List<Collider2D>();
    private readonly List<F_Ent> meleeHitTargets = new List<F_Ent>();
    private readonly List<float> meleeHitTargetDistances = new List<float>();

    /// <summary>Whether holding the primary action keeps swinging when this melee weapon is fully automatic.</summary>
    public override bool IsUseContinuous => meleeIsFullyAutomatic;

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

    /// <summary>Swings the weapon: spends stamina and durability, plays the swing sound, then damages and knocks back the closest targets in the arc.</summary>
    /// <param name="weaponHolder">The character swinging, who pays the stamina and is never hit by their own swing.</param>
    /// <param name="swingPosition">World position the swing range is measured from.</param>
    /// <param name="swingDirection">Aim direction the swing arc is centred on.</param>
    /// <returns>True when the swing happened.</returns>
    public bool TrySwing(F_Character weaponHolder, Vector2 swingPosition, Vector2 swingDirection)
    {
        if (!CanUseWeapon(weaponHolder))
        {
            return false;
        }

        if (weaponHolder != null && !weaponHolder.TryConsumeStamina(meleeStaminaCost))
        {
            return false;
        }

        meleeNextSwingReadyTime = HeldTime + Mathf.Max(meleeMinimumDelay, meleeSwingDelay);
        ApplyDurabilityLossForUse();
        PlaySwingSound(swingPosition);
        HitTargetsInSwing(weaponHolder, swingPosition, swingDirection);
        return true;
    }

    private void PlaySwingSound(Vector2 swingPosition)
    {
        if (meleeSwingSound == null)
        {
            return;
        }

        F_Logic_Audio.PlaySound(meleeSwingSound, EnumSoundType.Direct, meleeSwingLoudness, meleeSwingSoundVolume, swingPosition);
    }

    // Finds every separate ent inside the swing arc, then hits the closest ones up to the cleave amount.
    private void HitTargetsInSwing(F_Character weaponHolder, Vector2 swingPosition, Vector2 swingDirection)
    {
        CollectTargetsInSwing(weaponHolder, swingPosition, swingDirection);

        int targetsToHit = Mathf.Min(Mathf.Max(meleeCleaveAmountMin, meleeCleaveAmount), meleeHitTargets.Count);
        GameObject damageSource = weaponHolder != null ? weaponHolder.gameObject : gameObject;
        for (int hitIndex = 0; hitIndex < targetsToHit; hitIndex++)
        {
            int closestIndex = GetClosestTargetIndex();
            F_Ent hitTarget = meleeHitTargets[closestIndex];
            meleeHitTargets.RemoveAt(closestIndex);
            meleeHitTargetDistances.RemoveAt(closestIndex);

            hitTarget.TakeDamage(weaponDamage, weaponDamageType, damageSource);
            hitTarget.ApplyKnockback((Vector2)hitTarget.transform.position - swingPosition, meleeKnockbackAmount);
        }

        meleeHitTargets.Clear();
        meleeHitTargetDistances.Clear();
    }

    private void CollectTargetsInSwing(F_Character weaponHolder, Vector2 swingPosition, Vector2 swingDirection)
    {
        meleeHitTargets.Clear();
        meleeHitTargetDistances.Clear();
        meleeOverlapResults.Clear();

        ContactFilter2D overlapFilter = new ContactFilter2D().NoFilter();
        Physics2D.OverlapCircle(swingPosition, Mathf.Max(0f, meleeRange), overlapFilter, meleeOverlapResults);

        float swingArcHalfDegrees = Mathf.Clamp(meleeSwingArcDegrees, meleeSwingArcMin, meleeSwingArcMax) / meleeSwingArcHalfDivisor;
        for (int colliderIndex = 0; colliderIndex < meleeOverlapResults.Count; colliderIndex++)
        {
            Collider2D overlapCollider = meleeOverlapResults[colliderIndex];
            F_Ent overlapEnt = overlapCollider != null ? overlapCollider.GetComponentInParent<F_Ent>() : null;
            if (overlapEnt == null || overlapEnt.IsDead || overlapEnt == weaponHolder || overlapEnt.entFeatureCanShootOver)
            {
                continue;
            }

            Vector2 targetOffset = overlapCollider.ClosestPoint(swingPosition) - swingPosition;
            bool isInsideArc = targetOffset.sqrMagnitude <= meleeMinimumTargetOffsetSqr ||
                               Vector2.Angle(swingDirection, targetOffset) <= swingArcHalfDegrees;
            if (!isInsideArc)
            {
                continue;
            }

            AddOrUpdateTarget(overlapEnt, targetOffset.magnitude);
        }
    }

    // Ents can have several colliders; each ent counts as one target at its closest collider distance.
    private void AddOrUpdateTarget(F_Ent targetEnt, float targetDistance)
    {
        int existingIndex = meleeHitTargets.IndexOf(targetEnt);
        if (existingIndex < 0)
        {
            meleeHitTargets.Add(targetEnt);
            meleeHitTargetDistances.Add(targetDistance);
            return;
        }

        meleeHitTargetDistances[existingIndex] = Mathf.Min(meleeHitTargetDistances[existingIndex], targetDistance);
    }

    private int GetClosestTargetIndex()
    {
        int closestIndex = 0;
        for (int targetIndex = 1; targetIndex < meleeHitTargetDistances.Count; targetIndex++)
        {
            if (meleeHitTargetDistances[targetIndex] < meleeHitTargetDistances[closestIndex])
            {
                closestIndex = targetIndex;
            }
        }

        return closestIndex;
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
        tooltipDetails.Add(new F_ItemTooltipDetail("Swing Rate", meleeSwingDelay.ToString("0.##", CultureInfo.InvariantCulture) + "s"));
        tooltipDetails.Add(new F_ItemTooltipDetail("Range", meleeRange.ToString("0.##", CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Stamina Cost", meleeStaminaCost.ToString("0.##", CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Cleave", meleeCleaveAmount.ToString(CultureInfo.InvariantCulture)));
        tooltipDetails.Add(new F_ItemTooltipDetail("Knockback", meleeKnockbackAmount.ToString("0.##", CultureInfo.InvariantCulture)));
        return tooltipDetails;
    }
}
