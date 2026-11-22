/// <summary>What an NPC is currently doing. Drives F_NPC movement.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcState
{
    Idle,
    Wander,
    Chase
}
