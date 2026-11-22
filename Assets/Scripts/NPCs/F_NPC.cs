using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Generic base for all NPCs. Idles, wanders around its spawn point and chases its target when it comes in range,
/// using the NavMesh to plan paths while its Rigidbody2D stays in charge of movement and collisions.
/// New NPC types are prefabs with tuned values; subclass only for genuinely new behaviour.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(NavMeshAgent), typeof(CircleCollider2D))]
public class F_NPC : F_Character
{
    [Header("Constants Private")]
    private const float npcDefaultDetectionRange = 6f;
    private const float npcDefaultLoseTargetRange = 9f;
    private const float npcDefaultWanderRadius = 4f;
    private const float npcDefaultWanderWaitTimeMin = 1f;
    private const float npcDefaultWanderWaitTimeMax = 3f;
    private const float npcDefaultWanderSpeedMultiplier = 0.5f;
    private const float npcDefaultRepathInterval = 0.25f;
    private const float npcDefaultStoppingDistance = 0.4f;
    private const float npcDefaultRotationOffset = 0f;
    // Below this speed the NPC counts as standing still and keeps its current facing
    private const float npcVelocityStopThreshold = 0.05f;

    [Header("NPC Detection Stats")]
    // Starts chasing its target within this distance
    public float npcStatDetectionRange = npcDefaultDetectionRange;
    // Stops chasing its target beyond this distance; should be larger than the detection range
    public float npcStatLoseTargetRange = npcDefaultLoseTargetRange;

    [Header("NPC Wander Stats")]
    // How far from its spawn position this NPC wanders
    public float npcStatWanderRadius = npcDefaultWanderRadius;
    // Shortest pause in seconds between wander destinations
    public float npcStatWanderWaitTimeMin = npcDefaultWanderWaitTimeMin;
    // Longest pause in seconds between wander destinations
    public float npcStatWanderWaitTimeMax = npcDefaultWanderWaitTimeMax;
    // Fraction of the current move speed used while wandering
    public float npcStatWanderSpeedMultiplier = npcDefaultWanderSpeedMultiplier;

    [Header("NPC Movement Stats")]
    // Seconds between chase destination updates
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
    private float npcNextWanderTime;
    private float npcNextRepathTime;

    /// <summary>What this NPC is currently doing.</summary>
    public enumNpcState NpcState => npcStatusState;

    // Whether the agent can be queried and given destinations.
    private bool IsAgentUsable => npcAgent != null && npcAgent.enabled && npcAgent.isOnNavMesh;

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
    }

    protected virtual void OnDisable()
    {
        F_Logic_Navigation.OnNavMeshReady -= HandleNavMeshReady;
    }

    protected override void Start()
    {
        base.Start();
        if (npcTarget == null)
        {
            F_PlayerStats player = FindFirstObjectByType<F_PlayerStats>();
            npcTarget = player != null ? player.transform : null;
        }

        if (F_Logic_Navigation.IsNavMeshReady)
        {
            TryEnableAgent();
        }
    }

    /// <summary>Sets what this NPC chases when it comes within detection range. Null makes it only wander.</summary>
    /// <param name="target">The transform to chase, or null.</param>
    public void SetTarget(Transform target)
    {
        npcTarget = target;
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead || !IsAgentUsable)
        {
            return;
        }

        UpdateState();
    }

    private void FixedUpdate()
    {
        if (IsDead || !IsAgentUsable)
        {
            npcRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        float npcCurrentSpeed = GetStateSpeed();
        npcAgent.speed = npcCurrentSpeed;
        Vector2 npcVelocity = npcStatusState == enumNpcState.Idle
            ? Vector2.zero
            : Vector2.ClampMagnitude(npcAgent.desiredVelocity, npcCurrentSpeed);

        npcRigidbody.linearVelocity = npcVelocity;
        npcAgent.nextPosition = npcRigidbody.position;

        if (npcVelocity.sqrMagnitude > npcVelocityStopThreshold * npcVelocityStopThreshold)
        {
            npcRigidbody.rotation = Mathf.Atan2(npcVelocity.y, npcVelocity.x) * Mathf.Rad2Deg + npcStatRotationOffset;
        }
    }

    // Stops moving and releases the agent; F_Ent then shatters and destroys the NPC as normal.
    protected override void OnDeath()
    {
        base.OnDeath();
        npcRigidbody.linearVelocity = Vector2.zero;
        npcAgent.enabled = false;
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

    // Chases the target while it is in range, otherwise idles and wanders.
    private void UpdateState()
    {
        if (IsTargetInRange())
        {
            ChaseUpdate();
        }
        else
        {
            WanderUpdate();
        }
    }

    // Uses the detection range to start a chase and the larger lose range to keep it going.
    private bool IsTargetInRange()
    {
        if (npcTarget == null)
        {
            return false;
        }

        float range = npcStatusState == enumNpcState.Chase ? npcStatLoseTargetRange : npcStatDetectionRange;
        return ((Vector2)npcTarget.position - npcRigidbody.position).sqrMagnitude <= range * range;
    }

    // Updates the chase destination every repath interval; falls back to wandering when the target cannot be reached.
    private void ChaseUpdate()
    {
        bool npcPathInvalid = !npcAgent.pathPending && npcAgent.hasPath && npcAgent.pathStatus == NavMeshPathStatus.PathInvalid;
        if (npcPathInvalid)
        {
            StopAndWait();
            return;
        }

        if (Time.time < npcNextRepathTime)
        {
            return;
        }

        npcNextRepathTime = Time.time + npcStatRepathInterval;
        if (npcAgent.SetDestination(npcTarget.position))
        {
            npcStatusState = enumNpcState.Chase;
        }
        else
        {
            StopAndWait();
        }
    }

    // Waits at the current spot, then walks to a random NavMesh point around the spawn position.
    private void WanderUpdate()
    {
        if (npcStatusState == enumNpcState.Chase)
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

        if (F_Utility_Helper_Navigation.TryGetRandomPointOnNavMesh(npcSpawnPosition, npcStatWanderRadius, out Vector2 wanderPoint)
            && npcAgent.SetDestination(wanderPoint))
        {
            npcStatusState = enumNpcState.Wander;
        }
        else
        {
            ScheduleNextWander();
        }
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

    // Full move speed while chasing, reduced while wandering, none while idle.
    private float GetStateSpeed()
    {
        switch (npcStatusState)
        {
            case enumNpcState.Chase:
                return GetCurrentMoveSpeed();
            case enumNpcState.Wander:
                return GetCurrentMoveSpeed() * npcStatWanderSpeedMultiplier;
            default:
                return 0f;
        }
    }
}
