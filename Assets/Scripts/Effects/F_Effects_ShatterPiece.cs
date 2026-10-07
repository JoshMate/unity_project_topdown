using UnityEngine;

/// <summary>
/// Drives a single shattered sprite piece: scatters outward, spins, then fades, shrinks and destroys itself.
/// </summary>
public class F_Effects_ShatterPiece : MonoBehaviour
{
    [Header("Privates")]
    private Vector2 pieceVelocity;
    private float pieceSpinSpeed;
    private float pieceLifetime;
    private float pieceAge;
    private Vector3 pieceStartScale;
    private Color pieceStartColour;
    private Material pieceMaterial;
    private Mesh pieceMesh;

    /// <summary>Initialises the piece motion and the runtime assets it owns.</summary>
    /// <param name="velocity">Initial world velocity.</param>
    /// <param name="spinSpeed">Rotation speed in degrees per second.</param>
    /// <param name="lifetime">Seconds before the piece is destroyed.</param>
    /// <param name="material">Material instance owned by this piece.</param>
    /// <param name="mesh">Mesh owned by this piece.</param>
    public void Initialise(Vector2 velocity, float spinSpeed, float lifetime, Material material, Mesh mesh)
    {
        pieceVelocity = velocity;
        pieceSpinSpeed = spinSpeed;
        pieceLifetime = Mathf.Max(0.01f, lifetime);
        pieceMaterial = material;
        pieceMesh = mesh;
        pieceStartScale = transform.localScale;
        pieceStartColour = material.color;
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        pieceAge += deltaTime;
        if (pieceAge >= pieceLifetime)
        {
            Destroy(gameObject);
            return;
        }

        pieceVelocity *= Mathf.Max(0f, 1f - F_Utility_Config_Shatter.cfgShatterDragPerSecond * deltaTime);
        transform.position += (Vector3)(pieceVelocity * deltaTime);
        transform.Rotate(0f, 0f, pieceSpinSpeed * deltaTime);

        float fadeStart = pieceLifetime * (1f - F_Utility_Config_Shatter.cfgShatterFadeFraction);
        if (pieceAge > fadeStart)
        {
            float fadeProgress = (pieceAge - fadeStart) / (pieceLifetime - fadeStart);
            Color fadedColour = pieceStartColour;
            fadedColour.a *= 1f - fadeProgress;
            pieceMaterial.color = fadedColour;
            transform.localScale = pieceStartScale * Mathf.Lerp(1f, F_Utility_Config_Shatter.cfgShatterShrinkEndScale, fadeProgress);
        }
    }

    void OnDestroy()
    {
        if (pieceMaterial != null)
        {
            Destroy(pieceMaterial);
        }

        if (pieceMesh != null)
        {
            Destroy(pieceMesh);
        }
    }
}
