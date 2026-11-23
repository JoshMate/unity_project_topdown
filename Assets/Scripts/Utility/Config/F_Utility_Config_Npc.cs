/// <summary>
/// How one combat personality behaves. Every NPC decision reads from its profile, so a new personality is a new
/// enumNpcPersonalityCombatApproach value plus a profile in F_Utility_Config_Npc.GetCombatProfile, with no new AI code.
/// </summary>
public readonly struct F_NpcCombatProfile
{
    // Whether the NPC ever moves while engaged
    public readonly bool profileCanMove;
    // How the NPC closes in while it has a usable firearm but can't shoot its target from where it is
    public readonly enumNpcApproachMode profileApproachFirearm;
    // How the NPC closes in when it only has a melee weapon, is out of ammo, or is unarmed
    public readonly enumNpcApproachMode profileApproachMelee;
    // Whether the NPC stops moving once it can shoot its target from within range
    public readonly bool profileHoldsAtFiringRange;
    // Whether the NPC heads to cover while reloading
    public readonly bool profileCoverWhenReloading;
    // Whether the NPC heads to cover for the hold time after being hit
    public readonly bool profileCoverWhenHit;
    // Whether the NPC heads to cover for the hold time when it first notices its target
    public readonly bool profileCoverWhenNoticed;
    // Seconds spent in cover after being hit or noticing a target, before peeking out again
    public readonly float profileCoverHoldTime;
    // Whether the NPC sprints while closing in on its target
    public readonly bool profileSprintsWhenApproaching;
    // Whether the NPC attacks whenever it has a line of fire, ignoring weapon range
    public readonly bool profileAttacksOutOfRange;
    // Whether the NPC keeps pulling the trigger on an empty firearm with no ammo left
    public readonly bool profileDryFiresWhenEmpty;
    // Whether the NPC picks a friendly to guard (the player when nearby, otherwise the closest ally) and follows it instead of wandering
    public readonly bool profileGuardsAlly;
    // While not fighting, the guard follows once further than this from its guarded ally
    public readonly float profileGuardFollowDistance;
    // While fighting, the guard drops everything to catch up once further than this from its guarded ally, and never moves further away
    public readonly float profileGuardLeashDistance;
    // While not fighting and within its follow distance, whether the guard wanders around its guarded ally instead of standing beside it
    public readonly bool profileGuardWandersNearWard;
    // Whether the NPC leaves what it is doing to investigate hostile sounds it hears
    public readonly bool profileInvestigatesSounds;

    public F_NpcCombatProfile(bool canMove, enumNpcApproachMode approachFirearm, enumNpcApproachMode approachMelee,
        bool holdsAtFiringRange, bool coverWhenReloading, bool coverWhenHit, bool coverWhenNoticed, float coverHoldTime,
        bool sprintsWhenApproaching, bool attacksOutOfRange, bool dryFiresWhenEmpty,
        bool guardsAlly, float guardFollowDistance, float guardLeashDistance, bool guardWandersNearWard,
        bool investigatesSounds)
    {
        profileInvestigatesSounds = investigatesSounds;
        profileGuardWandersNearWard = guardWandersNearWard;
        profileCanMove = canMove;
        profileApproachFirearm = approachFirearm;
        profileApproachMelee = approachMelee;
        profileHoldsAtFiringRange = holdsAtFiringRange;
        profileCoverWhenReloading = coverWhenReloading;
        profileCoverWhenHit = coverWhenHit;
        profileCoverWhenNoticed = coverWhenNoticed;
        profileCoverHoldTime = coverHoldTime;
        profileSprintsWhenApproaching = sprintsWhenApproaching;
        profileAttacksOutOfRange = attacksOutOfRange;
        profileDryFiresWhenEmpty = dryFiresWhenEmpty;
        profileGuardsAlly = guardsAlly;
        profileGuardFollowDistance = guardFollowDistance;
        profileGuardLeashDistance = guardLeashDistance;
    }
}

