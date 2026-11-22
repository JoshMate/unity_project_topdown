using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Generic base for all NPCs. Wanders around its spawn point, targets the closest character its faction is hostile to
/// when it can see it (see F_Utility_Config_Factions), then fights using
/// its equipped weapons and combat personality. Weapons work exactly like the player's (deploy, fire rate, reloads,
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

    [Header("NPC Equipped Weapons")]
    // Melee weapon prefab this NPC spawns holding; leave empty for none
    public F_Item_Weapon_Melee npcEquippedWeaponMelee;
    // Firearm prefab this NPC spawns holding; leave empty for none
    public F_Item_Weapon_Firearm npcEquippedWeaponFirearm;
    // Virtual reserve rounds the firearm reloads from, instead of inventory ammo items; counts down as it reloads
    [Min(0)]
    public int npcEquippedWeaponAmmoCount = npcDefaultEquippedWeaponAmmoCount;

    [Header("NPC Personality")]
    // How this NPC fights once it notices a target (see enumNpcPersonalityCombatApproach and F_Utility_Config_Npc)
    [FormerlySerializedAs("npcPersonalityCombat")]
    public enumNpcPersonalityCombatApproach npcPersonalityCombatApproach = enumNpcPersonalityCombatApproach.Balanced;
    // How well this NPC aims and fires firearms (see enumNpcPersonalityCombatSkillAiming and F_Utility_Config_Npc)
    public enumNpcPersonalityCombatSkillAiming npcPersonalityCombatSkillAiming = enumNpcPersonalityCombatSkillAiming.Medium;
    // How far this NPC can see in line of sight; it notices targets within this range and loses sight of them beyond it multiplied by cfgNpcSightLoseTargetRangeMultiplier
    public enumNpcPersonalityCombatSightRange npcPersonalityCombatSightRange = enumNpcPersonalityCombatSightRange.VeryClose;
    // How long a target must stay in this NPC's sight before it is noticed; breaking sight starts the wait again (see F_Utility_Config_Npc)
    [FormerlySerializedAs("npcPersonalityCombatAwarenessLevel")]
    public enumNpcPersonalityCombatAwareness npcPersonalityCombatAwareness = enumNpcPersonalityCombatAwareness.Instant;
    // How long this NPC remembers its target after losing sight of it, searching where it was last seen before forgetting it (see F_Utility_Config_Npc)
    public enumNpcPersonalityCombatMemory npcPersonalityCombatMemory = enumNpcPersonalityCombatMemory.Medium;

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
    // Whether this NPC may sprint at all; when false its personality's sprinting is ignored
    public bool npcCanSprint = false;
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

    // Seconds a target must stay in sight before this NPC notices it; read live so it can be changed at runtime.
    private float AwarenessDelay => F_Utility_Config_Npc.GetAwarenessDelay(npcPersonalityCombatAwareness);

    // Seconds this NPC remembers its target after losing sight of it; read live so it can be changed at runtime.
    private float CombatMemoryDuration => F_Utility_Config_Npc.GetCombatMemoryDuration(npcPersonalityCombatMemory);

    // Whether the NPC is currently holding a firearm, so its aiming skill applies.
    private bool IsHoldingFirearm => npcHeldWeapon != null && npcHeldWeapon.SelectedWeaponItem is F_Item_Weapon_Firearm;

    // Whether the NPC may fire at its target now: a clear line, or only friendlies in the way and its skill roll lets it shoot anyway.
    private bool CanShootTarget => npcHasLineOfFire || (npcIsFriendlyInLineOfFire && npcIsFriendlyFireShotAllowed);

    // Whether a friendly is in the line of fire and the NPC is holding its fire because of it.
    private bool IsAvoidingFriendlyFire => npcIsFriendlyInLineOfFire && !npcIsFriendlyFireShotAllowed;

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
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        F_Logic_Navigation.OnNavMeshReady += HandleNavMeshReady;
        OnAnyEntDamaged += HandleAnyEntDamaged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        F_Logic_Navigation.OnNavMeshReady -= HandleNavMeshReady;
        OnAnyEntDamaged -= HandleAnyEntDamaged;
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
        if (target == npcTarget)
        {
            return;
        }

        npcTarget = target;
        // A new target has to be locked onto again
        npcIsAimTracking = false;
        npcIsFriendlyInLineOfFire = false;
        npcFriendlyFireMeleeUntilTime = 0f;
        // A new target has to be noticed from scratch
        npcIsNoticingTarget = false;
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

        bool isAimingAtTarget = npcIsEngaged && npcCanSeeTarget;
        if (isAimingAtTarget)
        {
            AimAtTarget();
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

        if (!isAimingAtTarget && npcVelocity.sqrMagnitude > npcVelocityStopThreshold * npcVelocityStopThreshold)
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
        if (npcEquippedWeaponMelee == null && npcEquippedWeaponFirearm == null)
        {
            return;
        }

        GameObject heldWeaponRigPrefab = Resources.Load<GameObject>(F_Utility_Config_Npc.cfgNpcHeldWeaponRigResourcePath);
        if (heldWeaponRigPrefab == null)
        {
            Debug.LogWarning($"F_NPC: no held weapon rig found at Resources/{F_Utility_Config_Npc.cfgNpcHeldWeaponRigResourcePath}, so {name} can't use its weapons.");
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
        npcFirearmInstance = SpawnEquippedWeapon(npcEquippedWeaponFirearm);
        npcHeldWeapon.SelectWeapon(npcFirearmInstance != null ? (F_Item_Weapon)npcFirearmInstance : npcMeleeInstance);
    }

    // Weapon instances stay out of the world like inventory items, so they keep their own state (ammo, durability, delays).
    // They are moved into this NPC's scene, as the active scene can be a loading scene that unloads and destroys them.
    private T SpawnEquippedWeapon<T>(T weaponPrefab) where T : F_Item_Weapon
    {
        if (weaponPrefab == null)
        {
            return null;
        }

        T weaponInstance = Instantiate(weaponPrefab);
        SceneManager.MoveGameObjectToScene(weaponInstance.gameObject, gameObject.scene);
        weaponInstance.SetInventoryStoredState(true);
        return weaponInstance;
    }

    private void DestroyEquippedWeapons()
    {
        if (npcMeleeInstance != null)
        {
            Destroy(npcMeleeInstance.gameObject);
            npcMeleeInstance = null;
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
    // forgets it once out of sight for longer than its combat memory or when it dies.
    private void UpdateTargetAwareness()
    {
        bool isTargetInvalid = npcTarget != null && (npcTargetEnt == null || npcTargetEnt.IsDead || !F_Utility_Helper_Faction.IsHostile(this, npcTargetEnt));
        if (isTargetInvalid)
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
        else if (!npcCanSeeTarget && Time.time - npcTargetLastSeenTime > CombatMemoryDuration)
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
    }

    // Engages once the target has stayed in sight within sight range for the whole awareness delay; losing sight of it
    // (or it leaving sight range) starts the wait again.
    private void UpdateNoticeTarget()
    {
        bool isTargetInSight = npcCanSeeTarget && npcTargetDistance <= SightRange;
        if (!isTargetInSight)
        {
            npcIsNoticingTarget = false;
            return;
        }

        if (!npcIsNoticingTarget)
        {
            npcIsNoticingTarget = true;
            npcNoticeStartTime = Time.time;
        }

        if (Time.time - npcNoticeStartTime >= AwarenessDelay)
        {
            Engage();
        }
    }

    // Every scan interval, while idle or while the current target is out of sight, switches to the closest visible hostile character.
    private void UpdateTargetScan()
    {
        if (Time.time < npcNextTargetScanTime || (npcIsEngaged && npcCanSeeTarget))
        {
            return;
        }

        npcNextTargetScanTime = Time.time + F_Utility_Config_Npc.cfgNpcTargetScanInterval;
        F_Character closestHostile = F_Utility_Helper_Npc.FindClosestVisibleHostile(this, npcRigidbody.position, SightRange);
        if (closestHostile != null)
        {
            SetTarget(closestHostile.transform);
        }
        else if (!npcIsEngaged)
        {
            SetTarget(null);
        }
    }

    // Being hit by a hostile character (or a body guard's ward being hit) makes the NPC notice it (switching to it when
    // idle or unable to see its target), and sends cover-seeking personalities to cover when it was the one hit.
    // Hits from allies and neutrals are ignored.
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
            SetTarget(attackerCharacter.transform);
        }

        if (attackerCharacter.transform != npcTarget)
        {
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

    private void Engage()
    {
        npcIsEngaged = true;
        npcIsNoticingTarget = false;
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
        npcIsNoticingTarget = false;
        if (!npcIsEngaged)
        {
            return;
        }

        npcIsEngaged = false;
        npcCoverUntilTime = 0f;
        npcIsSprintRequested = false;
        if (IsAgentUsable)
        {
            StopAndWait();
        }
    }

    // ---------- Weapon use ----------

    // Picks, readies and uses the right weapon while engaged; while idle, holds the firearm and tops it up.
    private void UpdateCombatWeapon()
    {
        if (npcHeldWeapon == null)
        {
            npcWeaponChoice = enumNpcWeaponChoice.None;
            return;
        }

        if (!npcIsEngaged)
        {
            npcWeaponChoice = enumNpcWeaponChoice.None;
            HoldWeapon(IsWeaponUsable(npcFirearmInstance) ? (F_Item_Weapon)npcFirearmInstance : npcMeleeInstance);
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
            HoldWeapon(npcMeleeInstance);
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
        bool isMeleeUsable = IsWeaponUsable(npcMeleeInstance);
        bool isMeleePreferred = isMeleeUsable && !(npcIsMeleeExhausted && hasFirearmAmmo);
        bool isFriendlyFireMeleeMode = Time.time < npcFriendlyFireMeleeUntilTime;
        if (isMeleePreferred && (isFriendlyFireMeleeMode || npcTargetReachDistance <= GetMeleeReadyRange(npcMeleeInstance)))
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
        if (npcMeleeInstance == null)
        {
            npcIsMeleeExhausted = false;
            return;
        }

        float meleeSwingCost = npcMeleeInstance.meleeStaminaCost;
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
        if (heldFirearm.IsFireBlockedByBrokenOrEmpty() || !npcCanSeeTarget)
        {
            ReloadHeldFirearmIfNotFull();
        }

        bool isInRange = combatProfile.profileAttacksOutOfRange || npcTargetDistance <= heldFirearm.weaponRange;
        bool isAimReady = Time.time >= npcAimLockedOnTime && Time.time >= npcNextFirearmShotTime;
        if (!CanShootTarget || !isInRange || !isAimReady || !IsAimedAtTarget())
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
        bool isInReach = combatProfile.profileAttacksOutOfRange || npcTargetReachDistance <= meleeReach;
        if (!npcCanSeeTarget || !isInReach || !IsAimedAtTarget())
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
        bool isTracking = npcIsEngaged && npcCanSeeTarget;
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
    // arrives (when leading this shot), offset by its aim error; otherwise straight at the target.
    private float GetDesiredAimAngle()
    {
        Vector2 aimPoint = IsHoldingFirearm && npcIsAimLeading ? GetLeadAimPoint() : (Vector2)npcTarget.position;
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
        float desiredAngle = GetDesiredAimAngle() + npcStatRotationOffset;
        float aimTurnSpeed = IsHoldingFirearm ? AimingProfile.profileAimTurnSpeed : F_Utility_Config_Npc.cfgNpcAimTurnSpeed;
        npcRigidbody.rotation = Mathf.MoveTowardsAngle(npcRigidbody.rotation, desiredAngle, aimTurnSpeed * Time.fixedDeltaTime);
    }

    private bool IsAimedAtTarget()
    {
        float facingAngle = npcRigidbody.rotation - npcStatRotationOffset;
        return Mathf.Abs(Mathf.DeltaAngle(facingAngle, GetDesiredAimAngle())) <= F_Utility_Config_Npc.cfgNpcAimToleranceDegrees;
    }

    // ---------- Movement ----------

    // Fights the target while engaged, otherwise follows its ward when guarding one, otherwise idles and wanders.
    private void UpdateState()
    {
        if (npcIsEngaged)
        {
            CombatMovementUpdate();
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
        npcIsSprintRequested = npcCanSprint && wardDistance > F_Utility_Config_Npc.cfgNpcGuardSprintDistance;
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

        if (combatProfile.profileApproachMelee == enumNpcApproachMode.None || !IsWeaponUsable(npcMeleeInstance))
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
        bool isMeleeHeld = npcWeaponChoice == enumNpcWeaponChoice.Melee && npcMeleeInstance != null;
        if (isMeleeHeld && npcTargetReachDistance <= npcMeleeInstance.meleeRange * F_Utility_Config_Npc.cfgNpcMeleeEngageRangeFraction)
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

        npcIsSprintRequested = npcCanSprint && combatProfile.profileSprintsWhenApproaching && toLastSeenPosition.magnitude > F_Utility_Config_Npc.cfgNpcSprintMinDistance;
        MoveTo(npcTargetLastSeenPosition, enumNpcState.Search, false);
    }

    // Closes in on the target: flanking and cover hopping give way to a straight charge once close enough.
    private void Approach(enumNpcApproachMode approachMode, F_NpcCombatProfile combatProfile)
    {
        npcIsSprintRequested = npcCanSprint && combatProfile.profileSprintsWhenApproaching && npcTargetDistance > F_Utility_Config_Npc.cfgNpcSprintMinDistance;
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
        bool isSprintAllowed = npcCanSprint && npcIsSprintRequested && CanSprint;
        float staminaReserve = npcMeleeInstance != null
            ? npcMeleeInstance.meleeStaminaCost * F_Utility_Config_Npc.cfgNpcSprintStaminaReserveMultiplier
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
    // (the spawn point, or a body guard's ward).
    private void WanderUpdate(Vector2 wanderCentre, float wanderRange)
    {
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
               state == enumNpcState.Follow || state == enumNpcState.Search;
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

    // Full (or sprint) move speed while moving in combat, reduced while wandering, none while standing.
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
            case enumNpcState.Wander:
                return GetCurrentMoveSpeed() * npcStatWanderSpeedMultiplier;
            default:
                return 0f;
        }
    }
}
