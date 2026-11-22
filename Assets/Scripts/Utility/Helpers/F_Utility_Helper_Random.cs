using UnityEngine;

public static class F_Utility_Helper_Random
{
    [Header("Constants Private")]
    // More samples give a tighter bell; three is a close, cheap approximation of a normal distribution
    private const int randomBellCurveSampleCount = 3;
    private const float randomChanceNever = 0f;
    private const float randomChanceAlways = 1f;

    /// <summary>Rolls a chance from 0 (never) to 1 (always); 0 and 1 are guaranteed outcomes.</summary>
    /// <param name="chance">Probability of success, from 0 to 1.</param>
    /// <returns>True when the roll succeeds.</returns>
    public static bool RollChance(float chance)
    {
        if (chance <= randomChanceNever)
        {
            return false;
        }

        return chance >= randomChanceAlways || Random.value < chance;
    }

    /// <summary>Rolls a whole number between min and max (both inclusive), distributed by the given curve.</summary>
    /// <param name="minValue">The lowest value that can be returned.</param>
    /// <param name="maxValue">The highest value that can be returned; treated as minValue when lower than it.</param>
    /// <param name="curve">How the result is distributed across the range.</param>
    /// <returns>A value from minValue to maxValue.</returns>
    public static int RollIntegerInRange(int minValue, int maxValue, enumRandomCurve curve)
    {
        maxValue = Mathf.Max(minValue, maxValue);
        int possibleValueCount = maxValue - minValue + 1;
        int rolledOffset = Mathf.FloorToInt(RollNormalised(curve) * possibleValueCount);
        return Mathf.Min(minValue + rolledOffset, maxValue);
    }

    // Returns a value from 0 to 1 distributed by the curve
    private static float RollNormalised(enumRandomCurve curve)
    {
        switch (curve)
        {
            case enumRandomCurve.BellCurve:
                float sampleTotal = 0f;
                for (int sampleIndex = 0; sampleIndex < randomBellCurveSampleCount; sampleIndex++)
                {
                    sampleTotal += Random.value;
                }
                return sampleTotal / randomBellCurveSampleCount;
            case enumRandomCurve.CliffCurve:
                // The lower of two uniform rolls has a linearly falling distribution
                return Mathf.Min(Random.value, Random.value);
            case enumRandomCurve.RampCurve:
                // The higher of two uniform rolls has a linearly rising distribution
                return Mathf.Max(Random.value, Random.value);
            default:
                return Random.value;
        }
    }
}