/// <summary>
/// How well one aiming skill level uses firearms. F_NPC reads every firearm aiming decision from its profile, so
/// retuning a skill level is a value change here with no new AI code.
/// </summary>
public readonly struct F_NpcAimingProfile
{
    // Degrees per second the NPC turns to track its target while holding a firearm
    public readonly float profileAimTurnSpeed;
    // Largest random angle in degrees the NPC aims away from its target; re-rolled when it locks on and after every shot
    public readonly float profileAimErrorDegreesMax;
    // Seconds the NPC must keep its target in sight before it pulls the trigger for the first time
    public readonly float profileAimLockOnTime;
    // Multiplies the firearm's fire rate delay between the NPC's shots (1 = as fast as the weapon allows)
    public readonly float profileAimFireDelayMultiplier;
    // Chance from 0 to 1 that the NPC leads its target (aims where it will be when the projectile arrives); rolled when it locks on and after every shot
    public readonly float profileAimLeadChance;
    // Chance from 0 to 1 that the NPC fires anyway when a friendly is in its line of fire; rolled when a friendly steps into the line and after every shot
    public readonly float profileAimFriendlyFireChance;

    public F_NpcAimingProfile(float aimTurnSpeed, float aimErrorDegreesMax, float aimLockOnTime, float aimFireDelayMultiplier, float aimLeadChance,
        float aimFriendlyFireChance)
    {
        profileAimTurnSpeed = aimTurnSpeed;
        profileAimErrorDegreesMax = aimErrorDegreesMax;
        profileAimLockOnTime = aimLockOnTime;
        profileAimFireDelayMultiplier = aimFireDelayMultiplier;
        profileAimLeadChance = aimLeadChance;
        profileAimFriendlyFireChance = aimFriendlyFireChance;
    }
}

/// <summary>Global NPC AI tuning, the behaviour profile for each combat personality, the aiming profile for each aiming skill, the range for each sight range, the range for each hearing level, the delay for each awareness level and the duration for each combat memory.</summary>
public static class F_Utility_Config_Npc
{
    // Constants Public: Decisions (combat decisions run every NPC's npcStatRepathInterval)
    // Seconds an NPC keeps its current weapon before it may switch again
    public const float cfgNpcWeaponSwitchCooldown = 1f;
    // Seconds between an NPC's searches for the closest visible hostile character; while engaged this also re-picks the closest known hostile
    public const float cfgNpcTargetScanInterval = 0.3f;

    // Constants Public: Combat targeting
    // An engaged NPC only drops a target it can see for another hostile in sight that is at least this much closer, so its focus doesn't flicker between enemies at similar distances
    public const float cfgNpcCombatRetargetCloserMargin = 1.5f;
    // Sight cone width in degrees an engaged NPC uses to keep track of every hostile around it (360 = alert to all sides); never narrower than its own sight cone
    public const float cfgNpcCombatAlertSightConeSize = 360f;

    // Constants Public: Sight
    // An NPC loses sight of its target beyond its sight range multiplied by this, which starts its combat memory running out
    public const float cfgNpcSightLoseTargetRangeMultiplier = 1.5f;

    // Constants Private: Sight range values in world units
    private const float cfgNpcSightRangeBlind = 0f;
    private const float cfgNpcSightRangeVeryClose = 5f;
    private const float cfgNpcSightRangeClose = 10f;
    private const float cfgNpcSightRangeMedium = 15f;
    private const float cfgNpcSightRangeFar = 20f;
    private const float cfgNpcSightRangeVeryFar = 25f;
    private const float cfgNpcSightRangeExtremelyFar = 30f;
    private const float cfgNpcSightRangeInfinitelyFar = 999f;

    // Constants Public: Hearing
    // Seconds an NPC stays at a heard sound's position, looking around, before giving up its investigation
    public const float cfgNpcHearingInvestigateWaitTime = 3f;
    // Fraction of the current move speed used while heading to a heard sound
    public const float cfgNpcHearingInvestigateSpeedMultiplier = 0.75f;

    // Constants Private: Hearing range values in world units
    private const float cfgNpcHearingRangeDeaf = 0f;
    private const float cfgNpcHearingRangeVeryLow = 3f;
    private const float cfgNpcHearingRangeLow = 8f;
    private const float cfgNpcHearingRangeMedium = 15f;
    private const float cfgNpcHearingRangeHigh = 25f;
    private const float cfgNpcHearingRangeVeryHigh = 50f;
    private const float cfgNpcHearingRangeInfinite = 9999f;

    // Constants Private: Awareness delays in seconds a target must stay in sight before it is noticed
    private const float cfgNpcAwarenessDelaySlowest = 3f;
    private const float cfgNpcAwarenessDelayVerySlow = 2.5f;
    private const float cfgNpcAwarenessDelaySlow = 2f;
    private const float cfgNpcAwarenessDelayMedium = 1.5f;
    private const float cfgNpcAwarenessDelayFast = 1f;
    private const float cfgNpcAwarenessDelayVeryFast = 0.5f;
    private const float cfgNpcAwarenessDelayExtremelyFast = 0.25f;
    private const float cfgNpcAwarenessDelayInstant = 0f;

