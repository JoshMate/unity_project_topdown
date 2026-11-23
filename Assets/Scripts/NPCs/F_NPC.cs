using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// Generic base for all NPCs. Wanders around its spawn point, targets the closest character its faction is hostile to
/// when it can see it (see F_Utility_Config_Factions), then fights using
/// its equipped weapons and combat personality. While idle it investigates hostile sounds it hears (see
/// npcPersonalityCombatHearing and F_Logic_Audio.OnWorldSoundPlayed). Weapons work exactly like the player's (deploy, fire rate, reloads,
/// stamina, durability) but firearms reload from the virtual ammo count instead of an inventory. Uses the NavMesh to plan
/// paths while its Rigidbody2D stays in charge of movement and collisions.
/// New NPC types are prefabs with tuned values; subclass only for genuinely new behaviour.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(NavMeshAgent), typeof(CircleCollider2D))]
public class F_NPC : F_Character, IF_Item_Weapon_AmmoReserve
{
    [Header("Constants Private")]
    private const float npcDefaultIdleWanderRange = 4f;
    private const float npcDefaultWanderWaitTimeMin = 1f;
    private const float npcDefaultWanderWaitTimeMax = 3f;
    private const float npcDefaultWanderSpeedMultiplier = 0.5f;
    private const float npcDefaultRepathInterval = 0.25f;
    private const float npcDefaultStoppingDistance = 0.4f;
    private const float npcDefaultRotationOffset = 0f;
    private const int npcDefaultEquippedWeaponAmmoCount = 30;
    private const float npcIdleWanderRangeStandStill = 0f;
    // Below this speed the NPC counts as standing still and keeps its current facing
    private const float npcVelocityStopThreshold = 0.05f;
    // Sight cone slider limits in degrees, and the default that sees all around
    private const float npcSightConeSizeMin = 0f;
    private const float npcSightConeSizeMax = 360f;
    private const float npcDefaultSightConeSize = npcSightConeSizeMax;

    [Header("NPC Equipped Weapons")]
    // Firearm prefab this NPC spawns holding; leave empty for none
    public F_Item_Weapon_Firearm npcEquippedWeaponFirearm;
    // Virtual reserve rounds the firearm reloads from, instead of inventory ammo items; counts down as it reloads
    [Min(0)]
    public int npcEquippedWeaponAmmoCount = npcDefaultEquippedWeaponAmmoCount;
    // Melee weapon prefab this NPC spawns holding; leave empty for none
    public F_Item_Weapon_Melee npcEquippedWeaponMelee;
    // Unarmed attack (such as fists or claws) used just like the melee weapon, but only when there is no melee weapon or it is broken; leave empty for none
    public F_Item_Weapon_Melee npcEquippedWeaponUnarmed;
    

    [Header("NPC Personality")]
    // How this NPC fights once it notices a target (see enumNpcPersonalityCombatApproach and F_Utility_Config_Npc)
    [FormerlySerializedAs("npcPersonalityCombat")]
    public enumNpcPersonalityCombatApproach npcPersonalityCombatApproach = enumNpcPersonalityCombatApproach.Balanced;
    // How well this NPC aims and fires firearms (see enumNpcPersonalityCombatSkillAiming and F_Utility_Config_Npc)
    public enumNpcPersonalityCombatSkillAiming npcPersonalityCombatSkillAiming = enumNpcPersonalityCombatSkillAiming.Medium;
    // How far this NPC can see in line of sight; it notices targets within this range and loses sight of them beyond it multiplied by cfgNpcSightLoseTargetRangeMultiplier
    public enumNpcPersonalityCombatSightRange npcPersonalityCombatSightRange = enumNpcPersonalityCombatSightRange.VeryClose;
    // How far this NPC can hear; idle, it investigates hostile sounds within both this range and the sound's own loudness radius (see F_Utility_Config_Npc)
    public enumNpcPersonalityCombatHearing npcPersonalityCombatHearing = enumNpcPersonalityCombatHearing.Medium;
    // How long a target must stay in this NPC's sight before it is noticed; breaking sight starts the wait again (see F_Utility_Config_Npc)
    [FormerlySerializedAs("npcPersonalityCombatAwarenessLevel")]
    public enumNpcPersonalityCombatAwareness npcPersonalityCombatAwareness = enumNpcPersonalityCombatAwareness.Instant;
    // How long this NPC remembers its target after losing sight of it, searching where it was last seen before forgetting it (see F_Utility_Config_Npc)
    public enumNpcPersonalityCombatMemory npcPersonalityCombatMemory = enumNpcPersonalityCombatMemory.Medium;
    // Whether this NPC attacks walls and obstacles between it and its target or a heard sound, or only to escape when trapped (see enumNpcPersonalityCombatWallBehaviour)
    public enumNpcPersonalityCombatWallBehaviour npcPersonalityCombatWallBehaviour = enumNpcPersonalityCombatWallBehaviour.NeverBreakAnything;
    // How wide, in degrees centred on its facing, this NPC can spot targets: 360 sees all around (even behind), lower values only see what is in front
    [Range(npcSightConeSizeMin, npcSightConeSizeMax)]
    public float npcPersonalityCombatLOSConeSize = npcDefaultSightConeSize;
    
    [Header("NPC Wander Stats")]
    // How far from its spawn point this NPC randomly wanders when it has no target (0 = stands still)
    [FormerlySerializedAs("npcStatWanderRadius")]
    [Min(npcIdleWanderRangeStandStill)]
    public float npcIdleWanderRange = npcDefaultIdleWanderRange;
    // Shortest pause in seconds between wander destinations
    public float npcStatWanderWaitTimeMin = npcDefaultWanderWaitTimeMin;
    // Longest pause in seconds between wander destinations
    public float npcStatWanderWaitTimeMax = npcDefaultWanderWaitTimeMax;
    // Fraction of the current move speed used while wandering
    public float npcStatWanderSpeedMultiplier = npcDefaultWanderSpeedMultiplier;

    [Header("NPC Movement Stats")]
    // Seconds between chase destination updates and combat decisions
    public float npcStatRepathInterval = npcDefaultRepathInterval;
    // How close this NPC gets to its destination before stopping
    public float npcStatStoppingDistance = npcDefaultStoppingDistance;
    // Sprite facing offset in degrees, added to the movement direction angle
    public float npcStatRotationOffset = npcDefaultRotationOffset;

    [Header("NPC Status Flags")]
    // What this NPC is currently doing (shown for debugging)
    public enumNpcState npcStatusState = enumNpcState.Idle;

    [Header("Privates")]
    private NavMeshAgent npcAgent;
    private Rigidbody2D npcRigidbody;
    private Vector2 npcSpawnPosition;
    private Transform npcTarget;
    private F_Ent npcTargetEnt;
    // The target's Rigidbody2D, read for its velocity when leading shots; null when it has none
    private Rigidbody2D npcTargetRigidbody;
    private float npcNextWanderTime;
    private float npcNextRepathTime;
    private float npcNextTargetScanTime;
    // Combat
    private F_Character_HeldWeapon npcHeldWeapon;
    private F_Item_Weapon_Melee npcMeleeInstance;
    private F_Item_Weapon_Melee npcUnarmedInstance;
    private F_Item_Weapon_Firearm npcFirearmInstance;
    private enumNpcWeaponChoice npcWeaponChoice = enumNpcWeaponChoice.None;
    private bool npcIsEngaged;
    private bool npcCanSeeTarget;
    private bool npcHasLineOfFire;
    private float npcTargetDistance;
    // Distance from this NPC's centre to the edge of the target's body, used for melee reach
    private float npcTargetReachDistance;
    private Collider2D[] npcTargetColliders;
    private float npcNextWeaponSwitchTime;
    // Whether the NPC ran out of stamina for melee and is waiting to recover before going back to it
    private bool npcIsMeleeExhausted;
    private float npcNextDryFireTime;
    private float npcCoverUntilTime;
    private float npcFlankSide = 1f;
    private bool npcIsSprintRequested;
    // Firearm aiming
    private bool npcIsAimTracking;
    private bool npcIsAimLeading;
    private float npcAimErrorDegrees;
    private float npcAimLockedOnTime;
    private float npcNextFirearmShotTime;
    // Friendly fire
    private bool npcIsFriendlyInLineOfFire;
    // Whether the current roll lets this NPC fire through a friendly in its line of fire
    private bool npcIsFriendlyFireShotAllowed;
    private float npcFriendlyFireMeleeUntilTime;
    // Body guard
    private F_Character npcGuardWard;
    private float npcNextGuardWardScanTime;
    // Awareness
    private bool npcIsNoticingTarget;
    // When the target came into sight, while it is being noticed
    private float npcNoticeStartTime;
    // Memory: where and when the target was last seen (or last revealed itself by attacking)
    private Vector2 npcTargetLastSeenPosition;
    private float npcTargetLastSeenTime;
    // Hearing: investigating the latest hostile sound heard while idle; a newer sound replaces it
    private bool npcIsInvestigating;
    private Vector2 npcInvestigatePosition;
    // When the sound was heard; the NPC remembers it (and keeps investigating) for its combat memory duration
    private float npcInvestigateHeardTime;
    // Whether it has reached the sound and is looking around, and since when
    private bool npcHasReachedInvestigatePosition;
    private float npcInvestigateArrivedTime;
    // Multiple hostiles: every hostile this NPC knows about while engaged, so it moves straight on to the next one
    private readonly Dictionary<F_Character, NpcKnownHostile> npcKnownHostiles = new Dictionary<F_Character, NpcKnownHostile>();
    // Reusable buffers for combat target selection, so scans don't allocate
    private readonly List<F_Character> npcVisibleHostiles = new List<F_Character>();
    private readonly List<F_Character> npcForgottenHostiles = new List<F_Character>();
    // Thought icon shown above this NPC's head
    private F_NPC_Thought npcThought;
    // Obstacle breaking: the wall or obstacle attacked instead of the target, or null when none
    private F_Ent npcObstacleTarget;
    private Collider2D[] npcObstacleColliders;
    // Closest point on the obstacle to this NPC, which it aims at and moves towards
    private Vector2 npcObstaclePoint;
    private float npcObstacleReachDistance;
    private bool npcHasLineOfFireToObstacle;
    // Reusable path for checking whether any route still reaches the target
    private NavMeshPath npcTrappedCheckPath;

