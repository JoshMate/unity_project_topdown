using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared NPC sight, line of fire and tactical position searches (cover, peek and flank points), so every NPC
/// personality reads the world the same way. Ents block sight unless entFeatureCanSeeThrough and block shots unless
/// entFeatureCanShootOver, so other NPCs count as cover too.
/// </summary>
public static class F_Utility_Helper_Npc
{
    [Header("Constants Private")]
    private const float npcFullCircleDegrees = 360f;
    // Half of a sight cone lies either side of the facing direction
    private const float npcSightConeHalfFraction = 0.5f;
    // Offsets alternate rings so their points don't line up
    private const float npcRingAngleOffsetFraction = 0.5f;
    // Below this the intercept equation is treated as linear (target speed matches projectile speed)
    private const float npcInterceptEpsilon = 0.0001f;

    [Header("Privates")]
    private static readonly List<RaycastHit2D> npcLineHits = new List<RaycastHit2D>();
    private static int npcLineCheckLayerMask;
    private static bool npcLineCheckLayerMaskReady;

    /// <summary>Whether nothing that blocks vision lies between two points.</summary>
    /// <param name="from">Where the line starts.</param>
    /// <param name="to">Where the line ends.</param>
    /// <param name="ignoreA">A transform (and its children) the line passes through, such as the looker.</param>
    /// <param name="ignoreB">A second transform (and its children) the line passes through, such as the target.</param>
    public static bool HasLineOfSight(Vector2 from, Vector2 to, Transform ignoreA, Transform ignoreB)
    {
        return !IsLineBlocked(from, to, ignoreA, ignoreB, false);
    }

    /// <summary>Whether nothing that blocks projectiles lies between two points.</summary>
    /// <param name="from">Where the line starts.</param>
    /// <param name="to">Where the line ends.</param>
    /// <param name="ignoreA">A transform (and its children) the line passes through, such as the shooter.</param>
    /// <param name="ignoreB">A second transform (and its children) the line passes through, such as the target.</param>
    public static bool HasLineOfFire(Vector2 from, Vector2 to, Transform ignoreA, Transform ignoreB)
    {
        return !IsLineBlocked(from, to, ignoreA, ignoreB, true);
    }

    /// <summary>
    /// Checks what stands between a shooter and its target. Walls and non-friendly ents that block shots make it Blocked;
    /// otherwise a living friendly character within cfgNpcFriendlyFireClearanceRadius of the line makes it BlockedByFriendly.
    /// </summary>
    /// <param name="shooter">The character shooting; its faction decides who counts as friendly.</param>
    /// <param name="from">Where the shot starts.</param>
    /// <param name="to">Where the shot is aimed.</param>
    /// <param name="target">The target being shot, ignored by line checks.</param>
    public static enumNpcLineOfFire GetLineOfFireStatus(F_Character shooter, Vector2 from, Vector2 to, Transform target)
    {
        if (IsLineBlocked(from, to, shooter.transform, target, true, shooter))
        {
            return enumNpcLineOfFire.Blocked;
        }

        return IsFriendlyNearLine(shooter, from, to, target) ? enumNpcLineOfFire.BlockedByFriendly : enumNpcLineOfFire.Clear;
    }