    // Constants Private: Combat memory durations in seconds an NPC remembers a target after losing sight of it, or a heard sound it is investigating
    private const float cfgNpcCombatMemoryDurationNone = 0f;
    private const float cfgNpcCombatMemoryDurationVeryShort = 2f;
    private const float cfgNpcCombatMemoryDurationShort = 5f;
    private const float cfgNpcCombatMemoryDurationMedium = 10f;
    private const float cfgNpcCombatMemoryDurationLong = 20f;
    private const float cfgNpcCombatMemoryDurationVeryLong = 30f;
    private const float cfgNpcCombatMemoryDurationInfinite = 9999f;

    // Constants Public: Aiming
    // Degrees per second an engaged NPC turns to face its target with a melee weapon (firearms use the aiming skill profile)
    public const float cfgNpcAimTurnSpeed = 540f;
    // An NPC only attacks when facing within this many degrees of its target
    public const float cfgNpcAimToleranceDegrees = 12f;
    // Degrees per second an NPC turns to track a target it is noticing (before it engages)
    public const float cfgNpcNoticeTurnSpeed = 270f;

    // Constants Public: Attacking
    // Fraction of firearm range an NPC closes to before it counts as in range
    public const float cfgNpcFiringRangeFraction = 0.85f;
    // Fraction of melee range the target must be within before an NPC swings
    public const float cfgNpcMeleeEngageRangeFraction = 0.9f;
    // NPCs prefer their firearm, only switching to melee when the target is within melee range multiplied by this...
    public const float cfgNpcMeleeReadyRangeMultiplier = 1f;
    // ...or within this distance, whichever is larger
    public const float cfgNpcMeleeReadyRangeMin = 0f;
    // An NPC without the stamina for a melee swing switches to its firearm (when it has ammo) and only goes back to
    // melee once its stamina is this much above the swing cost, so it doesn't flicker between weapons
    public const float cfgNpcMeleeStaminaResumeMargin = 20f;
    // Seconds between dry-fire clicks for NPCs that keep pulling the trigger on an empty firearm
    public const float cfgNpcDryFireInterval = 0.75f;

    // Constants Public: Approaching
    // Within this distance of its target an NPC stops flanking or cover hopping and charges straight in
    public const float cfgNpcChargeDistance = 3f;
    // NPCs only sprint while further than this from their target
    public const float cfgNpcSprintMinDistance = 2f;
    // NPCs stop sprinting when their stamina drops to their melee stamina cost multiplied by this, keeping enough to swing
    public const float cfgNpcSprintStaminaReserveMultiplier = 1f;
    // After stopping, NPCs only sprint again once their stamina is this much above that reserve, so they don't flicker in and out of sprinting
    public const float cfgNpcSprintStaminaResumeMargin = 20f;
    // Degrees around the target, away from the direct line, that a flanking NPC aims for
    public const float cfgNpcFlankAngleDegrees = 75f;
    // Fraction of its current distance a flanking NPC keeps from its target while circling round
    public const float cfgNpcFlankDistanceFraction = 0.7f;

    // Constants Public: Obstacle breaking (see enumNpcPersonalityCombatWallBehaviour)
    // How far in front of the obstacle's surface, towards itself, an NPC heads when moving up to break it
    public const float cfgNpcObstacleApproachOffset = 0.5f;

    // Constants Public: Cover
    // Furthest distance from itself an NPC looks for cover
    public const float cfgNpcCoverSearchRadius = 6f;
    // How many rings of points are tested when searching for cover
    public const int cfgNpcCoverSearchRings = 3;
    // How many points are tested on each cover search ring
    public const int cfgNpcCoverSearchPointsPerRing = 10;
    // Cover closer than this to the target is ignored
    public const float cfgNpcCoverMinTargetDistance = 2f;
    // When cover hopping, the next cover must be at least this much closer to the target
    public const float cfgNpcCoverHopMinAdvance = 1f;
    // Furthest distance from itself an NPC looks for a spot to peek out and shoot from
    public const float cfgNpcPeekSearchRadius = 3f;
    // How many rings of points are tested when searching for a peek spot
    public const int cfgNpcPeekSearchRings = 2;
    // How many points are tested on each peek search ring
    public const int cfgNpcPeekSearchPointsPerRing = 8;