    // Where and when a known hostile was last seen (or last revealed itself by attacking).
    private struct NpcKnownHostile
    {
        public Vector2 knownHostileLastSeenPosition;
        public float knownHostileLastSeenTime;
    }

    /// <summary>What this NPC is currently doing.</summary>
    public enumNpcState NpcState => npcStatusState;

    /// <summary>Whether this NPC's personality guards an ally.</summary>
    public bool IsBodyGuard => CombatProfile.profileGuardsAlly;

    /// <summary>The ally this NPC is guarding, or null when it guards no one.</summary>
    public F_Character GuardWard => npcGuardWard;

    /// <summary>Whether this NPC has noticed its target and is fighting it.</summary>
    public bool IsEngaged => npcIsEngaged;

    // Whether the agent can be queried and given destinations.
    private bool IsAgentUsable => npcAgent != null && npcAgent.enabled && npcAgent.isOnNavMesh;

    // The behaviour profile for the current personality; read live so it can be changed at runtime.
    private F_NpcCombatProfile CombatProfile => F_Utility_Config_Npc.GetCombatProfile(npcPersonalityCombatApproach);

    // The firearm aiming profile for the current aiming skill; read live so it can be changed at runtime.
    private F_NpcAimingProfile AimingProfile => F_Utility_Config_Npc.GetAimingProfile(npcPersonalityCombatSkillAiming);

    // How far this NPC notices targets, from its sight range; read live so it can be changed at runtime.
    private float SightRange => F_Utility_Config_Npc.GetSightRange(npcPersonalityCombatSightRange);

    // Beyond this distance an NPC loses sight of its target.
    private float LoseTargetRange => SightRange * F_Utility_Config_Npc.cfgNpcSightLoseTargetRangeMultiplier;

    // How far this NPC hears sounds, from its hearing level; read live so it can be changed at runtime.
    private float HearingRange => F_Utility_Config_Npc.GetHearingRange(npcPersonalityCombatHearing);

    // Seconds a target must stay in sight before this NPC notices it; read live so it can be changed at runtime.
    private float AwarenessDelay => F_Utility_Config_Npc.GetAwarenessDelay(npcPersonalityCombatAwareness);

    // Seconds this NPC remembers its target after losing sight of it; read live so it can be changed at runtime.
    private float CombatMemoryDuration => F_Utility_Config_Npc.GetCombatMemoryDuration(npcPersonalityCombatMemory);

    // The melee weapon the NPC fights with: its melee weapon while usable, otherwise its unarmed attack when it has one.
    private F_Item_Weapon_Melee ActiveMeleeWeapon =>
        IsWeaponUsable(npcMeleeInstance) || npcUnarmedInstance == null ? npcMeleeInstance : npcUnarmedInstance;

    // Whether the NPC is currently holding a firearm, so its aiming skill applies.
    private bool IsHoldingFirearm => npcHeldWeapon != null && npcHeldWeapon.SelectedWeaponItem is F_Item_Weapon_Firearm;

    // The direction the NPC faces in degrees, with the sprite's rotation offset removed.
    private float FacingAngle => npcRigidbody.rotation - npcStatRotationOffset;

    // Whether the NPC may fire at its target now: a clear line, or only friendlies in the way and its skill roll lets it shoot anyway.
    private bool CanShootTarget => npcHasLineOfFire || (npcIsFriendlyInLineOfFire && npcIsFriendlyFireShotAllowed);

    // Whether a friendly is in the line of fire and the NPC is holding its fire because of it.
    private bool IsAvoidingFriendlyFire => npcIsFriendlyInLineOfFire && !npcIsFriendlyFireShotAllowed;

    // Whether the NPC is breaking a wall or obstacle instead of attacking its target directly.
    private bool IsBreakingObstacle => npcObstacleTarget != null;

    // Whether the NPC is using its weapons: fighting its target, or breaking an obstacle in its way (also while investigating).
    private bool IsUsingWeapons => npcIsEngaged || IsBreakingObstacle;

    // The point the NPC attacks: the obstacle it is breaking, otherwise its target.
    private Vector2 AttackPoint => IsBreakingObstacle ? npcObstaclePoint : (Vector2)npcTarget.position;

    // Distance to the edge of whatever the NPC attacks, for melee reach.
    private float AttackReachDistance => IsBreakingObstacle ? npcObstacleReachDistance : npcTargetReachDistance;

    // Distance to whatever the NPC attacks, for firearm range.
    private float AttackDistance => IsBreakingObstacle ? npcObstacleReachDistance : npcTargetDistance;

    // Whether the NPC can see what it attacks (an obstacle being broken is always known).
    private bool CanSeeAttackPoint => IsBreakingObstacle || npcCanSeeTarget;

    // Whether the NPC may fire at what it attacks now.
    private bool CanShootAttackPoint => IsBreakingObstacle ? npcHasLineOfFireToObstacle : CanShootTarget;

    protected override void Awake()
    {
        base.Awake();
        npcRigidbody = GetComponent<Rigidbody2D>();
        npcAgent = GetComponent<NavMeshAgent>();
        // The Rigidbody2D moves the NPC; the agent only plans paths on the XY plane
        npcAgent.updatePosition = false;
        npcAgent.updateRotation = false;
        npcAgent.updateUpAxis = false;
        npcAgent.stoppingDistance = npcStatStoppingDistance;
        npcAgent.enabled = false;
        npcSpawnPosition = npcRigidbody.position;
        npcTrappedCheckPath = new NavMeshPath();
        npcThought = GetComponent<F_NPC_Thought>();
        if (npcThought == null)
        {
            npcThought = gameObject.AddComponent<F_NPC_Thought>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        F_Logic_Navigation.OnNavMeshReady += HandleNavMeshReady;
        OnAnyEntDamaged += HandleAnyEntDamaged;
        F_Logic_Audio.OnWorldSoundPlayed += HandleWorldSoundPlayed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        F_Logic_Navigation.OnNavMeshReady -= HandleNavMeshReady;
        OnAnyEntDamaged -= HandleAnyEntDamaged;
        F_Logic_Audio.OnWorldSoundPlayed -= HandleWorldSoundPlayed;
    }

    protected override void Start()
    {
        base.Start();
        EquipWeapons();

        if (F_Logic_Navigation.IsNavMeshReady)
        {
            TryEnableAgent();
        }
    }

    protected virtual void OnDestroy()
    {
        DestroyEquippedWeapons();
    }

    /// <summary>Sets what this NPC fights when it notices it. Null makes it only wander until it spots a hostile character.</summary>
    /// <param name="target">The transform to fight, or null.</param>
    public void SetTarget(Transform target)
    {
        ChangeTarget(target, false);
    }

    // Swaps the target. A combat switch moves straight on to the new target while staying engaged; otherwise a new
    // target has to be noticed from scratch and a null target disengages.
    private void ChangeTarget(Transform target, bool isCombatSwitch)
    {
        if (target == npcTarget)
        {
            return;
        }

        npcTarget = target;
        // A new target has to be locked onto again
        npcIsAimTracking = false;
        npcIsFriendlyInLineOfFire = false;
        npcFriendlyFireMeleeUntilTime = 0f;
        // A fight re-picks its obstacle for the new target; an investigation's obstacle doesn't depend on the target
        if (npcIsEngaged)
        {
            ClearObstacleTarget();
        }

        if (!isCombatSwitch)
        {
            StopNoticingTarget();
        }

        npcTargetEnt = target != null ? target.GetComponent<F_Ent>() : null;
        npcTargetRigidbody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        npcTargetColliders = target != null ? target.GetComponentsInChildren<Collider2D>() : null;
        if (target == null)
        {
            Disengage();
        }
        else
        {
            // Whatever drew attention to a new target reveals where it is right now
            RememberTargetPosition();
            npcNextRepathTime = 0f;
        }
    }

    /// <summary>Returns the NPC's remaining virtual reserve ammo; NPCs need no ammo items.</summary>
    /// <param name="ammoItem">Ignored: NPC ammo is virtual.</param>
    public int GetReserveAmmoCount(F_Item ammoItem)
    {
        return npcEquippedWeaponAmmoCount;
    }

    /// <summary>Removes up to the requested rounds from the virtual reserve and returns how many were removed.</summary>
    /// <param name="ammoItem">Ignored: NPC ammo is virtual.</param>
    /// <param name="amountToConsume">How many rounds the firearm wants to load.</param>
    public int ConsumeReserveAmmo(F_Item ammoItem, int amountToConsume)
    {
        int consumedAmmo = Mathf.Clamp(amountToConsume, 0, npcEquippedWeaponAmmoCount);
        npcEquippedWeaponAmmoCount -= consumedAmmo;
        return consumedAmmo;
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead)
        {
            return;
        }

        UpdateGuardWard();
        UpdateTargetAwareness();
        UpdateObstacleTarget();
        UpdateAimLock();
        UpdateCombatWeapon();

        if (IsAgentUsable)
        {
            UpdateState();
        }

        UpdateSprint();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            npcRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        bool isAimingAtTarget = IsUsingWeapons && CanSeeAttackPoint;
        bool isTrackingNoticedTarget = !npcIsEngaged && npcIsNoticingTarget;
        if (isAimingAtTarget)
        {
            AimAtTarget();
        }
        else if (isTrackingNoticedTarget)
        {
            TrackNoticedTarget();
        }

        if (!IsAgentUsable)
        {
            npcRigidbody.linearVelocity = KnockbackVelocity;
            return;
        }

        float npcCurrentSpeed = GetStateSpeed();
        npcAgent.speed = npcCurrentSpeed;
        Vector2 npcVelocity = npcCurrentSpeed <= 0f
            ? Vector2.zero
            : Vector2.ClampMagnitude(npcAgent.desiredVelocity, npcCurrentSpeed);

        npcRigidbody.linearVelocity = npcVelocity + KnockbackVelocity;
        npcAgent.nextPosition = npcRigidbody.position;

        if (!isAimingAtTarget && !isTrackingNoticedTarget && npcVelocity.sqrMagnitude > npcVelocityStopThreshold * npcVelocityStopThreshold)
        {
            npcRigidbody.rotation = Mathf.Atan2(npcVelocity.y, npcVelocity.x) * Mathf.Rad2Deg + npcStatRotationOffset;
        }
    }

