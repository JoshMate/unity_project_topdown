using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a character's held melee weapon sprite just in front of them and, while swinging, moves it along the weapon's
/// swing path. The sprite's physics shape is the hitbox: only ents it actually touches are hit, up to the cleave amount.
/// Place on a child of the character so its local +X follows the aim direction.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D), typeof(Rigidbody2D))]
public class F_Character_MeleeHitbox : MonoBehaviour
{
    [Header("Object Refs")]
    // Shows the held melee weapon's sprite
    public SpriteRenderer meleeHitboxSpriteRenderer;
    // Rebuilt from the held weapon sprite's physics shape; only enabled during the damaging swing phase
    public PolygonCollider2D meleeHitboxCollider;

    [Header("Privates")]
    private F_Item_Weapon_Melee meleeHitboxWeapon;
    private F_Character meleeHitboxHolder;
    private bool meleeHitboxIsSwinging;
    private float meleeHitboxSwingStartTime;
    private int meleeHitboxHitCount;
    private readonly List<Collider2D> meleeHitboxOverlapResults = new List<Collider2D>();
    private readonly HashSet<F_Ent> meleeHitboxHitTargets = new HashSet<F_Ent>();
    private readonly List<F_Ent> meleeHitboxNewTargets = new List<F_Ent>();
    private readonly List<float> meleeHitboxNewTargetDistances = new List<float>();

    /// <summary>Whether a swing is currently in progress.</summary>
    public bool IsSwinging => meleeHitboxIsSwinging;

    private void Awake()
    {
        EnsureReferences();
        Rigidbody2D hitboxBody = GetComponent<Rigidbody2D>();
        hitboxBody.bodyType = RigidbodyType2D.Kinematic;
        meleeHitboxCollider.isTrigger = true;
        meleeHitboxSpriteRenderer.sortingLayerName = F_Utility_Config_Melee.cfgMeleeSortingLayerName;
        ApplyHeldWeaponVisual();
    }

    private void OnValidate()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (meleeHitboxSpriteRenderer == null)
        {
            meleeHitboxSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (meleeHitboxCollider == null)
        {
            meleeHitboxCollider = GetComponent<PolygonCollider2D>();
        }
    }

    /// <summary>Shows the given melee weapon in front of the holder, or hides the hitbox when null. Cancels any swing in progress when the weapon changes.</summary>
    /// <param name="meleeWeapon">The melee weapon now held, or null when none is held.</param>
    public void SetHeldWeapon(F_Item_Weapon_Melee meleeWeapon)
    {
        if (meleeWeapon == meleeHitboxWeapon)
        {
            return;
        }

        meleeHitboxWeapon = meleeWeapon;
        EndSwing();
        ApplyHeldWeaponVisual();
    }

    /// <summary>Starts swinging the held weapon; ents the sprite touches during the swing are hit.</summary>
    /// <param name="weaponHolder">The character swinging, who is never hit by their own swing.</param>
    public void BeginSwing(F_Character weaponHolder)
    {
        if (meleeHitboxWeapon == null)
        {
            return;
        }

        meleeHitboxHolder = weaponHolder;
        meleeHitboxSwingStartTime = Time.time;
        meleeHitboxHitCount = 0;
        meleeHitboxHitTargets.Clear();
        meleeHitboxIsSwinging = true;
    }

    private void Update()
    {
        if (meleeHitboxWeapon == null)
        {
            return;
        }

        if (!meleeHitboxIsSwinging)
        {
            ApplyHoldPose();
            return;
        }

        float swingElapsed = Time.time - meleeHitboxSwingStartTime;
        bool isDamaging = ApplySwingPose(swingElapsed);

        // Only the swing phase deals damage; winding up and returning never hit anything
        bool canHit = isDamaging && meleeHitboxHitCount < meleeHitboxWeapon.MeleeCleaveAmountEffective;
        meleeHitboxCollider.enabled = canHit;
        if (canHit)
        {
            HitTouchingTargets();
        }

        if (swingElapsed >= meleeHitboxWeapon.MeleeSwingCycleDuration)
        {
            EndSwing();
        }
    }

    private void EndSwing()
    {
        meleeHitboxIsSwinging = false;
        meleeHitboxHolder = null;
        meleeHitboxHitTargets.Clear();
        if (meleeHitboxCollider != null)
        {
            meleeHitboxCollider.enabled = false;
        }
    }

