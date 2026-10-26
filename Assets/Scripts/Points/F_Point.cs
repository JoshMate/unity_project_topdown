using UnityEngine;

/// <summary>
/// A level-design placement marker. Its sprite is only visible in the editor; it has no collision and is hidden when the game runs.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class F_Point : MonoBehaviour
{
    [Header("Point Information")]
    // What this point is used for; game logic finds points by this type
    public enumPointType pointType = enumPointType.None;

    // Awake only runs at runtime, so the marker sprite stays visible in the editor
    protected virtual void Awake()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }
}
