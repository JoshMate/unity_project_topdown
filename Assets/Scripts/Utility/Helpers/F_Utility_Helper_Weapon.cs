using UnityEngine;

/// <summary>Shared weapon classification rules and melee swing maths, so weapon logic lives in one place.</summary>
public static class F_Utility_Helper_Weapon
{
    [Header("Constants Private")]
    private const float weaponArcHalfDivisor = 2f;
    private const float weaponProgressMin = 0f;
    private const float weaponProgressMax = 1f;
    private const float weaponQuarterTurnRadians = Mathf.PI / 2f;

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

    /// <summary>
    /// Works out where a melee weapon sits during a swing, in the holder's local space (+X is the aim direction).
    /// The swing has three phases: wind up (hold to the start side of the arc), swing (across the arc while reaching out
    /// to the range, the only damaging phase) and return (back to the hold). A 0 arc gives a straight thrust; a high arc
    /// with a range close to the hold distance gives a sweep.
    /// </summary>
    /// <param name="swingElapsed">Seconds since the swing started.</param>
    /// <param name="windUpDelay">Seconds spent moving to the start of the swing.</param>
    /// <param name="swingDuration">Seconds spent in the damaging swing.</param>
    /// <param name="returnDelay">Seconds spent returning to the holding position.</param>
    /// <param name="swingRange">Distance from the holder's centre the weapon reaches out to by the end of the swing.</param>
    /// <param name="swingArcDegrees">Width of the cone, in degrees, the weapon moves across while swinging.</param>
    /// <param name="localPosition">Weapon position relative to the holder.</param>
    /// <param name="localAngleDegrees">Direction the weapon points, relative to the aim direction.</param>
    /// <returns>True while the weapon is in its damaging swing phase.</returns>
    public static bool EvaluateMeleeSwingPose(float swingElapsed, float windUpDelay, float swingDuration, float returnDelay,
        float swingRange, float swingArcDegrees, out Vector2 localPosition, out float localAngleDegrees)
    {
        float holdDistance = F_Utility_Config_Melee.cfgMeleeHoldDistance;
        float reachDistance = Mathf.Max(holdDistance, swingRange);
        float arcHalfDegrees = Mathf.Max(0f, swingArcDegrees) / weaponArcHalfDivisor;
        float swingStart = Mathf.Max(0f, windUpDelay);
        float returnStart = swingStart + Mathf.Max(0f, swingDuration);
        bool isDamaging = false;
        float distance;

        if (swingElapsed < swingStart)
        {
            // Wind up: turn from the hold to the start side of the arc
            float phaseProgress = GetSmoothPhaseProgress(swingElapsed, 0f, swingStart);
            distance = holdDistance;
            localAngleDegrees = Mathf.Lerp(0f, arcHalfDegrees, phaseProgress);
        }
        else if (swingElapsed < returnStart)
        {
            // Swing: steady sweep across the arc while reaching out (fast at first, easing into full reach)
            float phaseProgress = Mathf.Clamp01((swingElapsed - swingStart) / (returnStart - swingStart));
            distance = Mathf.Lerp(holdDistance, reachDistance, Mathf.Sin(phaseProgress * weaponQuarterTurnRadians));
            localAngleDegrees = Mathf.Lerp(arcHalfDegrees, -arcHalfDegrees, phaseProgress);
            isDamaging = true;
        }
        else
        {
            // Return: come back from the end of the swing to the hold
            float phaseProgress = GetSmoothPhaseProgress(swingElapsed, returnStart, returnStart + Mathf.Max(0f, returnDelay));
            distance = Mathf.Lerp(reachDistance, holdDistance, phaseProgress);
            localAngleDegrees = Mathf.Lerp(-arcHalfDegrees, 0f, phaseProgress);
        }

        float angleRadians = localAngleDegrees * Mathf.Deg2Rad;
        localPosition = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * distance;
        return isDamaging;
    }

    // Eased 0-1 progress through a phase; a zero-length phase counts as complete.
    private static float GetSmoothPhaseProgress(float elapsed, float phaseStart, float phaseEnd)
    {
        float phaseLength = phaseEnd - phaseStart;
        if (phaseLength <= 0f)
        {
            return weaponProgressMax;
        }

        return Mathf.SmoothStep(weaponProgressMin, weaponProgressMax, (elapsed - phaseStart) / phaseLength);
    }
}