    // Constants Public: Friendly fire
    // Radius swept along an NPC's line of fire; a friendly character within it counts as in the way
    public const float cfgNpcFriendlyFireClearanceRadius = 0.3f;
    // Seconds an NPC sticks to melee after switching to it because a friendly blocked its shot and it found no clear shot nearby
    public const float cfgNpcFriendlyFireMeleeModeDuration = 3f;

    // Constants Public: Body guards
    // Seconds between a body guard's checks for who it should guard
    public const float cfgNpcGuardWardScanInterval = 0.5f;
    // Furthest distance a body guard looks for an ally to guard; it lets go of an ally further than this
    public const float cfgNpcGuardWardSearchRange = 15f;
    // A friendly player within this distance always becomes the body guard's focus
    public const float cfgNpcGuardPlayerPriorityRange = 10f;
    // A body guard catching up sprints (when it can sprint) while further than this from its guarded ally
    public const float cfgNpcGuardSprintDistance = 4f;
    // A body guard catching up heads for this fraction of its follow distance from its guarded ally, so it doesn't stop and start at the edge
    public const float cfgNpcGuardFollowArriveFraction = 0.6f;
    // A wandering body guard picks wander points within this fraction of its follow distance from its guarded ally
    public const float cfgNpcGuardWanderRangeFraction = 0.6f;

    // Constants Private: Body guard profile values (follow and leash distances in units)
    private const bool cfgNpcProfileGuardsAllyNone = false;
    private const bool cfgNpcProfileGuardWandersNearWardNone = false;
    private const float cfgNpcProfileGuardFollowDistanceNone = 0f;
    private const float cfgNpcProfileGuardFollowDistanceClose = 1.5f;
    private const float cfgNpcProfileGuardFollowDistanceLoose = 5f;
    private const float cfgNpcProfileGuardLeashDistanceClose = 5f;
    // No leash: the guard fights freely and only follows again once the threat has gone
    private const float cfgNpcProfileGuardLeashDistanceUnlimited = float.MaxValue;

    // Constants Private: Hearing profile values (whether a personality leaves what it is doing to investigate hostile sounds)
    private const bool cfgNpcProfileInvestigatesSoundsDefault = true;
    // A close body guard never leaves its ally to chase a sound
    private const bool cfgNpcProfileInvestigatesSoundsBodyGuardClose = false;
    private const bool cfgNpcProfileInvestigatesSoundsBodyGuardLoose = true;

    // Constants Private: Profile values (the personality profiles below are built from these)
    private const float cfgNpcProfileCoverHoldTimeBalanced = 1.5f;
    private const float cfgNpcProfileCoverHoldTimeDefensive = 3f;
    private const float cfgNpcProfileCoverHoldTimeNone = 0f;

