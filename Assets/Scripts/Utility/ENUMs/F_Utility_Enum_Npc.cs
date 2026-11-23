/// <summary>What an NPC is currently doing. Drives F_NPC movement.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcState
{
    Idle,
    Wander,
    Chase,
    // Standing still while attacking its target
    Attack,
    // Moving to, or holding, a position its target can't shoot
    Cover,
    // Moving around its target's side to attack from another angle
    Flank,
    // Engaged with its target but deliberately not moving
    Hold,
    // Moving to a spot with a clear shot because a friendly is in its line of fire
    Reposition,
    // Following the ally it guards
    Follow,
    // Heading to where it last saw its target, which it can no longer see
    Search,
    // Heading to the last hostile sound it heard, while not engaged
    Investigate
}

/// <summary>What stands between an NPC and its target along its line of fire.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcLineOfFire
{
    // Nothing in the way
    Clear,
    // Only friendly characters are in the way
    BlockedByFriendly,
    // Walls or other shot-blocking ents are in the way
    Blocked
}

/// <summary>How an NPC fights once it notices a target. Each value maps to a behaviour profile in F_Utility_Config_Npc.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatApproach
{
    // Mixes offence and defence: uses cover, returns fire, then smartly approaches when out of ammo or melee only
    Balanced,
    // Sprints straight at the target by the fastest route, attacking all the way, and never stops
    ChargerAggressive,
    // Runs at the target around the back of cover or other NPCs, using cover a little when reloading
    ChargerFlanker,
    // Gets to cover and defends itself, but never charges the target
    DefensiveMobile,
    // Never moves; returns fire on the spot, even when out of ammo or out of range
    DefensiveStationary,
    // Guards an ally (the player when nearby): stays very close, fights beside it and always catches up before fighting or defending
    BodyGuardClose,
    // Guards an ally (the player when nearby): fights independently using cover and repositioning, then follows once the threat has gone
    BodyGuardLoose
}

/// <summary>How well an NPC aims and fires firearms. Each value maps to an aiming profile in F_Utility_Config_Npc.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatSkillAiming
{
    // Aims well wide, tracks sluggishly, takes ages to lock on and fires far slower than the weapon allows
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    // Aims dead on, tracks instantly, fires the moment it sees the target and at the weapon's maximum rate
    Perfect
}

/// <summary>How far an NPC can see, in world units, using line of sight. Each value maps to a range in F_Utility_Config_Npc.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatSightRange
{
    // Sees nothing at all
    Blind,
    VeryClose,
    Close,
    Medium,
    Far,
    VeryFar,
    ExtremelyFar,
    // Sees anything in line of sight, however far away
    InfinitelyFar
}

/// <summary>
/// How far an NPC can hear, in world units. A spatial sound is heard when it is within both this range and the sound's
/// own loudness radius (EnumSoundLoudness). Each value maps to a range in F_Utility_Config_Npc.
/// </summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatHearing
{
    // Hears nothing at all
    Deaf,
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    // Hears any sound that reaches it, however far away
    Infinite
}

/// <summary>
/// How quickly an NPC notices a target in sight. A target must stay in sight for the whole delay to be noticed.
/// Each value maps to a delay in F_Utility_Config_Npc.
/// </summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatAwareness
{
    Slowest,
    VerySlow,
    Slow,
    Medium,
    Fast,
    VeryFast,
    ExtremelyFast,
    // Notices a target the moment it comes into sight
    Instant
}

/// <summary>
/// How long an engaged NPC remembers a target after losing sight of it. While it remembers, it heads to where it last
/// saw the target to try and find it; once the memory runs out it forgets the target and moves on.
/// Each value maps to a duration in F_Utility_Config_Npc.
/// </summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatMemory
{
    // Forgets a target the moment sight of it is broken
    None,
    VeryShort,
    Short,
    Medium,
    Long,
    VeryLong,
    // Never effectively forgets a target
    Infinite
}

/// <summary>Whether an NPC attacks walls and obstacles that stand between it and its goal: its target while engaged, or a heard sound while investigating.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcPersonalityCombatWallBehaviour
{
    // Attacks whatever blocks its line to the goal (targets it can shoot directly always come first), and breaks out when trapped
    BreakAllObstacles,
    // Only attacks obstacles when no route reaches its goal, such as being shut in a room with no exits
    BreakOnlyIfTrapped,
    // Never attacks walls or obstacles; when trapped it stays trapped
    NeverBreakAnything
}

/// <summary>How an NPC moves when it wants to close the distance to its target.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcApproachMode
{
    // Never closes the distance
    None,
    // Straight at the target by the fastest route
    Direct,
    // Around the target's side, preferring points the target can't shoot
    Flank,
    // Hops between cover points that get closer to the target
    CoverHop
}

/// <summary>Which of its equipped weapons an NPC wants to be holding.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcWeaponChoice
{
    None,
    Melee,
    Firearm
}

/// <summary>
/// The thought icon shown above an NPC's head. Each value maps to a sprite and display duration in
/// F_Utility_Config_NpcThoughts, so a new thought is a new value here plus its config entry.
/// </summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumNpcThought
{
    // Nothing shown
    None,
    // A target is in sight but has not been fully noticed yet (question mark)
    Suspicious,
    // The target has just been fully noticed and engaged (exclamation mark)
    Alerted
}