    // Stops moving and releases the agent and weapons; F_Ent then shatters and destroys the NPC as normal.
    protected override void OnDeath()
    {
        base.OnDeath();
        npcRigidbody.linearVelocity = Vector2.zero;
        npcAgent.enabled = false;
        DestroyEquippedWeapons();
    }

    // Enables the agent once the NavMesh exists at the NPC's position.
    private void HandleNavMeshReady()
    {
        if (!IsDead && npcAgent != null && !npcAgent.enabled)
        {
            TryEnableAgent();
        }
    }

    // Places the agent on the NavMesh at the Rigidbody2D position, if the NavMesh reaches it.
    private void TryEnableAgent()
    {
        if (!F_Utility_Helper_Navigation.IsOnNavMesh(npcRigidbody.position))
        {
            return;
        }

        npcAgent.enabled = true;
        npcAgent.Warp(npcRigidbody.position);
    }

    // ---------- Equipped weapons ----------

    // Spawns the shared held weapon rig and private instances of the equipped weapon prefabs, holding the firearm first.
    private void EquipWeapons()
    {
        if (npcEquippedWeaponMelee == null && npcEquippedWeaponUnarmed == null && npcEquippedWeaponFirearm == null)
        {
            return;
        }

        GameObject heldWeaponRigPrefab = F_Utility_Config_HeldWeapons.GetNpcRigPrefab();
        if (heldWeaponRigPrefab == null)
        {
            Debug.LogWarning($"F_NPC: no NPC held weapon rig is assigned in the held weapon config (cfgHeldWeaponNpcRigPrefab), so {name} can't use its weapons.");
            return;
        }

        // Counter-rotate the rig so its +X follows the aim even when the sprite has a facing offset
        GameObject heldWeaponRig = Instantiate(heldWeaponRigPrefab, transform);
        heldWeaponRig.transform.localPosition = Vector3.zero;
        heldWeaponRig.transform.localRotation = Quaternion.Euler(0f, 0f, -npcStatRotationOffset);
        npcHeldWeapon = heldWeaponRig.GetComponent<F_Character_HeldWeapon>();
        if (npcHeldWeapon == null)
        {
            Debug.LogWarning($"F_NPC: the held weapon rig has no F_Character_HeldWeapon, so {name} can't use its weapons.");
            Destroy(heldWeaponRig);
            return;
        }

        npcMeleeInstance = SpawnEquippedWeapon(npcEquippedWeaponMelee);
        npcUnarmedInstance = SpawnEquippedWeapon(npcEquippedWeaponUnarmed);
        npcFirearmInstance = SpawnEquippedWeapon(npcEquippedWeaponFirearm);
        npcHeldWeapon.SelectWeapon(npcFirearmInstance != null ? (F_Item_Weapon)npcFirearmInstance : ActiveMeleeWeapon);
    }

    // Spawns a private, out-of-world instance of an equipped weapon prefab in this NPC's scene.
    private T SpawnEquippedWeapon<T>(T weaponPrefab) where T : F_Item_Weapon
    {
        return F_Utility_Helper_Weapon.SpawnStoredWeaponInstance(weaponPrefab, gameObject.scene);
    }

    private void DestroyEquippedWeapons()
    {
        if (npcMeleeInstance != null)
        {
            Destroy(npcMeleeInstance.gameObject);
            npcMeleeInstance = null;
        }

        if (npcUnarmedInstance != null)
        {
            Destroy(npcUnarmedInstance.gameObject);
            npcUnarmedInstance = null;
        }

        if (npcFirearmInstance != null)
        {
            Destroy(npcFirearmInstance.gameObject);
            npcFirearmInstance = null;
        }
    }

    // ---------- Target awareness ----------

    // Body guards drop a dead or no longer friendly ward, then every scan interval pick a new one when they have none,
    // the current one is out of range, or a friendly player comes near (the player is always the focus).
    private void UpdateGuardWard()
    {
        if (!IsBodyGuard)
        {
            npcGuardWard = null;
            return;
        }

        bool isWardInvalid = npcGuardWard != null && (npcGuardWard.IsDead || !F_Utility_Helper_Faction.IsFriendly(this, npcGuardWard));
        if (isWardInvalid)
        {
            npcGuardWard = null;
        }

        if (Time.time < npcNextGuardWardScanTime)
        {
            return;
        }

        npcNextGuardWardScanTime = Time.time + F_Utility_Config_Npc.cfgNpcGuardWardScanInterval;
        F_Character bestWard = F_Utility_Helper_Npc.FindGuardWard(this, npcRigidbody.position,
            F_Utility_Config_Npc.cfgNpcGuardWardSearchRange, F_Utility_Config_Npc.cfgNpcGuardPlayerPriorityRange);
        float searchRangeSqr = F_Utility_Config_Npc.cfgNpcGuardWardSearchRange * F_Utility_Config_Npc.cfgNpcGuardWardSearchRange;
        bool isWardOutOfRange = npcGuardWard != null && ((Vector2)npcGuardWard.transform.position - npcRigidbody.position).sqrMagnitude > searchRangeSqr;
        if (npcGuardWard == null || isWardOutOfRange || bestWard is F_PlayerStats)
        {
            npcGuardWard = bestWard;
        }
    }

    // Picks hostile targets, notices the target on sight within detection range, remembers where it was last seen, and
    // forgets it once out of sight for longer than its combat memory or when it dies. While engaged, a lost target is
    // replaced straight away by the next known hostile so the fight carries on without re-noticing anyone.
    private void UpdateTargetAwareness()
    {
        bool isTargetInvalid = npcTarget != null && (npcTargetEnt == null || npcTargetEnt.IsDead || !F_Utility_Helper_Faction.IsHostile(this, npcTargetEnt));
        if (isTargetInvalid && !(npcIsEngaged && TrySelectCombatTarget(true)))
        {
            SetTarget(null);
        }

        UpdateTargetScan();

        if (npcTarget == null)
        {
            Disengage();
            return;
        }

        Vector2 npcPosition = npcRigidbody.position;
        Vector2 targetPosition = npcTarget.position;
        npcTargetDistance = Vector2.Distance(npcPosition, targetPosition);
        npcTargetReachDistance = F_Utility_Helper_Npc.GetDistanceToBody(npcPosition, npcTarget, npcTargetColliders);

        float loseTargetRange = LoseTargetRange;
        npcCanSeeTarget = npcTargetDistance <= loseTargetRange &&
                          F_Utility_Helper_Npc.HasLineOfSight(npcPosition, targetPosition, transform, npcTarget);
        enumNpcLineOfFire lineOfFire = npcCanSeeTarget
            ? F_Utility_Helper_Npc.GetLineOfFireStatus(this, npcPosition, targetPosition, npcTarget)
            : enumNpcLineOfFire.Blocked;
        npcHasLineOfFire = lineOfFire == enumNpcLineOfFire.Clear;

        // Each time a friendly steps into the line of fire, the aiming skill decides whether the NPC notices
        bool isFriendlyInLineOfFire = lineOfFire == enumNpcLineOfFire.BlockedByFriendly;
        if (isFriendlyInLineOfFire && !npcIsFriendlyInLineOfFire)
        {
            RollFriendlyFireShot(AimingProfile);
        }

        npcIsFriendlyInLineOfFire = isFriendlyInLineOfFire;

        if (npcCanSeeTarget)
        {
            RememberTargetPosition();
        }

        if (!npcIsEngaged)
        {
            UpdateNoticeTarget();
        }
        else if (!npcCanSeeTarget && Time.time - npcTargetLastSeenTime > CombatMemoryDuration && !TrySelectCombatTarget(true))
        {
            Disengage();
            SetTarget(null);
        }
    }

    // Records the target's current position as where it was last seen, restarting the combat memory.
    private void RememberTargetPosition()
    {
        npcTargetLastSeenPosition = npcTarget.position;
        npcTargetLastSeenTime = Time.time;
        if (npcIsEngaged && npcTargetEnt is F_Character targetCharacter)
        {
            RememberHostile(targetCharacter);
        }
    }

    // Records where a hostile is right now in this NPC's combat memory.
    private void RememberHostile(F_Character hostile)
    {
        npcKnownHostiles[hostile] = new NpcKnownHostile
        {
            knownHostileLastSeenPosition = hostile.transform.position,
            knownHostileLastSeenTime = Time.time
        };
    }

    // Drops known hostiles that died, stopped being hostile or haven't been seen for longer than the combat memory.
    private void ForgetLostHostiles()
    {
        npcForgottenHostiles.Clear();
        float combatMemoryDuration = CombatMemoryDuration;
        foreach (KeyValuePair<F_Character, NpcKnownHostile> knownHostile in npcKnownHostiles)
        {
            F_Character hostile = knownHostile.Key;
            bool isLost = hostile == null || hostile.IsDead || !F_Utility_Helper_Faction.IsHostile(this, hostile) ||
                          Time.time - knownHostile.Value.knownHostileLastSeenTime > combatMemoryDuration;
            if (isLost)
            {
                npcForgottenHostiles.Add(hostile);
            }
        }

        for (int forgottenIndex = 0; forgottenIndex < npcForgottenHostiles.Count; forgottenIndex++)
        {
            npcKnownHostiles.Remove(npcForgottenHostiles[forgottenIndex]);
        }
    }