    // Constants Private: Aiming skill profile values (turn speed deg/s, max aim error deg, lock-on seconds, fire delay multiplier, lead chance 0-1, friendly fire chance 0-1)
    private const float cfgNpcAimSkillTurnSpeedVeryLow = 120f;
    private const float cfgNpcAimSkillTurnSpeedLow = 200f;
    private const float cfgNpcAimSkillTurnSpeedMedium = 320f;
    private const float cfgNpcAimSkillTurnSpeedHigh = 450f;
    private const float cfgNpcAimSkillTurnSpeedVeryHigh = 720f;
    // Large enough to snap onto the target every physics step
    private const float cfgNpcAimSkillTurnSpeedPerfect = 100000f;
    private const float cfgNpcAimSkillErrorDegreesVeryLow = 20f;
    private const float cfgNpcAimSkillErrorDegreesLow = 12f;
    private const float cfgNpcAimSkillErrorDegreesMedium = 7f;
    private const float cfgNpcAimSkillErrorDegreesHigh = 3.5f;
    private const float cfgNpcAimSkillErrorDegreesVeryHigh = 1.5f;
    private const float cfgNpcAimSkillErrorDegreesPerfect = 0f;
    private const float cfgNpcAimSkillLockOnTimeVeryLow = 1f;
    private const float cfgNpcAimSkillLockOnTimeLow = 0.7f;
    private const float cfgNpcAimSkillLockOnTimeMedium = 0.45f;
    private const float cfgNpcAimSkillLockOnTimeHigh = 0.25f;
    private const float cfgNpcAimSkillLockOnTimeVeryHigh = 0.12f;
    private const float cfgNpcAimSkillLockOnTimePerfect = 0f;
    private const float cfgNpcAimSkillFireDelayMultiplierVeryLow = 2f;
    private const float cfgNpcAimSkillFireDelayMultiplierLow = 1.6f;
    private const float cfgNpcAimSkillFireDelayMultiplierMedium = 1.3f;
    private const float cfgNpcAimSkillFireDelayMultiplierHigh = 1.15f;
    private const float cfgNpcAimSkillFireDelayMultiplierVeryHigh = 1.05f;
    private const float cfgNpcAimSkillFireDelayMultiplierPerfect = 1f;
    private const float cfgNpcAimSkillLeadChanceVeryLow = 0f;
    private const float cfgNpcAimSkillLeadChanceLow = 0.25f;
    private const float cfgNpcAimSkillLeadChanceMedium = 0.5f;
    private const float cfgNpcAimSkillLeadChanceHigh = 0.67f;
    private const float cfgNpcAimSkillLeadChanceVeryHigh = 0.83f;
    private const float cfgNpcAimSkillLeadChancePerfect = 1f;
    private const float cfgNpcAimSkillFriendlyFireChanceVeryLow = 0.5f;
    private const float cfgNpcAimSkillFriendlyFireChanceLow = 0.25f;
    private const float cfgNpcAimSkillFriendlyFireChanceMedium = 0.05f;
    private const float cfgNpcAimSkillFriendlyFireChanceHigh = 0f;
    private const float cfgNpcAimSkillFriendlyFireChanceVeryHigh = 0f;
    private const float cfgNpcAimSkillFriendlyFireChancePerfect = 0f;

