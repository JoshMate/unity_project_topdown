/// <summary>How a random value is distributed between its minimum and maximum.</summary>
public enum enumRandomCurve
{
    // Every value is equally likely
    Random,
    // Middle values are more common than the extremes
    BellCurve,
    // Lower values are most common, fading off linearly towards higher ones
    CliffCurve,
    // Higher values are most common, fading off linearly towards lower ones
    RampCurve
}
