using UnityEngine;

/// <summary>
/// Draws a small thought icon just above an NPC's head. Added automatically by F_NPC, which decides which thought to
/// show; sprites and display durations come from F_Utility_Config_NpcThoughts. Timed thoughts hide themselves once their
/// duration runs out, the rest stay until cleared or replaced.
/// </summary>
[RequireComponent(typeof(F_NPC))]
public class F_NPC_Thought : MonoBehaviour
{
    [Header("Constants Private")]
    private const string thoughtObjectName = "NPC_Thought";

    [Header("Privates")]
    private F_NPC thoughtNpc;
    private Collider2D thoughtNpcCollider;
    private Transform thoughtTransform;
    private SpriteRenderer thoughtRenderer;
    private enumNpcThought thoughtCurrent = enumNpcThought.None;
    private Sprite thoughtSprite;
    private bool thoughtIsTimed;
    private float thoughtHideTime;

    /// <summary>The thought currently shown, or None.</summary>
    public enumNpcThought CurrentThought => thoughtCurrent;

    private void Awake()
    {
        thoughtNpc = GetComponent<F_NPC>();
        thoughtNpcCollider = GetComponent<Collider2D>();
        CreateThoughtObject();
    }

    /// <summary>Shows a thought, replacing the current one and restarting its display duration when it has one.</summary>
    /// <param name="thought">The thought to show; None hides the icon.</param>
    public void ShowThought(enumNpcThought thought)
    {
        F_Utility_Config_NpcThoughts.NpcThoughtSettings settings = F_Utility_Config_NpcThoughts.GetThoughtSettings(thought);
        float displayDuration = settings != null ? settings.thoughtDisplayDuration : F_Utility_Config_NpcThoughts.cfgNpcThoughtDisplayDurationUntilCleared;

        thoughtCurrent = thought;
        thoughtIsTimed = displayDuration > F_Utility_Config_NpcThoughts.cfgNpcThoughtDisplayDurationUntilCleared;
        thoughtHideTime = Time.time + displayDuration;

        thoughtSprite = settings != null ? settings.thoughtSprite : null;
    }

    /// <summary>Hides the icon only if it is currently showing this thought, so a newer thought is never cleared by mistake.</summary>
    /// <param name="thought">The thought to clear.</param>
    public void ClearThought(enumNpcThought thought)
    {
        if (thoughtCurrent == thought)
        {
            ShowThought(enumNpcThought.None);
        }
    }

    private void LateUpdate()
    {
        bool hasExpired = thoughtIsTimed && Time.time >= thoughtHideTime;
        if (thoughtCurrent != enumNpcThought.None && (hasExpired || thoughtNpc.IsDead))
        {
            ShowThought(enumNpcThought.None);
        }

        EnsureThoughtObjectExists();
        bool isVisible = thoughtCurrent != enumNpcThought.None && thoughtSprite != null;
        if (thoughtRenderer.enabled != isVisible)
        {
            thoughtRenderer.enabled = isVisible;
        }

        if (isVisible)
        {
            UpdateThoughtPlacement();
        }
    }

    // Sits the icon centred just above the NPC's body; never rotates with the NPC and draws over its sprites.
    private void UpdateThoughtPlacement()
    {
        Bounds bodyBounds = thoughtNpcCollider != null && thoughtNpcCollider.enabled
            ? thoughtNpcCollider.bounds
            : F_Utility_Helper_Damage.GetEntBounds(thoughtNpc);
        float scale = F_Utility_Config_NpcThoughts.cfgNpcThoughtScale;
        float iconHalfHeight = thoughtSprite.bounds.extents.y * scale;

        thoughtRenderer.sprite = thoughtSprite;

        thoughtTransform.rotation = Quaternion.identity;
        thoughtTransform.localScale = Vector3.one * scale;
        thoughtTransform.position = new Vector3(
            bodyBounds.center.x,
            bodyBounds.max.y + F_Utility_Config_NpcThoughts.cfgNpcThoughtOffsetY + iconHalfHeight,
            thoughtNpc.transform.position.z);

        F_Utility_Helper_Damage.GetEntSorting(thoughtNpc, out int sortingLayerId, out int sortingOrder);
        thoughtRenderer.sortingLayerID = sortingLayerId;
        thoughtRenderer.sortingOrder = sortingOrder + F_Utility_Config_NpcThoughts.cfgNpcThoughtSortingOrderOffset;
    }

    // Builds the icon as an unparented object so the NPC's rotation and scale never distort it.
    private void CreateThoughtObject()
    {
        GameObject thoughtObject = new GameObject($"{thoughtObjectName}_{name}");
        thoughtTransform = thoughtObject.transform;
        thoughtRenderer = thoughtObject.AddComponent<SpriteRenderer>();
        thoughtRenderer.enabled = false;
    }

    // The icon is unparented, so a scene unload can destroy it while a persistent NPC lives on.
    private void EnsureThoughtObjectExists()
    {
        if (thoughtRenderer == null)
        {
            DestroyThoughtObject();
            CreateThoughtObject();
        }
    }

    private void DestroyThoughtObject()
    {
        if (thoughtTransform != null)
        {
            Destroy(thoughtTransform.gameObject);
        }
    }

    private void OnDestroy()
    {
        DestroyThoughtObject();
    }
}
