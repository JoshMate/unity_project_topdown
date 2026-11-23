using UnityEngine;

/// <summary>
/// Draws a held firearm sprite in front of its holder and keeps the fire position on the sprite's barrel tip
/// (its rightmost opaque edge), so any held weapon sprite works without manual fire point setup.
/// </summary>
public class F_playerHeldWeaponIndividual : MonoBehaviour
{
    [Header("Object Refs")]
    public Transform firePosition;
    // The sprite shown is set directly on this renderer
    public SpriteRenderer spriteRenderer;

    [Header("Privates")]
    private Sprite heldWeaponAppliedSprite;
    private float heldWeaponAppliedStartDistance;

    /// <summary>Shows the given sprite starting at the holder's body edge with the fire position on its barrel tip, or hides it when null.</summary>
    /// <param name="heldSprite">The held weapon sprite to draw, pointing right (+X).</param>
    /// <param name="holderFrontEdgeDistance">Distance from the holder's centre to the front edge of its body sprite, along the aim direction.</param>
    public void SetHeldSprite(Sprite heldSprite, float holderFrontEdgeDistance)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = heldSprite != null;
        float heldWeaponStartDistance = holderFrontEdgeDistance + F_Utility_Config_HeldWeapons.cfgHeldWeaponFirearmEdgeOffset;
        if (heldSprite == heldWeaponAppliedSprite && Mathf.Approximately(heldWeaponStartDistance, heldWeaponAppliedStartDistance))
        {
            return;
        }

        heldWeaponAppliedSprite = heldSprite;
        heldWeaponAppliedStartDistance = heldWeaponStartDistance;
        spriteRenderer.sprite = heldSprite;
        if (heldSprite == null)
        {
            return;
        }

        // Places the rear of the sprite at the front edge of the holder's body
        Transform rendererTransform = spriteRenderer.transform;
        rendererTransform.localPosition = new Vector3(
            heldWeaponStartDistance - heldSprite.bounds.min.x,
            0f,
            rendererTransform.localPosition.z);

        if (firePosition != null)
        {
            Vector2 barrelTipLocal = F_Utility_Helper_Sprite.GetRightEdgeCentre(heldSprite);
            firePosition.position = rendererTransform.TransformPoint(barrelTipLocal);
        }
    }
}