    /// <summary>Whether a point lies within a looker's sight cone, centred on the direction it faces.</summary>
    /// <param name="lookPosition">Where the looker sees from.</param>
    /// <param name="facingAngle">The direction the looker faces, in degrees.</param>
    /// <param name="sightConeSize">Total width of the cone in degrees; 360 or more sees all around.</param>
    /// <param name="targetPosition">The point being checked.</param>
    public static bool IsInSightCone(Vector2 lookPosition, float facingAngle, float sightConeSize, Vector2 targetPosition)
    {
        if (sightConeSize >= npcFullCircleDegrees)
        {
            return true;
        }

        Vector2 toTarget = targetPosition - lookPosition;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(facingAngle, targetAngle)) <= sightConeSize * npcSightConeHalfFraction;
    }

    /// <summary>
    /// Finds the closest living character the looker's faction is hostile to, within range, inside its sight cone and in line of sight.
    /// </summary>
    /// <param name="looker">The character searching; its faction decides who counts as hostile.</param>
    /// <param name="lookPosition">Where the looker sees from.</param>
    /// <param name="detectionRange">Characters further than this are ignored.</param>
    /// <param name="facingAngle">The direction the looker faces, in degrees.</param>
    /// <param name="sightConeSize">Total width of the looker's sight cone in degrees; 360 sees all around.</param>
    /// <returns>The closest visible hostile character, or null when there is none.</returns>
    public static F_Character FindClosestVisibleHostile(F_Character looker, Vector2 lookPosition, float detectionRange,
        float facingAngle = 0f, float sightConeSize = npcFullCircleDegrees)
    {
        IReadOnlyList<F_Character> activeCharacters = F_Character.ActiveCharacters;
        float closestDistanceSqr = detectionRange * detectionRange;
        F_Character closestHostile = null;

        for (int characterIndex = 0; characterIndex < activeCharacters.Count; characterIndex++)
        {
            F_Character candidate = activeCharacters[characterIndex];
            if (!IsVisibleHostile(looker, candidate, lookPosition, closestDistanceSqr, facingAngle, sightConeSize, out float distanceSqr))
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            closestHostile = candidate;
        }

        return closestHostile;
    }

    /// <summary>
    /// Collects every living character the looker's faction is hostile to, within range, inside its sight cone and in line of sight.
    /// </summary>
    /// <param name="looker">The character searching; its faction decides who counts as hostile.</param>
    /// <param name="lookPosition">Where the looker sees from.</param>
    /// <param name="detectionRange">Characters further than this are ignored.</param>
    /// <param name="facingAngle">The direction the looker faces, in degrees.</param>
    /// <param name="sightConeSize">Total width of the looker's sight cone in degrees; 360 sees all around.</param>
    /// <param name="visibleHostiles">Cleared, then filled with every visible hostile character.</param>
    public static void FindVisibleHostiles(F_Character looker, Vector2 lookPosition, float detectionRange, float facingAngle,
        float sightConeSize, List<F_Character> visibleHostiles)
    {
        visibleHostiles.Clear();
        IReadOnlyList<F_Character> activeCharacters = F_Character.ActiveCharacters;
        float detectionRangeSqr = detectionRange * detectionRange;

        for (int characterIndex = 0; characterIndex < activeCharacters.Count; characterIndex++)
        {
            F_Character candidate = activeCharacters[characterIndex];
            if (IsVisibleHostile(looker, candidate, lookPosition, detectionRangeSqr, facingAngle, sightConeSize, out _))
            {
                visibleHostiles.Add(candidate);
            }
        }
    }

    // Whether a candidate is a living hostile of the looker, within range, inside its sight cone and in line of sight.
    private static bool IsVisibleHostile(F_Character looker, F_Character candidate, Vector2 lookPosition, float detectionRangeSqr,
        float facingAngle, float sightConeSize, out float distanceSqr)
    {
        distanceSqr = float.MaxValue;
        if (candidate == null || candidate == looker || candidate.IsDead || !F_Utility_Helper_Faction.IsHostile(looker, candidate))
        {
            return false;
        }

        Vector2 candidatePosition = candidate.transform.position;
        distanceSqr = (candidatePosition - lookPosition).sqrMagnitude;
        return distanceSqr <= detectionRangeSqr &&
               IsInSightCone(lookPosition, facingAngle, sightConeSize, candidatePosition) &&
               HasLineOfSight(lookPosition, candidatePosition, looker.transform, candidate.transform);
    }

    /// <summary>
    /// Finds the friendly character a body guard should guard: a friendly player within the priority range always wins,
    /// otherwise the closest living ally within range. Other body guards are skipped so guards never guard each other.
    /// </summary>
    /// <param name="guard">The body guard searching; its faction decides who counts as an ally.</param>
    /// <param name="guardPosition">Where the guard is.</param>
    /// <param name="searchRange">Allies further than this are ignored.</param>
    /// <param name="playerPriorityRange">A friendly player within this distance is always chosen.</param>
    /// <returns>The ally to guard, or null when there is none.</returns>
    public static F_Character FindGuardWard(F_Character guard, Vector2 guardPosition, float searchRange, float playerPriorityRange)
    {
        IReadOnlyList<F_Character> activeCharacters = F_Character.ActiveCharacters;
        float playerPriorityRangeSqr = playerPriorityRange * playerPriorityRange;
        float closestDistanceSqr = searchRange * searchRange;
        F_Character closestAlly = null;

        for (int characterIndex = 0; characterIndex < activeCharacters.Count; characterIndex++)
        {
            F_Character candidate = activeCharacters[characterIndex];
            bool isOtherGuard = candidate is F_NPC candidateNpc && candidateNpc.IsBodyGuard;
            if (candidate == null || candidate == guard || candidate.IsDead || isOtherGuard || !F_Utility_Helper_Faction.IsFriendly(guard, candidate))
            {
                continue;
            }

            float distanceSqr = ((Vector2)candidate.transform.position - guardPosition).sqrMagnitude;
            if (candidate is F_PlayerStats && distanceSqr <= playerPriorityRangeSqr)
            {
                return candidate;
            }

            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestAlly = candidate;
            }
        }

        return closestAlly;
    }

    /// <summary>
    /// Finds where a projectile fired now at a fixed speed would meet a target moving at a constant velocity, so a
    /// shooter can lead its shot. Fails when the projectile can never catch the target.
    /// </summary>
    /// <param name="shotOrigin">Where the projectile spawns.</param>
    /// <param name="targetPosition">The target's current position.</param>
    /// <param name="targetVelocity">The target's current velocity.</param>
    /// <param name="projectileSpeed">The projectile's speed in units per second.</param>
    /// <param name="interceptPoint">Where to aim to hit the target.</param>
    /// <returns>True when an intercept point was found.</returns>
    public static bool TryGetInterceptPoint(Vector2 shotOrigin, Vector2 targetPosition, Vector2 targetVelocity, float projectileSpeed, out Vector2 interceptPoint)
    {
        interceptPoint = targetPosition;
        if (projectileSpeed <= 0f)
        {
            return false;
        }

        // Solve |offset + velocity * t| = speed * t for the earliest positive time t
        Vector2 targetOffset = targetPosition - shotOrigin;
        float quadraticA = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float quadraticB = 2f * Vector2.Dot(targetOffset, targetVelocity);
        float quadraticC = Vector2.Dot(targetOffset, targetOffset);
        float interceptTime;

        if (Mathf.Abs(quadraticA) < npcInterceptEpsilon)
        {
            if (Mathf.Abs(quadraticB) < npcInterceptEpsilon)
            {
                return false;
            }

            interceptTime = -quadraticC / quadraticB;
        }
        else
        {
            float discriminant = quadraticB * quadraticB - 4f * quadraticA * quadraticC;
            if (discriminant < 0f)
            {
                return false;
            }

            float discriminantRoot = Mathf.Sqrt(discriminant);
            float timeFirst = (-quadraticB - discriminantRoot) / (2f * quadraticA);
            float timeSecond = (-quadraticB + discriminantRoot) / (2f * quadraticA);
            float timeEarliest = Mathf.Min(timeFirst, timeSecond);
            interceptTime = timeEarliest > 0f ? timeEarliest : Mathf.Max(timeFirst, timeSecond);
        }

        if (interceptTime <= 0f)
        {
            return false;
        }

        interceptPoint = targetPosition + targetVelocity * interceptTime;
        return true;
    }

    /// <summary>
    /// Distance from a point to the closest edge of a target's solid colliders (0 when inside one), so reach checks
    /// work for targets of any size. Falls back to the target's centre when it has no solid colliders.
    /// </summary>
    /// <param name="fromPosition">Where the distance is measured from, such as an attacker's centre.</param>
    /// <param name="target">The target being measured to.</param>
    /// <param name="targetColliders">The target's colliders; triggers and disabled colliders are skipped.</param>
    public static float GetDistanceToBody(Vector2 fromPosition, Transform target, Collider2D[] targetColliders)
    {
        float closestDistance = Vector2.Distance(fromPosition, target.position);
        if (targetColliders == null)
        {
            return closestDistance;
        }

        for (int colliderIndex = 0; colliderIndex < targetColliders.Length; colliderIndex++)
        {
            Collider2D targetCollider = targetColliders[colliderIndex];
            if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
            {
                continue;
            }

            float edgeDistance = Vector2.Distance(fromPosition, targetCollider.ClosestPoint(fromPosition));
            closestDistance = Mathf.Min(closestDistance, edgeDistance);
        }

        return closestDistance;
    }

    /// <summary>Finds the closest NavMesh point to a search origin that the target can't shoot.</summary>
    /// <param name="searchOrigin">Where the search is centred; closer points are preferred.</param>
    /// <param name="searchRadius">Furthest distance from the origin to search.</param>
    /// <param name="self">The NPC searching, ignored by line checks.</param>
    /// <param name="target">The target to hide from, ignored by line checks.</param>
    /// <param name="maxTargetDistance">Cover further than this from the target is ignored.</param>
    /// <param name="coverPoint">The cover point found.</param>
    /// <returns>True when cover was found.</returns>
    public static bool TryFindCoverPoint(Vector2 searchOrigin, float searchRadius, Transform self, Transform target,
        float maxTargetDistance, out Vector2 coverPoint)
    {
        Vector2 targetPosition = target.position;
        float minTargetDistanceSqr = F_Utility_Config_Npc.cfgNpcCoverMinTargetDistance * F_Utility_Config_Npc.cfgNpcCoverMinTargetDistance;
        float maxTargetDistanceSqr = maxTargetDistance * maxTargetDistance;
        return TryFindClosestPoint(searchOrigin, searchRadius, F_Utility_Config_Npc.cfgNpcCoverSearchRings,
            F_Utility_Config_Npc.cfgNpcCoverSearchPointsPerRing, candidate =>
            {
                float targetDistanceSqr = (candidate - targetPosition).sqrMagnitude;
                return targetDistanceSqr >= minTargetDistanceSqr && targetDistanceSqr <= maxTargetDistanceSqr &&
                       !HasLineOfFire(targetPosition, candidate, self, target);
            }, out coverPoint);
    }

    /// <summary>Finds the closest NavMesh point to a search origin with a clear line of fire (no walls or friendlies in the way) to the target within range.</summary>
    /// <param name="searchOrigin">Where the search is centred; closer points are preferred.</param>
    /// <param name="self">The NPC searching, ignored by line checks; its faction decides who counts as friendly.</param>
    /// <param name="target">The target to shoot, ignored by line checks.</param>
    /// <param name="maxTargetDistance">Points further than this from the target are ignored.</param>
    /// <param name="peekPoint">The peek point found.</param>
    /// <returns>True when a peek point was found.</returns>
    public static bool TryFindPeekPoint(Vector2 searchOrigin, F_Character self, Transform target, float maxTargetDistance, out Vector2 peekPoint)
    {
        Vector2 targetPosition = target.position;
        float maxTargetDistanceSqr = maxTargetDistance * maxTargetDistance;
        return TryFindClosestPoint(searchOrigin, F_Utility_Config_Npc.cfgNpcPeekSearchRadius, F_Utility_Config_Npc.cfgNpcPeekSearchRings,
            F_Utility_Config_Npc.cfgNpcPeekSearchPointsPerRing, candidate =>
                (candidate - targetPosition).sqrMagnitude <= maxTargetDistanceSqr &&
                GetLineOfFireStatus(self, candidate, targetPosition, target) == enumNpcLineOfFire.Clear, out peekPoint);
    }

    /// <summary>
    /// Finds a point round the side of the target, preferring cover near it (behind walls or other NPCs). Tries the
    /// given side first and flips to the other side when that one has no NavMesh.
    /// </summary>
    /// <param name="self">The flanking NPC.</param>
    /// <param name="target">The target being flanked.</param>
    /// <param name="flankSide">1 or -1 for the side to try first; updated to the side used.</param>
    /// <param name="flankPoint">The flank point found.</param>
    /// <returns>True when a flank point was found.</returns>
    public static bool TryFindFlankPoint(Transform self, Transform target, ref float flankSide, out Vector2 flankPoint)
    {
        Vector2 targetPosition = target.position;
        Vector2 targetToSelf = (Vector2)self.position - targetPosition;
        float flankRadius = Mathf.Max(F_Utility_Config_Npc.cfgNpcChargeDistance, targetToSelf.magnitude * F_Utility_Config_Npc.cfgNpcFlankDistanceFraction);
        Vector2 flankDirection = targetToSelf.sqrMagnitude > 0f ? targetToSelf.normalized : Vector2.right;

        for (int sideAttempt = 0; sideAttempt < 2; sideAttempt++)
        {
            Vector2 sideDirection = Quaternion.Euler(0f, 0f, flankSide * F_Utility_Config_Npc.cfgNpcFlankAngleDegrees) * flankDirection;
            Vector2 candidate = targetPosition + sideDirection * flankRadius;
            if (F_Utility_Helper_Navigation.TrySamplePosition(candidate, out Vector2 navCandidate))
            {
                flankPoint = TryFindCoverPoint(navCandidate, F_Utility_Config_Npc.cfgNpcPeekSearchRadius, self, target, float.MaxValue, out Vector2 flankCover)
                    ? flankCover
                    : navCandidate;
                return true;
            }

            flankSide = -flankSide;
        }

        flankPoint = targetPosition;
        return false;
    }

    // Tests the origin, then rings of points around it, and returns the valid NavMesh point closest to the origin.
    private static bool TryFindClosestPoint(Vector2 origin, float radius, int ringCount, int pointsPerRing, Func<Vector2, bool> isValid, out Vector2 bestPoint)
    {
        if (F_Utility_Helper_Navigation.TrySamplePosition(origin, out Vector2 originPoint) && isValid(originPoint))
        {
            bestPoint = originPoint;
            return true;
        }

        int safeRingCount = Mathf.Max(1, ringCount);
        int safePointsPerRing = Mathf.Max(1, pointsPerRing);
        float angleStep = npcFullCircleDegrees / safePointsPerRing;
        float bestDistanceSqr = float.MaxValue;
        bestPoint = origin;

        // Rings run from the inside out, so the first ring with a valid point holds the closest one
        for (int ringIndex = 1; ringIndex <= safeRingCount; ringIndex++)
        {
            float ringRadius = radius * ringIndex / safeRingCount;
            float ringAngleOffset = ringIndex % 2 == 0 ? angleStep * npcRingAngleOffsetFraction : 0f;
            for (int pointIndex = 0; pointIndex < safePointsPerRing; pointIndex++)
            {
                float angleRadians = (pointIndex * angleStep + ringAngleOffset) * Mathf.Deg2Rad;
                Vector2 candidate = origin + new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * ringRadius;
                if (!F_Utility_Helper_Navigation.TrySamplePosition(candidate, out Vector2 navCandidate) || !isValid(navCandidate))
                {
                    continue;
                }

                float distanceSqr = (navCandidate - origin).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestPoint = navCandidate;
                }
            }

            if (bestDistanceSqr < float.MaxValue)
            {
                return true;
            }
        }

        return false;
    }

    // Walks the solid colliders along a line; non-ent colliders always block, ents block unless they let sight or shots
    // through, or are friendly to ignoreFriendliesOf when one is given.
    private static bool IsLineBlocked(Vector2 from, Vector2 to, Transform ignoreA, Transform ignoreB, bool isLineOfFire, F_Character ignoreFriendliesOf = null)
    {
        ContactFilter2D lineFilter = new ContactFilter2D { useTriggers = false };
        lineFilter.SetLayerMask(GetLineCheckLayerMask());
        npcLineHits.Clear();
        Physics2D.Linecast(from, to, lineFilter, npcLineHits);

        for (int hitIndex = 0; hitIndex < npcLineHits.Count; hitIndex++)
        {
            Collider2D hitCollider = npcLineHits[hitIndex].collider;
            if (hitCollider == null || IsPartOf(hitCollider.transform, ignoreA) || IsPartOf(hitCollider.transform, ignoreB))
            {
                continue;
            }

            F_Ent hitEnt = hitCollider.GetComponentInParent<F_Ent>();
            if (hitEnt == null)
            {
                return true;
            }

            if (hitEnt.IsDead || (ignoreFriendliesOf != null && F_Utility_Helper_Faction.IsFriendly(ignoreFriendliesOf, hitEnt)))
            {
                continue;
            }

            bool letsLineThrough = isLineOfFire ? hitEnt.entFeatureCanShootOver : hitEnt.entFeatureCanSeeThrough;
            if (!letsLineThrough)
            {
                return true;
            }
        }

        return false;
    }

    // Sweeps a circle of cfgNpcFriendlyFireClearanceRadius along the line and reports whether it touches a living
    // character friendly to the shooter that shots can't pass over.
    private static bool IsFriendlyNearLine(F_Character shooter, Vector2 from, Vector2 to, Transform target)
    {
        Vector2 lineOffset = to - from;
        float lineLength = lineOffset.magnitude;
        if (lineLength <= 0f)
        {
            return false;
        }

        ContactFilter2D friendlyFilter = new ContactFilter2D { useTriggers = false };
        friendlyFilter.SetLayerMask(GetLineCheckLayerMask());
        npcLineHits.Clear();
        Physics2D.CircleCast(from, F_Utility_Config_Npc.cfgNpcFriendlyFireClearanceRadius, lineOffset / lineLength, friendlyFilter, npcLineHits, lineLength);

        for (int hitIndex = 0; hitIndex < npcLineHits.Count; hitIndex++)
        {
            Collider2D hitCollider = npcLineHits[hitIndex].collider;
            if (hitCollider == null || IsPartOf(hitCollider.transform, shooter.transform) || IsPartOf(hitCollider.transform, target))
            {
                continue;
            }

            F_Character hitCharacter = hitCollider.GetComponentInParent<F_Character>();
            if (hitCharacter != null && !hitCharacter.IsDead && !hitCharacter.entFeatureCanShootOver &&
                F_Utility_Helper_Faction.IsFriendly(shooter, hitCharacter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartOf(Transform candidate, Transform owner)
    {
        return owner != null && candidate.IsChildOf(owner);
    }

    // Default raycast layers (which already skip Ignore Raycast, used by the walkable base) minus projectiles in flight.
    private static int GetLineCheckLayerMask()
    {
        if (!npcLineCheckLayerMaskReady)
        {
            npcLineCheckLayerMask = Physics2D.DefaultRaycastLayers & ~LayerMask.GetMask(F_Utility_Config_Physics.cfgPhysicsLayerProjectile);
            npcLineCheckLayerMaskReady = true;
        }

        return npcLineCheckLayerMask;
    }
}