    // Shows the held weapon's sprite and rebuilds the hitbox from its physics shape.
    private void ApplyHeldWeaponVisual()
    {
        Sprite weaponSprite = meleeHitboxWeapon != null ? meleeHitboxWeapon.ItemSprite : null;
        meleeHitboxSpriteRenderer.sprite = weaponSprite;
        meleeHitboxSpriteRenderer.enabled = weaponSprite != null;

        if (!F_Utility_Helper_Sprite.ApplyPhysicsShape(meleeHitboxCollider, weaponSprite))
        {
            meleeHitboxCollider.pathCount = 0;
        }

        meleeHitboxCollider.enabled = false;
        if (meleeHitboxWeapon != null)
        {
            ApplyHoldPose();
        }
    }

    // The end of a swing cycle is the fully returned holding position.
    private void ApplyHoldPose()
    {
        ApplySwingPose(meleeHitboxWeapon.MeleeSwingCycleDuration);
    }

    // Moves the sprite to its pose at this point of the swing and returns whether that point deals damage.
    private bool ApplySwingPose(float swingElapsed)
    {
        bool isDamaging = meleeHitboxWeapon.EvaluateSwingPose(swingElapsed, out Vector2 localPosition, out float localAngleDegrees);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.Euler(0f, 0f, localAngleDegrees + F_Utility_Config_Melee.cfgMeleeSpriteForwardAngleOffset);
        return isDamaging;
    }

    // Hits ents newly touching the hitbox, closest to the holder first, until the cleave amount is used up.
    private void HitTouchingTargets()
    {
        int cleaveAmount = meleeHitboxWeapon.MeleeCleaveAmountEffective;
        if (meleeHitboxHitCount >= cleaveAmount)
        {
            return;
        }

        Vector2 knockbackOrigin = meleeHitboxHolder != null ? (Vector2)meleeHitboxHolder.transform.position : (Vector2)transform.position;
        CollectNewTouchingTargets(knockbackOrigin);

        while (meleeHitboxHitCount < cleaveAmount && meleeHitboxNewTargets.Count > 0)
        {
            int closestIndex = GetClosestNewTargetIndex();
            F_Ent hitTarget = meleeHitboxNewTargets[closestIndex];
            meleeHitboxNewTargets.RemoveAt(closestIndex);
            meleeHitboxNewTargetDistances.RemoveAt(closestIndex);

            meleeHitboxHitTargets.Add(hitTarget);
            meleeHitboxHitCount++;
            meleeHitboxWeapon.ApplyHit(hitTarget, meleeHitboxHolder, knockbackOrigin);
        }

        if (meleeHitboxHitCount >= cleaveAmount)
        {
            meleeHitboxCollider.enabled = false;
        }
    }

    // Ents can have several colliders; each ent counts once, at its distance from the holder.
    private void CollectNewTouchingTargets(Vector2 knockbackOrigin)
    {
        meleeHitboxNewTargets.Clear();
        meleeHitboxNewTargetDistances.Clear();
        meleeHitboxOverlapResults.Clear();

        // The hitbox moves through its transform, so push that pose into physics before querying
        Physics2D.SyncTransforms();
        meleeHitboxCollider.Overlap(ContactFilter2D.noFilter, meleeHitboxOverlapResults);

        for (int colliderIndex = 0; colliderIndex < meleeHitboxOverlapResults.Count; colliderIndex++)
        {
            Collider2D overlapCollider = meleeHitboxOverlapResults[colliderIndex];
            F_Ent overlapEnt = overlapCollider != null ? overlapCollider.GetComponentInParent<F_Ent>() : null;
            if (overlapEnt == null || overlapEnt.IsDead || overlapEnt == meleeHitboxHolder || transform.IsChildOf(overlapEnt.transform) || overlapEnt.entFeatureCanShootOver ||
                meleeHitboxHitTargets.Contains(overlapEnt) || meleeHitboxNewTargets.Contains(overlapEnt))
            {
                continue;
            }

            meleeHitboxNewTargets.Add(overlapEnt);
            meleeHitboxNewTargetDistances.Add(((Vector2)overlapEnt.transform.position - knockbackOrigin).sqrMagnitude);
        }
    }

    private int GetClosestNewTargetIndex()
    {
        int closestIndex = 0;
        for (int targetIndex = 1; targetIndex < meleeHitboxNewTargetDistances.Count; targetIndex++)
        {
            if (meleeHitboxNewTargetDistances[targetIndex] < meleeHitboxNewTargetDistances[closestIndex])
            {
                closestIndex = targetIndex;
            }
        }

        return closestIndex;
    }
}