    // Updates the combat memory with every hostile in sight (alert to all sides while engaged), then fights the closest
    // one in sight, otherwise the one last seen closest. A target still in sight is only dropped for a visible hostile
    // closer by cfgNpcCombatRetargetCloserMargin, and one out of sight is chased until a visible hostile turns up.
    // Returns false when there is no hostile left to fight.
    private bool TrySelectCombatTarget(bool isCurrentTargetLost)
    {
        Vector2 npcPosition = npcRigidbody.position;
        float combatSightConeSize = Mathf.Max(npcPersonalityCombatLOSConeSize, F_Utility_Config_Npc.cfgNpcCombatAlertSightConeSize);
        F_Utility_Helper_Npc.FindVisibleHostiles(this, npcPosition, LoseTargetRange, FacingAngle, combatSightConeSize, npcVisibleHostiles);
        for (int visibleIndex = 0; visibleIndex < npcVisibleHostiles.Count; visibleIndex++)
        {
            RememberHostile(npcVisibleHostiles[visibleIndex]);
        }

        ForgetLostHostiles();
        Transform excludedTarget = isCurrentTargetLost ? npcTarget : null;
        F_Character bestHostile = FindClosestVisibleHostile(npcPosition, excludedTarget, out float bestHostileDistance);
        bool isBestHostileVisible = bestHostile != null;
        if (!isBestHostileVisible)
        {
            bestHostile = FindClosestRememberedHostile(npcPosition, excludedTarget);
        }

        if (bestHostile == null)
        {
            return false;
        }

        if (bestHostile.transform == npcTarget)
        {
            return true;
        }

        bool isKeepingCurrentTarget = !isCurrentTargetLost && npcTarget != null &&
            (!isBestHostileVisible || (npcCanSeeTarget && bestHostileDistance > npcTargetDistance - F_Utility_Config_Npc.cfgNpcCombatRetargetCloserMargin));
        if (!isKeepingCurrentTarget)
        {
            SwitchCombatTarget(bestHostile);
        }

        return true;
    }

