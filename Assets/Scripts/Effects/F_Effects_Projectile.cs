using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Effects_Projectile : MonoBehaviour
{
    // Ignore near-zero vectors so stopped bullets don't produce unstable angles.
    private const float DirectionEpsilon = 0.0001f;

    // Nudge particles just outside the contact surface to avoid clipping into it.
    private const float ImpactSurfaceOffset = 0.005f;

    public Rigidbody2D rb;
    public GameObject projectileImpactObject;
    public Color bloodColour;

    // Cache motion before collision response can reduce the Rigidbody velocity to zero.
    private Vector2 lastTravelDirection;

    void Update()
    {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > DirectionEpsilon)
        {
            lastTravelDirection = velocity.normalized;
            float aimAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rb.rotation = aimAngle;
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Prefer cached incoming motion; relative velocity is a fallback if the bullet has not moved yet.
        Vector2 incomingDirection = lastTravelDirection;
        if (incomingDirection.sqrMagnitude <= DirectionEpsilon)
        {
            incomingDirection = other.relativeVelocity.normalized;
        }

        // Default to the direction opposite travel until a valid contact normal is available.
        Vector2 outwardDirection = incomingDirection.sqrMagnitude > DirectionEpsilon
            ? -incomingDirection
            : Vector2.up;
        Vector2 impactPosition = transform.position;

        if (other.contactCount > 0)
        {
            ContactPoint2D contact = other.GetContact(0);
            Vector2 surfaceNormal = contact.normal;

            if (surfaceNormal.sqrMagnitude > DirectionEpsilon)
            {
                surfaceNormal.Normalize();

                // Contact normal orientation depends on the collision pair; flip it if it points with the bullet.
                if (incomingDirection.sqrMagnitude > DirectionEpsilon && Vector2.Dot(surfaceNormal, incomingDirection) > 0f)
                {
                    surfaceNormal = -surfaceNormal;
                }

                outwardDirection = surfaceNormal;
                impactPosition = contact.point + outwardDirection * ImpactSurfaceOffset;
            }
        }

        CreateProjectileImpact(other, impactPosition, outwardDirection);
        Destroy(gameObject);
    }

    void CreateProjectileImpact(Collision2D other, Vector2 impactPosition, Vector2 outwardDirection)
    {
        // Only entities have the blood-color data used by this impact prefab.
        if (projectileImpactObject == null || !other.gameObject.TryGetComponent<F_Ent>(out F_Ent hitEntity))
        {
            return;
        }

        float targetAngle = Mathf.Atan2(outwardDirection.y, outwardDirection.x) * Mathf.Rad2Deg;

        // Rotate the authored prefab emission axis toward the surface normal without discarding its setup rotation.
        Vector3 prefabEmissionAxis = projectileImpactObject.transform.forward;
        float prefabAxisAngle = Mathf.Atan2(prefabEmissionAxis.y, prefabEmissionAxis.x) * Mathf.Rad2Deg;
        Quaternion prefabRotation = projectileImpactObject.transform.rotation;
        Quaternion impactRotation = Quaternion.AngleAxis(targetAngle - prefabAxisAngle, Vector3.forward) * prefabRotation;
        GameObject impactObject = Instantiate(projectileImpactObject, impactPosition, impactRotation);

        if (impactObject.TryGetComponent<F_Effects_Projectile_Impact>(out F_Effects_Projectile_Impact impactEffect))
        {
            impactEffect.bloodColour = hitEntity.bloodColour;
        }
    }
}