    /// <summary>Returns the firearm aiming profile for an aiming skill level.</summary>
    /// <param name="aimingSkill">The NPC's aiming skill.</param>
    public static F_NpcAimingProfile GetAimingProfile(enumNpcPersonalityCombatSkillAiming aimingSkill)
    {
        switch (aimingSkill)
        {
            case enumNpcPersonalityCombatSkillAiming.VeryLow:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedVeryLow, cfgNpcAimSkillErrorDegreesVeryLow,
                    cfgNpcAimSkillLockOnTimeVeryLow, cfgNpcAimSkillFireDelayMultiplierVeryLow, cfgNpcAimSkillLeadChanceVeryLow,
                    cfgNpcAimSkillFriendlyFireChanceVeryLow);
            case enumNpcPersonalityCombatSkillAiming.Low:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedLow, cfgNpcAimSkillErrorDegreesLow,
                    cfgNpcAimSkillLockOnTimeLow, cfgNpcAimSkillFireDelayMultiplierLow, cfgNpcAimSkillLeadChanceLow,
                    cfgNpcAimSkillFriendlyFireChanceLow);
            case enumNpcPersonalityCombatSkillAiming.High:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedHigh, cfgNpcAimSkillErrorDegreesHigh,
                    cfgNpcAimSkillLockOnTimeHigh, cfgNpcAimSkillFireDelayMultiplierHigh, cfgNpcAimSkillLeadChanceHigh,
                    cfgNpcAimSkillFriendlyFireChanceHigh);
            case enumNpcPersonalityCombatSkillAiming.VeryHigh:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedVeryHigh, cfgNpcAimSkillErrorDegreesVeryHigh,
                    cfgNpcAimSkillLockOnTimeVeryHigh, cfgNpcAimSkillFireDelayMultiplierVeryHigh, cfgNpcAimSkillLeadChanceVeryHigh,
                    cfgNpcAimSkillFriendlyFireChanceVeryHigh);
            case enumNpcPersonalityCombatSkillAiming.Perfect:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedPerfect, cfgNpcAimSkillErrorDegreesPerfect,
                    cfgNpcAimSkillLockOnTimePerfect, cfgNpcAimSkillFireDelayMultiplierPerfect, cfgNpcAimSkillLeadChancePerfect,
                    cfgNpcAimSkillFriendlyFireChancePerfect);
            default:
                return new F_NpcAimingProfile(cfgNpcAimSkillTurnSpeedMedium, cfgNpcAimSkillErrorDegreesMedium,
                    cfgNpcAimSkillLockOnTimeMedium, cfgNpcAimSkillFireDelayMultiplierMedium, cfgNpcAimSkillLeadChanceMedium,
                    cfgNpcAimSkillFriendlyFireChanceMedium);
        }
    }

    /// <summary>Returns how far, in world units, an NPC with a sight range can see.</summary>
    /// <param name="sightRange">The NPC's sight range.</param>
    public static float GetSightRange(enumNpcPersonalityCombatSightRange sightRange)
    {
        switch (sightRange)
        {
            case enumNpcPersonalityCombatSightRange.Blind:
                return cfgNpcSightRangeBlind;
            case enumNpcPersonalityCombatSightRange.VeryClose:
                return cfgNpcSightRangeVeryClose;
            case enumNpcPersonalityCombatSightRange.Close:
                return cfgNpcSightRangeClose;
            case enumNpcPersonalityCombatSightRange.Far:
                return cfgNpcSightRangeFar;
            case enumNpcPersonalityCombatSightRange.VeryFar:
                return cfgNpcSightRangeVeryFar;
            case enumNpcPersonalityCombatSightRange.ExtremelyFar:
                return cfgNpcSightRangeExtremelyFar;
            case enumNpcPersonalityCombatSightRange.InfinitelyFar:
                return cfgNpcSightRangeInfinitelyFar;
            default:
                return cfgNpcSightRangeMedium;
        }
    }

    /// <summary>Returns how far, in world units, an NPC with a hearing level can hear.</summary>
    /// <param name="hearing">The NPC's hearing.</param>
    public static float GetHearingRange(enumNpcPersonalityCombatHearing hearing)
    {
        switch (hearing)
        {
            case enumNpcPersonalityCombatHearing.Deaf:
                return cfgNpcHearingRangeDeaf;
            case enumNpcPersonalityCombatHearing.VeryLow:
                return cfgNpcHearingRangeVeryLow;
            case enumNpcPersonalityCombatHearing.Low:
                return cfgNpcHearingRangeLow;
            case enumNpcPersonalityCombatHearing.High:
                return cfgNpcHearingRangeHigh;
            case enumNpcPersonalityCombatHearing.VeryHigh:
                return cfgNpcHearingRangeVeryHigh;
            case enumNpcPersonalityCombatHearing.Infinite:
                return cfgNpcHearingRangeInfinite;
            default:
                return cfgNpcHearingRangeMedium;
        }
    }

    /// <summary>Returns how many seconds a target must stay in sight before an NPC with an awareness level notices it.</summary>
    /// <param name="awareness">The NPC's awareness.</param>
    public static float GetAwarenessDelay(enumNpcPersonalityCombatAwareness awareness)
    {
        switch (awareness)
        {
            case enumNpcPersonalityCombatAwareness.Slowest:
                return cfgNpcAwarenessDelaySlowest;
            case enumNpcPersonalityCombatAwareness.VerySlow:
                return cfgNpcAwarenessDelayVerySlow;
            case enumNpcPersonalityCombatAwareness.Slow:
                return cfgNpcAwarenessDelaySlow;
            case enumNpcPersonalityCombatAwareness.Fast:
                return cfgNpcAwarenessDelayFast;
            case enumNpcPersonalityCombatAwareness.VeryFast:
                return cfgNpcAwarenessDelayVeryFast;
            case enumNpcPersonalityCombatAwareness.ExtremelyFast:
                return cfgNpcAwarenessDelayExtremelyFast;
            case enumNpcPersonalityCombatAwareness.Instant:
                return cfgNpcAwarenessDelayInstant;
            default:
                return cfgNpcAwarenessDelayMedium;
        }
    }

    /// <summary>Returns how many seconds an engaged NPC with a combat memory remembers a target after losing sight of it.</summary>
    /// <param name="combatMemory">The NPC's combat memory.</param>
    public static float GetCombatMemoryDuration(enumNpcPersonalityCombatMemory combatMemory)
    {
        switch (combatMemory)
        {
            case enumNpcPersonalityCombatMemory.None:
                return cfgNpcCombatMemoryDurationNone;
            case enumNpcPersonalityCombatMemory.VeryShort:
                return cfgNpcCombatMemoryDurationVeryShort;
            case enumNpcPersonalityCombatMemory.Short:
                return cfgNpcCombatMemoryDurationShort;
            case enumNpcPersonalityCombatMemory.Long:
                return cfgNpcCombatMemoryDurationLong;
            case enumNpcPersonalityCombatMemory.VeryLong:
                return cfgNpcCombatMemoryDurationVeryLong;
            case enumNpcPersonalityCombatMemory.Infinite:
                return cfgNpcCombatMemoryDurationInfinite;
            default:
                return cfgNpcCombatMemoryDurationMedium;
        }
    }

    /// <summary>Returns the behaviour profile for a combat personality.</summary>
    /// <param name="personality">The NPC's combat personality.</param>
    public static F_NpcCombatProfile GetCombatProfile(enumNpcPersonalityCombatApproach personality)
    {
        switch (personality)
        {
            case enumNpcPersonalityCombatApproach.ChargerAggressive:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.Direct, enumNpcApproachMode.Direct,
                    false, false, false, false, cfgNpcProfileCoverHoldTimeNone, true, false, false,
                    cfgNpcProfileGuardsAllyNone, cfgNpcProfileGuardFollowDistanceNone, cfgNpcProfileGuardLeashDistanceUnlimited, cfgNpcProfileGuardWandersNearWardNone,
                    cfgNpcProfileInvestigatesSoundsDefault);
            case enumNpcPersonalityCombatApproach.ChargerFlanker:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.Flank, enumNpcApproachMode.Flank,
                    false, true, false, false, cfgNpcProfileCoverHoldTimeNone, true, false, false,
                    cfgNpcProfileGuardsAllyNone, cfgNpcProfileGuardFollowDistanceNone, cfgNpcProfileGuardLeashDistanceUnlimited, cfgNpcProfileGuardWandersNearWardNone,
                    cfgNpcProfileInvestigatesSoundsDefault);
            case enumNpcPersonalityCombatApproach.DefensiveMobile:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.None, enumNpcApproachMode.None,
                    true, true, true, true, cfgNpcProfileCoverHoldTimeDefensive, false, false, false,
                    cfgNpcProfileGuardsAllyNone, cfgNpcProfileGuardFollowDistanceNone, cfgNpcProfileGuardLeashDistanceUnlimited, cfgNpcProfileGuardWandersNearWardNone,
                    cfgNpcProfileInvestigatesSoundsDefault);
            case enumNpcPersonalityCombatApproach.DefensiveStationary:
                return new F_NpcCombatProfile(false, enumNpcApproachMode.None, enumNpcApproachMode.None,
                    true, false, false, false, cfgNpcProfileCoverHoldTimeNone, false, true, true,
                    cfgNpcProfileGuardsAllyNone, cfgNpcProfileGuardFollowDistanceNone, cfgNpcProfileGuardLeashDistanceUnlimited, cfgNpcProfileGuardWandersNearWardNone,
                    cfgNpcProfileInvestigatesSoundsDefault);
            // Sticks to its ally: holds and shoots beside it, only closes in for melee within its short leash, never leaves for cover, never wanders
            case enumNpcPersonalityCombatApproach.BodyGuardClose:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.None, enumNpcApproachMode.Direct,
                    true, false, false, false, cfgNpcProfileCoverHoldTimeNone, true, false, false,
                    true, cfgNpcProfileGuardFollowDistanceClose, cfgNpcProfileGuardLeashDistanceClose, false,
                    cfgNpcProfileInvestigatesSoundsBodyGuardClose);
            // Wanders near its ally, fights on its own with a defensive focus (cover, peeking, cover hopping), then returns to its ally once the threat has gone
            case enumNpcPersonalityCombatApproach.BodyGuardLoose:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.None, enumNpcApproachMode.CoverHop,
                    true, true, true, true, cfgNpcProfileCoverHoldTimeBalanced, false, false, false,
                    true, cfgNpcProfileGuardFollowDistanceLoose, cfgNpcProfileGuardLeashDistanceUnlimited, true,
                    cfgNpcProfileInvestigatesSoundsBodyGuardLoose);
            default:
                return new F_NpcCombatProfile(true, enumNpcApproachMode.Direct, enumNpcApproachMode.CoverHop,
                    true, true, true, false, cfgNpcProfileCoverHoldTimeBalanced, true, false, false,
                    cfgNpcProfileGuardsAllyNone, cfgNpcProfileGuardFollowDistanceNone, cfgNpcProfileGuardLeashDistanceUnlimited, cfgNpcProfileGuardWandersNearWardNone,
                    cfgNpcProfileInvestigatesSoundsDefault);
        }
    }
}