    // The closest hostile seen in the latest combat scan, skipping the excluded target.
    private F_Character FindClosestVisibleHostile(Vector2 npcPosition, Transform excludedTarget, out float closestDistance)
    {
        F_Character closestHostile = null;
        float closestDistanceSqr = float.MaxValue;
        for (int visibleIndex = 0; visibleIndex < npcVisibleHostiles.Count; visibleIndex++)
        {
            F_Character candidate = npcVisibleHostiles[visibleIndex];
            float distanceSqr = ((Vector2)candidate.transform.position - npcPosition).sqrMagnitude;
            if (candidate.IsDead || candidate.transform == excludedTarget || distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            closestHostile = candidate;
        }

        closestDistance = closestHostile != null ? Mathf.Sqrt(closestDistanceSqr) : float.MaxValue;
        return closestHostile;
    }

    // The known hostile whose last seen position is closest, skipping the excluded target.
    private F_Character FindClosestRememberedHostile(Vector2 npcPosition, Transform excludedTarget)
    {
        F_Character closestHostile = null;
        float closestDistanceSqr = float.MaxValue;
        foreach (KeyValuePair<F_Character, NpcKnownHostile> knownHostile in npcKnownHostiles)
        {
            float distanceSqr = (knownHostile.Value.knownHostileLastSeenPosition - npcPosition).sqrMagnitude;
            if (knownHostile.Key.transform == excludedTarget || distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            closestHostile = knownHostile.Key;
        }

        return closestHostile;
    }

    // Moves straight on to another known hostile while staying engaged, carrying over where it was last seen.
    private void SwitchCombatTarget(F_Character hostile)
    {
        ChangeTarget(hostile.transform, true);
        if (npcKnownHostiles.TryGetValue(hostile, out NpcKnownHostile knownHostile))
        {
            npcTargetLastSeenPosition = knownHostile.knownHostileLastSeenPosition;
            npcTargetLastSeenTime = knownHostile.knownHostileLastSeenTime;
        }
    }

    // Engages once the target has stayed in sight within sight range and inside the sight cone for the whole awareness
    // delay; losing sight of it (or it leaving sight range or the cone) starts the wait again.
    private void UpdateNoticeTarget()
    {
        bool isTargetInSight = npcCanSeeTarget && npcTargetDistance <= SightRange &&
                               F_Utility_Helper_Npc.IsInSightCone(npcRigidbody.position, FacingAngle, npcPersonalityCombatLOSConeSize, npcTarget.position);
        if (!isTargetInSight)
        {
            StopNoticingTarget();
            return;
        }

        if (!npcIsNoticingTarget)
        {
            npcIsNoticingTarget = true;
            npcNoticeStartTime = Time.time;
            npcThought.ShowThought(enumNpcThought.Suspicious);
        }

        if (Time.time - npcNoticeStartTime >= AwarenessDelay)
        {
            Engage();
        }
    }

    // Stops the awareness wait and hides the suspicious thought it shows, unless an investigation is still showing it.
    private void StopNoticingTarget()
    {
        npcIsNoticingTarget = false;
        if (!npcIsInvestigating)
        {
            npcThought.ClearThought(enumNpcThought.Suspicious);
        }
    }

    // Every scan interval: while engaged, re-picks the closest known hostile; while idle, switches to the closest visible hostile character.
    private void UpdateTargetScan()
    {
        if (Time.time < npcNextTargetScanTime)
        {
            return;
        }

        npcNextTargetScanTime = Time.time + F_Utility_Config_Npc.cfgNpcTargetScanInterval;
        if (npcIsEngaged)
        {
            TrySelectCombatTarget(false);
            return;
        }

        F_Character closestHostile = F_Utility_Helper_Npc.FindClosestVisibleHostile(this, npcRigidbody.position, SightRange,
            FacingAngle, npcPersonalityCombatLOSConeSize);
        SetTarget(closestHostile != null ? closestHostile.transform : null);
    }

    // Being hit by a hostile character (or a body guard's ward being hit) makes the NPC notice it (switching to it when
    // idle or unable to see its target, otherwise remembering it to deal with next), and sends cover-seeking
    // personalities to cover when it was the one hit. Hits from allies and neutrals are ignored.
    private void HandleAnyEntDamaged(F_Ent damagedEnt, float damageAmount, enumDamageType damageType, GameObject damageSource)
    {
        bool isSelfDamaged = damagedEnt == this;
        bool isWardDamaged = npcGuardWard != null && damagedEnt == npcGuardWard;
        if ((!isSelfDamaged && !isWardDamaged) || IsDead)
        {
            return;
        }

        F_Character attackerCharacter = F_Utility_Helper_Faction.GetSourceCharacter(damageSource);
        if (attackerCharacter == null || attackerCharacter == this || attackerCharacter.IsDead || !F_Utility_Helper_Faction.IsHostile(this, attackerCharacter))
        {
            return;
        }

        if (attackerCharacter.transform != npcTarget && (!npcIsEngaged || !npcCanSeeTarget))
        {
            ChangeTarget(attackerCharacter.transform, npcIsEngaged);
        }

        if (attackerCharacter.transform != npcTarget)
        {
            // Busy with a target in sight: remember the attacker so the closest-first scan can deal with it
            RememberHostile(attackerCharacter);
            return;
        }

        // Attacking reveals where the target is, even when it can't be seen
        RememberTargetPosition();

        if (!npcIsEngaged)
        {
            Engage();
        }

        F_NpcCombatProfile combatProfile = CombatProfile;
        if (isSelfDamaged && combatProfile.profileCoverWhenHit)
        {
            npcCoverUntilTime = Time.time + combatProfile.profileCoverHoldTime;
            npcNextRepathTime = 0f;
        }
    }

    // ---------- Hearing ----------

    // An idle NPC investigates a hostile character's sound when it is within both its hearing range and the sound's
    // loudness radius; a newer sound replaces the one it was investigating. Targets in sight come first, so sounds are
    // ignored while noticing or fighting. Sounds made by allies, neutrals or nobody are ignored, as are all sounds for
    // personalities whose combat profile doesn't investigate sounds (e.g. close body guards).
    private void HandleWorldSoundPlayed(Vector2 soundPosition, float soundRadius, F_Character soundOwner)
    {
        if (IsDead || npcIsEngaged || npcIsNoticingTarget || !CombatProfile.profileInvestigatesSounds)
        {
            return;
        }

        if (soundOwner == null || soundOwner == this || soundOwner.IsDead || !F_Utility_Helper_Faction.IsHostile(this, soundOwner))
        {
            return;
        }

        float audibleRange = Mathf.Min(soundRadius, HearingRange);
        if (audibleRange <= 0f || (soundPosition - npcRigidbody.position).sqrMagnitude > audibleRange * audibleRange)
        {
            return;
        }

        StartInvestigating(soundPosition);
    }

    // Heads for a heard sound with a question mark above its head, replacing any previous investigation.
    private void StartInvestigating(Vector2 soundPosition)
    {
        npcIsInvestigating = true;
        npcInvestigatePosition = soundPosition;
        npcInvestigateHeardTime = Time.time;
        npcHasReachedInvestigatePosition = false;
        npcNextRepathTime = 0f;
        npcThought.ShowThought(enumNpcThought.Suspicious);
    }

    // Ends the investigation and hides its question mark, unless a target in sight is still being noticed.
    private void StopInvestigating()
    {
        if (!npcIsInvestigating)
        {
            return;
        }

        npcIsInvestigating = false;
        if (!npcIsNoticingTarget)
        {
            npcThought.ClearThought(enumNpcThought.Suspicious);
        }
    }

    private void Engage()
    {
        npcIsEngaged = true;
        StopInvestigating();
        StopNoticingTarget();
        // The noticed target is the first entry in the combat memory
        RememberTargetPosition();
        npcThought.ShowThought(enumNpcThought.Alerted);
        npcNextRepathTime = 0f;
        F_NpcCombatProfile combatProfile = CombatProfile;
        if (combatProfile.profileCoverWhenNoticed)
        {
            npcCoverUntilTime = Time.time + combatProfile.profileCoverHoldTime;
        }
    }

    private void Disengage()
    {
        npcCanSeeTarget = false;
        npcHasLineOfFire = false;
        npcIsFriendlyInLineOfFire = false;
        StopNoticingTarget();
        if (!npcIsEngaged)
        {
            return;
        }

        // Only the fight's obstacle is dropped; an investigation keeps breaking towards its sound without a target
        ClearObstacleTarget();
        npcIsEngaged = false;
        npcKnownHostiles.Clear();
        npcThought.ClearThought(enumNpcThought.Alerted);
        npcCoverUntilTime = 0f;
        npcIsSprintRequested = false;
        if (IsAgentUsable)
        {
            StopAndWait();
        }
    }

    // ---------- Weapon use ----------

    // Picks, readies and uses the right weapon while engaged or breaking an obstacle; otherwise holds the firearm and tops it up.
    private void UpdateCombatWeapon()
    {
        if (npcHeldWeapon == null)
        {
            npcWeaponChoice = enumNpcWeaponChoice.None;
            return;
        }

        if (!IsUsingWeapons)
        {
            npcWeaponChoice = enumNpcWeaponChoice.None;
            HoldWeapon(IsWeaponUsable(npcFirearmInstance) ? (F_Item_Weapon)npcFirearmInstance : ActiveMeleeWeapon);
            ReloadHeldFirearmIfNotFull();
            return;
        }

        F_NpcCombatProfile combatProfile = CombatProfile;
        npcWeaponChoice = ChooseWeapon(combatProfile);
        if (npcWeaponChoice == enumNpcWeaponChoice.Firearm)
        {
            HoldWeapon(npcFirearmInstance);
        }
        else if (npcWeaponChoice == enumNpcWeaponChoice.Melee)
        {
            HoldWeapon(ActiveMeleeWeapon);
        }

        F_Item_Weapon heldWeapon = npcHeldWeapon.SelectedWeaponItem;
        if (heldWeapon is F_Item_Weapon_Firearm heldFirearm)
        {
            FirearmAttackUpdate(heldFirearm, combatProfile);
        }
        else if (heldWeapon is F_Item_Weapon_Melee heldMelee)
        {
            MeleeAttackUpdate(heldMelee, combatProfile);
        }
    }

    // Melee while in friendly fire melee mode or when the target is within melee range, otherwise the firearm while it
    // has ammo, otherwise melee. Too tired to swing, it falls back to the firearm when it has ammo.
    private enumNpcWeaponChoice ChooseWeapon(F_NpcCombatProfile combatProfile)
    {
        UpdateMeleeExhaustion();
        bool hasFirearmAmmo = HasFirearmAmmo();
        F_Item_Weapon_Melee activeMeleeWeapon = ActiveMeleeWeapon;
        bool isMeleeUsable = IsWeaponUsable(activeMeleeWeapon);
        bool isMeleePreferred = isMeleeUsable && !(npcIsMeleeExhausted && hasFirearmAmmo);
        bool isFriendlyFireMeleeMode = Time.time < npcFriendlyFireMeleeUntilTime;
        if (isMeleePreferred && (isFriendlyFireMeleeMode || AttackReachDistance <= GetMeleeReadyRange(activeMeleeWeapon)))
        {
            return enumNpcWeaponChoice.Melee;
        }

        if (hasFirearmAmmo)
        {
            return enumNpcWeaponChoice.Firearm;
        }

        if (isMeleeUsable)
        {
            return enumNpcWeaponChoice.Melee;
        }

        if (combatProfile.profileDryFiresWhenEmpty && IsWeaponUsable(npcFirearmInstance))
        {
            return enumNpcWeaponChoice.Firearm;
        }

        return enumNpcWeaponChoice.None;
    }

    // Becomes exhausted when it can't afford a melee swing, recovering only once its stamina passes the swing cost plus
    // the resume margin (never exhausted with infinite stamina).
    private void UpdateMeleeExhaustion()
    {
        F_Item_Weapon_Melee activeMeleeWeapon = ActiveMeleeWeapon;
        if (activeMeleeWeapon == null)
        {
            npcIsMeleeExhausted = false;
            return;
        }

        float meleeSwingCost = activeMeleeWeapon.meleeStaminaCost;
        if (!HasStamina(meleeSwingCost))
        {
            npcIsMeleeExhausted = true;
        }
        else if (HasStamina(meleeSwingCost + F_Utility_Config_Npc.cfgNpcMeleeStaminaResumeMargin))
        {
            npcIsMeleeExhausted = false;
        }
    }

    // How close the target must be before the NPC readies its melee weapon: the weapon range multiplied, never below the minimum.
    private static float GetMeleeReadyRange(F_Item_Weapon_Melee meleeWeapon)
    {
        return Mathf.Max(meleeWeapon.meleeRange * F_Utility_Config_Npc.cfgNpcMeleeReadyRangeMultiplier, F_Utility_Config_Npc.cfgNpcMeleeReadyRangeMin);
    }

    // Whether the firearm can shoot now, is reloading, or has virtual ammo left to reload with.
    private bool HasFirearmAmmo()
    {
        return IsWeaponUsable(npcFirearmInstance) &&
               (!npcFirearmInstance.IsFireBlockedByBrokenOrEmpty() || npcFirearmInstance.IsReloading || npcFirearmInstance.CanReloadFrom(this));
    }

    private static bool IsWeaponUsable(F_Item_Weapon weapon)
    {
        return weapon != null && !weapon.IsBroken();
    }

    // Switches to the weapon (which starts its deploy delay) unless it is already held or a switch happened too recently.
    private void HoldWeapon(F_Item_Weapon weapon)
    {
        if (weapon == null || npcHeldWeapon.SelectedWeaponItem == weapon || Time.time < npcNextWeaponSwitchTime)
        {
            return;
        }

        npcHeldWeapon.SelectWeapon(weapon);
        npcNextWeaponSwitchTime = Time.time + F_Utility_Config_Npc.cfgNpcWeaponSwitchCooldown;
    }

    private void ReloadHeldFirearmIfNotFull()
    {
        if (npcHeldWeapon.SelectedWeaponItem is F_Item_Weapon_Firearm heldFirearm && heldFirearm.CanReloadFrom(this))
        {
            npcHeldWeapon.ReloadCurrentWeapon(this);
        }
    }

    // Reloads when empty (or topping up while the target is out of sight) and fires when aimed with a clear shot,
    // once locked on and no faster than its aiming skill allows.
    private void FirearmAttackUpdate(F_Item_Weapon_Firearm heldFirearm, F_NpcCombatProfile combatProfile)
    {
        if (heldFirearm.IsFireBlockedByBrokenOrEmpty() || !CanSeeAttackPoint)
        {
            ReloadHeldFirearmIfNotFull();
        }

        bool isInRange = combatProfile.profileAttacksOutOfRange || AttackDistance <= heldFirearm.weaponRange;
        bool isAimReady = Time.time >= npcAimLockedOnTime && Time.time >= npcNextFirearmShotTime;
        if (!CanShootAttackPoint || !isInRange || !isAimReady || !IsAimedAtTarget())
        {
            return;
        }

        if (npcHeldWeapon.CanUseCurrentWeapon)
        {
            npcHeldWeapon.UseCurrentWeapon();
            F_NpcAimingProfile aimingProfile = AimingProfile;
            npcNextFirearmShotTime = Time.time + heldFirearm.weaponFireRateDelay * aimingProfile.profileAimFireDelayMultiplier;
            RollShotAim(aimingProfile);
        }
        else if (combatProfile.profileDryFiresWhenEmpty && !heldFirearm.IsReloading && !heldFirearm.CanReloadFrom(this) &&
                 Time.time >= npcNextDryFireTime)
        {
            npcHeldWeapon.TryPlayDryFireSound();
            npcNextDryFireTime = Time.time + F_Utility_Config_Npc.cfgNpcDryFireInterval;
        }
    }

    // Swings when the target is within reach and aimed at; the weapon itself enforces stamina, cooldown and durability.
    private void MeleeAttackUpdate(F_Item_Weapon_Melee heldMelee, F_NpcCombatProfile combatProfile)
    {
        float meleeReach = heldMelee.meleeRange * F_Utility_Config_Npc.cfgNpcMeleeEngageRangeFraction;
        bool isInReach = combatProfile.profileAttacksOutOfRange || AttackReachDistance <= meleeReach;
        if (!CanSeeAttackPoint || !isInReach || !IsAimedAtTarget())
        {
            return;
        }

        if (npcHeldWeapon.CanUseCurrentWeapon)
        {
            npcHeldWeapon.UseCurrentWeapon();
        }
    }

    // Each time the NPC starts tracking a visible target, it has to lock on again before firing and rolls a fresh shot aim.
    private void UpdateAimLock()
    {
        bool isTracking = IsUsingWeapons && CanSeeAttackPoint;
        if (isTracking && !npcIsAimTracking)
        {
            F_NpcAimingProfile aimingProfile = AimingProfile;
            npcAimLockedOnTime = Time.time + aimingProfile.profileAimLockOnTime;
            RollShotAim(aimingProfile);
        }

        npcIsAimTracking = isTracking;
    }

    // Rolls how the next shot is aimed: a random angle away from the target within the skill's maximum error, and
    // whether to lead the target based on the skill's lead chance.
    private void RollShotAim(F_NpcAimingProfile aimingProfile)
    {
        npcAimErrorDegrees = Random.Range(-aimingProfile.profileAimErrorDegreesMax, aimingProfile.profileAimErrorDegreesMax);
        npcIsAimLeading = F_Utility_Helper_Random.RollChance(aimingProfile.profileAimLeadChance);
        RollFriendlyFireShot(aimingProfile);
    }

    // Rolls whether the NPC fires through a friendly in its line of fire, from the aiming skill's friendly fire chance.
    private void RollFriendlyFireShot(F_NpcAimingProfile aimingProfile)
    {
        npcIsFriendlyFireShotAllowed = F_Utility_Helper_Random.RollChance(aimingProfile.profileAimFriendlyFireChance);
    }

    // World angle the NPC wants to face. With a firearm it aims at where the target will be when its projectile
    // arrives (when leading this shot), offset by its aim error; otherwise straight at what it attacks. Obstacles don't move, so are never led.
    private float GetDesiredAimAngle()
    {
        Vector2 aimPoint = IsHoldingFirearm && npcIsAimLeading && !IsBreakingObstacle ? GetLeadAimPoint() : AttackPoint;
        Vector2 toAimPoint = aimPoint - npcRigidbody.position;
        float aimAngle = Mathf.Atan2(toAimPoint.y, toAimPoint.x) * Mathf.Rad2Deg;
        return IsHoldingFirearm ? aimAngle + npcAimErrorDegrees : aimAngle;
    }

    // Where the held firearm's projectile would meet the target, from the barrel's projectile speed and the target's
    // current velocity. Falls back to the target's position when it has no Rigidbody2D or can't be caught.
    private Vector2 GetLeadAimPoint()
    {
        Vector2 targetPosition = npcTarget.position;
        if (npcTargetRigidbody == null || !(npcHeldWeapon.SelectedWeaponItem is F_Item_Weapon_Firearm heldFirearm))
        {
            return targetPosition;
        }

        Transform firePosition = npcHeldWeapon.CurrentFirePosition;
        Vector2 shotOrigin = firePosition != null ? (Vector2)firePosition.position : npcRigidbody.position;
        float projectileSpeed = npcHeldWeapon.GetProjectileLaunchSpeed(heldFirearm);
        return F_Utility_Helper_Npc.TryGetInterceptPoint(shotOrigin, targetPosition, npcTargetRigidbody.linearVelocity, projectileSpeed, out Vector2 interceptPoint)
            ? interceptPoint
            : targetPosition;
    }

    // Turns towards the aim angle: at the aiming skill's speed with a firearm, otherwise at the configured melee speed.
    private void AimAtTarget()
    {
        float aimTurnSpeed = IsHoldingFirearm ? AimingProfile.profileAimTurnSpeed : F_Utility_Config_Npc.cfgNpcAimTurnSpeed;
        TurnTowardsAngle(GetDesiredAimAngle(), aimTurnSpeed);
    }

    // Keeps facing the target it is noticing, so it stays in the sight cone until the NPC engages or loses sight of it.
    private void TrackNoticedTarget()
    {
        if (npcTarget == null)
        {
            return;
        }

        Vector2 toTarget = (Vector2)npcTarget.position - npcRigidbody.position;
        TurnTowardsAngle(Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg, F_Utility_Config_Npc.cfgNpcNoticeTurnSpeed);
    }

    // Turns towards a world angle (sprite rotation offset applied) at the given degrees per second.
    private void TurnTowardsAngle(float worldAngle, float turnSpeed)
    {
        float desiredRotation = worldAngle + npcStatRotationOffset;
        npcRigidbody.rotation = Mathf.MoveTowardsAngle(npcRigidbody.rotation, desiredRotation, turnSpeed * Time.fixedDeltaTime);
    }

    private bool IsAimedAtTarget()
    {
        float facingAngle = FacingAngle;
        return Mathf.Abs(Mathf.DeltaAngle(facingAngle, GetDesiredAimAngle())) <= F_Utility_Config_Npc.cfgNpcAimToleranceDegrees;
    }

    // ---------- Obstacle breaking ----------

    // Drops the obstacle once it is gone or the NPC no longer needs to get past it: while engaged, once the target is lost
    // or can be shot directly (targets in sight come first); otherwise once the investigation ends or a target is being
    // noticed. Otherwise tracks the closest point on it and whether that point can be shot.
    private void UpdateObstacleTarget()
    {
        bool isGoalLost = npcIsEngaged
            ? npcTarget == null || npcHasLineOfFire
            : !npcIsInvestigating || npcIsNoticingTarget;
        bool isObstacleInvalid = IsBreakingObstacle && (npcObstacleTarget.IsDead || isGoalLost);
        if (isObstacleInvalid)
        {
            ClearObstacleTarget();
        }

        if (!IsBreakingObstacle)
        {
            return;
        }

        Vector2 npcPosition = npcRigidbody.position;
        npcObstaclePoint = F_Utility_Helper_Npc.GetClosestPointOnBody(npcPosition, npcObstacleTarget.transform, npcObstacleColliders);
        npcObstacleReachDistance = Vector2.Distance(npcPosition, npcObstaclePoint);
        npcHasLineOfFireToObstacle = F_Utility_Helper_Npc.GetLineOfFireStatus(this, npcPosition, npcObstaclePoint, npcObstacleTarget.transform) ==
                                     enumNpcLineOfFire.Clear;
    }

    private void ClearObstacleTarget()
    {
        npcObstacleTarget = null;
        npcObstacleColliders = null;
        npcHasLineOfFireToObstacle = false;
    }

    // Picks the obstacle standing between the NPC and its goal from the wall behaviour personality and moves to attack it.
    // Returns false when there is nothing to break or no weapon that can damage it, so the NPC carries on as normal.
    // goalTransform is ignored by the line checks (the target while fighting, null for a heard sound).
    private bool TryBreakObstacle(F_NpcCombatProfile combatProfile, Vector2 goalPosition, Transform goalTransform, enumNpcState movingState)
    {
        F_Ent obstacle = GetChosenWeaponReach() > 0f ? FindObstacleToBreak(goalPosition, goalTransform) : null;
        if (obstacle == null)
        {
            ClearObstacleTarget();
            return false;
        }

        if (obstacle != npcObstacleTarget)
        {
            npcObstacleTarget = obstacle;
            npcObstacleColliders = obstacle.GetComponentsInChildren<Collider2D>();
            UpdateObstacleTarget();
        }

        ObstacleMovement(combatProfile, movingState);
        return true;
    }

    // Break All Obstacles breaks whatever blocks its line to the goal, then whatever traps it; Break Only If Trapped
    // only breaks out when no route reaches the goal; Never Break Anything never breaks anything. Nothing is broken
    // while engaged and the target can be shot directly.
    private F_Ent FindObstacleToBreak(Vector2 goalPosition, Transform goalTransform)
    {
        if (npcIsEngaged && (npcTarget == null || npcHasLineOfFire))
        {
            return null;
        }

        Vector2 npcPosition = npcRigidbody.position;
        switch (npcPersonalityCombatWallBehaviour)
        {
            case enumNpcPersonalityCombatWallBehaviour.BreakAllObstacles:
                F_Ent obstacleInLine = FindObstacleOnLine(npcPosition, goalPosition, goalTransform);
                return obstacleInLine != null ? obstacleInLine : FindTrappingObstacle(npcPosition, goalPosition, goalTransform);
            case enumNpcPersonalityCombatWallBehaviour.BreakOnlyIfTrapped:
                return FindTrappingObstacle(npcPosition, goalPosition, goalTransform);
            default:
                return null;
        }
    }

    // The closest breakable obstacle on the line between two points, ignoring the goal transform, or null.
    private F_Ent FindObstacleOnLine(Vector2 from, Vector2 to, Transform goalTransform)
    {
        return F_Utility_Helper_Npc.TryFindBreakableObstacle(this, from, to, goalTransform, out F_Ent obstacle) ? obstacle : null;
    }

    // When no route reaches the goal, the obstacle between the end of the closest route and the goal (falling back
    // to the one between this NPC and the goal). Null when a route reaches the goal.
    private F_Ent FindTrappingObstacle(Vector2 npcPosition, Vector2 goalPosition, Transform goalTransform)
    {
        if (F_Utility_Helper_Navigation.HasCompletePath(npcPosition, goalPosition, npcAgent.areaMask, npcTrappedCheckPath, out Vector2 routeEnd))
        {
            return null;
        }

        F_Ent obstacleFromRouteEnd = FindObstacleOnLine(routeEnd, goalPosition, goalTransform);
        return obstacleFromRouteEnd != null ? obstacleFromRouteEnd : FindObstacleOnLine(npcPosition, goalPosition, goalTransform);
    }

    // The weapon used against obstacles: the combat choice once made, otherwise (such as when an investigation first
    // meets an obstacle) whichever usable weapon is currently held.
    private enumNpcWeaponChoice ObstacleWeaponChoice
    {
        get
        {
            if (npcWeaponChoice != enumNpcWeaponChoice.None || npcHeldWeapon == null)
            {
                return npcWeaponChoice;
            }

            F_Item_Weapon heldWeapon = npcHeldWeapon.SelectedWeaponItem;
            if (heldWeapon != null && heldWeapon == npcFirearmInstance)
            {
                return enumNpcWeaponChoice.Firearm;
            }

            return heldWeapon != null && heldWeapon == ActiveMeleeWeapon ? enumNpcWeaponChoice.Melee : enumNpcWeaponChoice.None;
        }
    }

    // How close the NPC must be to damage something with its obstacle weapon, or 0 when it has nothing that can.
    private float GetChosenWeaponReach()
    {
        enumNpcWeaponChoice weaponChoice = ObstacleWeaponChoice;
        if (weaponChoice == enumNpcWeaponChoice.Firearm && HasFirearmAmmo())
        {
            return npcFirearmInstance.weaponRange * F_Utility_Config_Npc.cfgNpcFiringRangeFraction;
        }

        F_Item_Weapon_Melee activeMeleeWeapon = ActiveMeleeWeapon;
        if (weaponChoice == enumNpcWeaponChoice.Melee && IsWeaponUsable(activeMeleeWeapon))
        {
            return activeMeleeWeapon.meleeRange * F_Utility_Config_Npc.cfgNpcMeleeEngageRangeFraction;
        }

        return 0f;
    }

    // Stands and attacks the obstacle once within its weapon's reach (with a clear shot for firearms), otherwise moves
    // up to just in front of it in the given moving state (sprinting only while fighting). Personalities that never
    // move only attack it from where they stand.
    private void ObstacleMovement(F_NpcCombatProfile combatProfile, enumNpcState movingState)
    {
        bool isInWeaponReach = npcObstacleReachDistance <= GetChosenWeaponReach();
        bool canAttackFromHere = isInWeaponReach && (ObstacleWeaponChoice == enumNpcWeaponChoice.Melee || npcHasLineOfFireToObstacle);
        if (canAttackFromHere || !combatProfile.profileCanMove)
        {
            HoldPosition(canAttackFromHere ? enumNpcState.Attack : enumNpcState.Hold);
            return;
        }

        Vector2 obstacleToSelf = npcRigidbody.position - npcObstaclePoint;
        Vector2 approachDirection = obstacleToSelf.sqrMagnitude > 0f ? obstacleToSelf.normalized : Vector2.zero;
        Vector2 approachPoint = npcObstaclePoint + approachDirection * F_Utility_Config_Npc.cfgNpcObstacleApproachOffset;
        npcIsSprintRequested = npcIsEngaged && characterStatStaminaCanSprint && combatProfile.profileSprintsWhenApproaching &&
                               npcObstacleReachDistance > F_Utility_Config_Npc.cfgNpcSprintMinDistance;
        MoveTo(approachPoint, movingState, false);
    }

    // ---------- Movement ----------

    // Fights the target while engaged, otherwise investigates a heard sound, otherwise follows its ward when guarding
    // one, otherwise idles and wanders.
    private void UpdateState()
    {
        if (npcIsEngaged)
        {
            CombatMovementUpdate();
        }
        else if (npcIsInvestigating)
        {
            InvestigateUpdate();
        }
        else if (npcGuardWard != null)
        {
            GuardFollowUpdate();
        }
        else
        {
            WanderUpdate(npcSpawnPosition, npcIdleWanderRange);
        }
    }

    // Heads to the heard sound, then looks around there for the investigate wait time before going back to idling. Stops
    // to watch a target it spots on the way (facing is tracked in FixedUpdate), which engages as normal once noticed.
    // Walls and obstacles in the way are broken according to its wall behaviour personality. Gives up when the sound
    // can't be reached or it has gone unheard for longer than its combat memory (hearing it again refreshes the memory).
    private void InvestigateUpdate()
    {
        if (npcIsNoticingTarget)
        {
            ClearObstacleTarget();
            HoldPosition(enumNpcState.Idle);
            npcNextRepathTime = 0f;
            return;
        }

        if (npcHasReachedInvestigatePosition)
        {
            if (Time.time - npcInvestigateArrivedTime >= F_Utility_Config_Npc.cfgNpcHearingInvestigateWaitTime)
            {
                StopInvestigating();
                StopAndWait();
            }

            return;
        }

        if (Time.time - npcInvestigateHeardTime > CombatMemoryDuration)
        {
            StopInvestigating();
            StopAndWait();
            return;
        }

        if (Time.time >= npcNextRepathTime)
        {
            npcNextRepathTime = Time.time + npcStatRepathInterval;
            npcIsSprintRequested = false;
            if (TryBreakObstacle(CombatProfile, npcInvestigatePosition, null, enumNpcState.Investigate))
            {
                return;
            }

            if (npcAgent.SetDestination(npcInvestigatePosition))
            {
                npcStatusState = enumNpcState.Investigate;
            }
            else
            {
                StopInvestigating();
                StopAndWait();
            }

            return;
        }

        if (!IsBreakingObstacle && npcStatusState == enumNpcState.Investigate && HasArrived())
        {
            npcHasReachedInvestigatePosition = true;
            npcInvestigateArrivedTime = Time.time;
            HoldPosition(enumNpcState.Idle);
        }
    }

    // Every repath interval, follows the ward once further than the follow distance. Within it, wandering guards wander
    // around the ward and the rest wait beside it.
    private void GuardFollowUpdate()
    {
        F_NpcCombatProfile combatProfile = CombatProfile;
        if (Time.time >= npcNextRepathTime)
        {
            npcNextRepathTime = Time.time + npcStatRepathInterval;
            if (TryCatchUpToWard(combatProfile.profileGuardFollowDistance))
            {
                return;
            }

            if (!combatProfile.profileGuardWandersNearWard)
            {
                HoldPosition(enumNpcState.Idle);
                return;
            }

            // Caught up: stop following and pause before the first wander
            if (npcStatusState == enumNpcState.Follow)
            {
                StopAndWait();
            }
        }

        if (combatProfile.profileGuardWandersNearWard && npcStatusState != enumNpcState.Follow)
        {
            float guardWanderRange = combatProfile.profileGuardFollowDistance * F_Utility_Config_Npc.cfgNpcGuardWanderRangeFraction;
            WanderUpdate(npcGuardWard.transform.position, guardWanderRange);
        }
    }

    // Heads back towards the ward when further than the given distance, stopping at a fraction of the follow distance so
    // it doesn't stop and start at the edge, and sprinting while far behind. Returns false when already close enough.
    private bool TryCatchUpToWard(float catchUpDistance)
    {
        Vector2 wardPosition = npcGuardWard.transform.position;
        Vector2 wardToSelf = npcRigidbody.position - wardPosition;
        float wardDistance = wardToSelf.magnitude;
        if (wardDistance <= catchUpDistance)
        {
            return false;
        }

        float arriveDistance = CombatProfile.profileGuardFollowDistance * F_Utility_Config_Npc.cfgNpcGuardFollowArriveFraction;
        Vector2 arrivePoint = wardPosition + wardToSelf / wardDistance * arriveDistance;
        Vector2 destination = F_Utility_Helper_Navigation.TrySamplePosition(arrivePoint, out Vector2 navArrivePoint) ? navArrivePoint : wardPosition;
        npcIsSprintRequested = characterStatStaminaCanSprint && wardDistance > F_Utility_Config_Npc.cfgNpcGuardSprintDistance;
        MoveTo(destination, enumNpcState.Follow, false);
        return true;
    }

    // While engaged, keeps a body guard's destination within its leash of the ward, so it never strays to fight.
    private Vector2 ClampToGuardLeash(Vector2 destination)
    {
        if (!npcIsEngaged || npcGuardWard == null)
        {
            return destination;
        }

        float leashDistance = CombatProfile.profileGuardLeashDistance;
        Vector2 wardPosition = npcGuardWard.transform.position;
        Vector2 wardToDestination = destination - wardPosition;
        if (wardToDestination.sqrMagnitude <= leashDistance * leashDistance)
        {
            return destination;
        }

        Vector2 leashPoint = wardPosition + wardToDestination.normalized * leashDistance;
        return F_Utility_Helper_Navigation.TrySamplePosition(leashPoint, out Vector2 navLeashPoint) ? navLeashPoint : wardPosition;
    }

    // Decides where to move every repath interval, from the personality profile and the weapon in use.
    private void CombatMovementUpdate()
    {
        bool npcPathInvalid = !npcAgent.pathPending && npcAgent.hasPath && npcAgent.pathStatus == NavMeshPathStatus.PathInvalid;
        if (npcPathInvalid)
        {
            HoldPosition(enumNpcState.Hold);
        }

        if (Time.time < npcNextRepathTime)
        {
            return;
        }

        npcNextRepathTime = Time.time + npcStatRepathInterval;
        F_NpcCombatProfile combatProfile = CombatProfile;
        if (npcTarget != null && TryBreakObstacle(combatProfile, npcTarget.position, npcTarget, enumNpcState.Chase))
        {
            return;
        }

        if (!combatProfile.profileCanMove)
        {
            HoldPosition(npcCanSeeTarget ? enumNpcState.Attack : enumNpcState.Hold);
            return;
        }

        // Body guards that have strayed past their leash catch up before fighting or defending
        if (npcGuardWard != null && TryCatchUpToWard(combatProfile.profileGuardLeashDistance))
        {
            return;
        }

        bool isFirearmReloading = npcFirearmInstance != null && npcFirearmInstance.IsReloading &&
                                  npcHeldWeapon != null && npcHeldWeapon.SelectedWeaponItem == npcFirearmInstance;
        bool wantsCover = (combatProfile.profileCoverWhenReloading && isFirearmReloading) || Time.time < npcCoverUntilTime;
        if (wantsCover && TryMoveToCover())
        {
            return;
        }

        if (!npcCanSeeTarget)
        {
            SearchMovement(combatProfile);
            return;
        }

        if (npcWeaponChoice == enumNpcWeaponChoice.Firearm && HasFirearmAmmo())
        {
            FirearmMovement(combatProfile);
        }
        else
        {
            MeleeMovement(combatProfile);
        }
    }

    // Holds or keeps moving once it has a clear shot in range; otherwise closes in, or peeks out from nearby if it never closes in.
    // A friendly in the way makes it reposition or join the fight in melee first.
    private void FirearmMovement(F_NpcCombatProfile combatProfile)
    {
        float firingRange = npcFirearmInstance.weaponRange * F_Utility_Config_Npc.cfgNpcFiringRangeFraction;
        if (IsAvoidingFriendlyFire && TryAvoidFriendlyFire(combatProfile, firingRange))
        {
            return;
        }

        bool hasShotInRange = CanShootTarget && npcTargetDistance <= firingRange;
        if (hasShotInRange && combatProfile.profileHoldsAtFiringRange)
        {
            HoldPosition(enumNpcState.Attack);
            return;
        }

        if (combatProfile.profileApproachFirearm != enumNpcApproachMode.None)
        {
            Approach(combatProfile.profileApproachFirearm, combatProfile);
            return;
        }

        if (!hasShotInRange && F_Utility_Helper_Npc.TryFindPeekPoint(npcRigidbody.position, this, npcTarget, firingRange, out Vector2 peekPoint))
        {
            MoveTo(peekPoint, enumNpcState.Chase);
            return;
        }

        HoldPosition(hasShotInRange ? enumNpcState.Attack : enumNpcState.Hold);
    }

    // A friendly blocks the shot: moves to the closest spot with a clear shot, otherwise switches to melee to help up
    // close when its personality closes in with melee. Returns false when it can do neither.
    private bool TryAvoidFriendlyFire(F_NpcCombatProfile combatProfile, float firingRange)
    {
        if (F_Utility_Helper_Npc.TryFindPeekPoint(npcRigidbody.position, this, npcTarget, firingRange, out Vector2 clearShotPoint))
        {
            MoveTo(clearShotPoint, enumNpcState.Reposition);
            return true;
        }

        if (combatProfile.profileApproachMelee == enumNpcApproachMode.None || !IsWeaponUsable(ActiveMeleeWeapon))
        {
            return false;
        }

        npcFriendlyFireMeleeUntilTime = Time.time + F_Utility_Config_Npc.cfgNpcFriendlyFireMeleeModeDuration;
        MeleeMovement(combatProfile);
        return true;
    }

    // Melee only, out of ammo or unarmed: stops to swing when in reach, otherwise closes in, or hides if it never closes in.
    private void MeleeMovement(F_NpcCombatProfile combatProfile)
    {
        F_Item_Weapon_Melee activeMeleeWeapon = ActiveMeleeWeapon;
        bool isMeleeHeld = npcWeaponChoice == enumNpcWeaponChoice.Melee && activeMeleeWeapon != null;
        if (isMeleeHeld && npcTargetReachDistance <= activeMeleeWeapon.meleeRange * F_Utility_Config_Npc.cfgNpcMeleeEngageRangeFraction)
        {
            HoldPosition(enumNpcState.Attack);
            return;
        }

        if (combatProfile.profileApproachMelee != enumNpcApproachMode.None)
        {
            Approach(combatProfile.profileApproachMelee, combatProfile);
            return;
        }

        if (!TryMoveToCover())
        {
            HoldPosition(enumNpcState.Hold);
        }
    }

    // The target is out of sight: heads to where it was last seen to try and find it, then waits there until it is seen
    // again or forgotten.
    private void SearchMovement(F_NpcCombatProfile combatProfile)
    {
        Vector2 toLastSeenPosition = npcTargetLastSeenPosition - npcRigidbody.position;
        if (toLastSeenPosition.sqrMagnitude <= npcStatStoppingDistance * npcStatStoppingDistance)
        {
            HoldPosition(enumNpcState.Hold);
            return;
        }

        npcIsSprintRequested = characterStatStaminaCanSprint && combatProfile.profileSprintsWhenApproaching && toLastSeenPosition.magnitude > F_Utility_Config_Npc.cfgNpcSprintMinDistance;
        MoveTo(npcTargetLastSeenPosition, enumNpcState.Search, false);
    }

    // Closes in on the target: flanking and cover hopping give way to a straight charge once close enough.
    private void Approach(enumNpcApproachMode approachMode, F_NpcCombatProfile combatProfile)
    {
        npcIsSprintRequested = characterStatStaminaCanSprint && combatProfile.profileSprintsWhenApproaching && npcTargetDistance > F_Utility_Config_Npc.cfgNpcSprintMinDistance;
        bool isCloseEnoughToCharge = npcTargetDistance <= F_Utility_Config_Npc.cfgNpcChargeDistance;

        if (!isCloseEnoughToCharge && approachMode == enumNpcApproachMode.Flank &&
            F_Utility_Helper_Npc.TryFindFlankPoint(transform, npcTarget, ref npcFlankSide, out Vector2 flankPoint))
        {
            MoveTo(flankPoint, enumNpcState.Flank, false);
            return;
        }

        float coverHopMaxTargetDistance = npcTargetDistance - F_Utility_Config_Npc.cfgNpcCoverHopMinAdvance;
        if (!isCloseEnoughToCharge && approachMode == enumNpcApproachMode.CoverHop &&
            F_Utility_Helper_Npc.TryFindCoverPoint(npcRigidbody.position, F_Utility_Config_Npc.cfgNpcCoverSearchRadius, transform, npcTarget,
                coverHopMaxTargetDistance, out Vector2 coverHopPoint))
        {
            MoveTo(coverHopPoint, enumNpcState.Cover, false);
            return;
        }

        MoveTo(npcTarget.position, enumNpcState.Chase, false);
    }

    // Heads for the closest spot the target can't shoot, within firing range when holding a firearm.
    private bool TryMoveToCover()
    {
        float maxTargetDistance = npcFirearmInstance != null
            ? npcFirearmInstance.weaponRange
            : npcTargetDistance + F_Utility_Config_Npc.cfgNpcCoverSearchRadius;
        if (!F_Utility_Helper_Npc.TryFindCoverPoint(npcRigidbody.position, F_Utility_Config_Npc.cfgNpcCoverSearchRadius, transform, npcTarget,
                maxTargetDistance, out Vector2 coverPoint))
        {
            return false;
        }

        MoveTo(coverPoint, enumNpcState.Cover);
        return true;
    }

    // Sets a destination (kept within a body guard's leash) and state; falls back to holding when the destination can't be reached.
    private void MoveTo(Vector2 destination, enumNpcState movingState, bool stopSprinting = true)
    {
        if (stopSprinting)
        {
            npcIsSprintRequested = false;
        }

        if (npcAgent.SetDestination(ClampToGuardLeash(destination)))
        {
            npcStatusState = movingState;
        }
        else
        {
            HoldPosition(enumNpcState.Hold);
        }
    }

    private void HoldPosition(enumNpcState holdingState)
    {
        npcIsSprintRequested = false;
        if (npcAgent.hasPath)
        {
            npcAgent.ResetPath();
        }

        npcStatusState = holdingState;
    }

    // Sprints while allowed and requested, stopping before stamina drops below what a melee swing needs and only resuming
    // once it has recovered past the resume margin (no reserve with infinite stamina). Runs every frame so sprint drain
    // can never overshoot the reserve between decisions.
    private void UpdateSprint()
    {
        bool isSprintAllowed = characterStatStaminaCanSprint && npcIsSprintRequested && CanSprint;
        F_Item_Weapon_Melee activeMeleeWeapon = ActiveMeleeWeapon;
        float staminaReserve = activeMeleeWeapon != null
            ? activeMeleeWeapon.meleeStaminaCost * F_Utility_Config_Npc.cfgNpcSprintStaminaReserveMultiplier
            : 0f;

        if (IsSprinting)
        {
            if (!isSprintAllowed || (!characterStatStaminaIsInfinite && characterStatStamina <= staminaReserve))
            {
                SprintEnd();
            }
        }
        else if (isSprintAllowed &&
                 HasStamina(staminaReserve + F_Utility_Config_Npc.cfgNpcSprintStaminaResumeMargin))
        {
            SprintStart();
        }
    }

    // Waits at the current spot, then walks to a random NavMesh point within the wander range of the wander centre
    // (the spawn point, or a body guard's ward). While noticing a target it stops to watch it (facing is tracked in
    // FixedUpdate), then pauses before wandering again once it engages or loses sight.
    private void WanderUpdate(Vector2 wanderCentre, float wanderRange)
    {
        if (npcIsNoticingTarget)
        {
            HoldPosition(enumNpcState.Idle);
            ScheduleNextWander();
            return;
        }

        if (IsCombatState(npcStatusState))
        {
            StopAndWait();
            return;
        }

        if (npcStatusState == enumNpcState.Wander && HasArrived())
        {
            StopAndWait();
            return;
        }

        if (npcStatusState != enumNpcState.Idle || Time.time < npcNextWanderTime)
        {
            return;
        }

        // A range of 0 stands still, only walking back to the wander centre after being drawn away from it
        bool isAtWanderCentre = (npcRigidbody.position - wanderCentre).sqrMagnitude <= npcStatStoppingDistance * npcStatStoppingDistance;
        if (wanderRange <= npcIdleWanderRangeStandStill && isAtWanderCentre)
        {
            ScheduleNextWander();
            return;
        }

        if (F_Utility_Helper_Navigation.TryGetRandomPointOnNavMesh(wanderCentre, wanderRange, out Vector2 wanderPoint)
            && npcAgent.SetDestination(wanderPoint))
        {
            npcStatusState = enumNpcState.Wander;
        }
        else
        {
            ScheduleNextWander();
        }
    }

    // States that aren't wandering, so wandering clears them before picking its next destination.
    private static bool IsCombatState(enumNpcState state)
    {
        return state == enumNpcState.Chase || state == enumNpcState.Attack || state == enumNpcState.Cover ||
               state == enumNpcState.Flank || state == enumNpcState.Hold || state == enumNpcState.Reposition ||
               state == enumNpcState.Follow || state == enumNpcState.Search || state == enumNpcState.Investigate;
    }

    // Clears the current path, goes idle and schedules the next wander.
    private void StopAndWait()
    {
        npcAgent.ResetPath();
        npcStatusState = enumNpcState.Idle;
        ScheduleNextWander();
    }

    // Picks a random pause before the next wander destination.
    private void ScheduleNextWander()
    {
        npcNextWanderTime = Time.time + Random.Range(npcStatWanderWaitTimeMin, npcStatWanderWaitTimeMax);
    }

    // Whether the agent has reached its destination or has nowhere to go.
    private bool HasArrived()
    {
        return !npcAgent.pathPending && (!npcAgent.hasPath || npcAgent.remainingDistance <= npcAgent.stoppingDistance);
    }

    // Full (or sprint) move speed while moving in combat, reduced while wandering or investigating, none while standing.
    private float GetStateSpeed()
    {
        switch (npcStatusState)
        {
            case enumNpcState.Chase:
            case enumNpcState.Cover:
            case enumNpcState.Flank:
            case enumNpcState.Reposition:
            case enumNpcState.Follow:
            case enumNpcState.Search:
                return GetCurrentMoveSpeed();
            case enumNpcState.Investigate:
                return GetCurrentMoveSpeed() * F_Utility_Config_Npc.cfgNpcHearingInvestigateSpeedMultiplier;
            case enumNpcState.Wander:
                return GetCurrentMoveSpeed() * npcStatWanderSpeedMultiplier;
            default:
                return 0f;
        }
    }
}
