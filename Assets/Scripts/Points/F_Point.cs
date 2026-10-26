using TMPro;
using UnityEngine;

/// <summary>
/// A level-design placement marker. Its sprite and name label are only visible in the editor; it has no collision and is hidden when the game runs.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class F_Point : MonoBehaviour
{
    [Header("Constants Private")]
    private const string pointLabelObjectName = "PointLabel";
    // Gap in world units kept between the label and each edge of the sprite, so text stays inside the border
    private const float pointLabelPadding = 0.1f;
    private const float pointLabelFontSizeMin = 0.8f;
    private const float pointLabelFontSizeMax = 2.5f;
    private const int pointLabelSortingBoost = 1;

    [Header("Point Information")]
    // Editor-only label drawn in the centre of the sprite, to identify the point while placing it
    public string pointName = "";
    // What this point is used for; game logic finds points by this type
    public enumPointType pointType = enumPointType.None;

#if UNITY_EDITOR
    [Header("Privates")]
    private TextMeshPro pointLabel;
    private string pointLabelShownName;
    private Sprite pointLabelShownSprite;
#endif

    // Runs in edit mode too ([ExecuteAlways]), so only hide the marker sprite while the game is running
    protected virtual void Awake()
    {
        if (Application.isPlaying)
        {
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        RefreshEditorLabel();
    }

    // In edit mode Update only runs when the scene changes, so this cheaply catches name, sprite, prefab and undo changes
    private void Update()
    {
        Sprite pointSprite = GetComponent<SpriteRenderer>().sprite;
        if (pointName != pointLabelShownName || pointSprite != pointLabelShownSprite)
        {
            RefreshEditorLabel();
        }
    }

    // Sprite picking ignores transparent pixels, so an invisible gizmo box covers the sprite to make the whole square clickable
    private void OnDrawGizmos()
    {
        Sprite pointSprite = GetComponent<SpriteRenderer>().sprite;
        if (pointSprite == null)
        {
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = F_Utility_Config_Colours.cfgColourPointPickArea;
        Gizmos.DrawCube(pointSprite.bounds.center, pointSprite.bounds.size);
    }

    // Creates, updates or removes the hidden, unsaved label that shows pointName inside the sprite
    private void RefreshEditorLabel()
    {
        if (Application.isPlaying)
        {
            return;
        }

        SpriteRenderer pointSpriteRenderer = GetComponent<SpriteRenderer>();
        pointLabelShownName = pointName;
        pointLabelShownSprite = pointSpriteRenderer.sprite;

        if (pointLabel == null)
        {
            // Reuse a label that survived a script reload or was copied when the point was duplicated
            Transform existingLabel = transform.Find(pointLabelObjectName);
            pointLabel = existingLabel != null ? existingLabel.GetComponent<TextMeshPro>() : null;
        }

        if (string.IsNullOrWhiteSpace(pointName) || pointSpriteRenderer.sprite == null)
        {
            if (pointLabel != null)
            {
                DestroyImmediate(pointLabel.gameObject);
            }
            return;
        }

        if (pointLabel == null)
        {
            pointLabel = CreateEditorLabel();
        }

        Bounds spriteBounds = pointSpriteRenderer.sprite.bounds;
        pointLabel.transform.localPosition = spriteBounds.center;
        pointLabel.rectTransform.sizeDelta = (Vector2)spriteBounds.size - Vector2.one * (pointLabelPadding * 2f);
        pointLabel.text = pointName;

        MeshRenderer pointLabelRenderer = pointLabel.GetComponent<MeshRenderer>();
        pointLabelRenderer.sortingLayerID = pointSpriteRenderer.sortingLayerID;
        pointLabelRenderer.sortingOrder = pointSpriteRenderer.sortingOrder + pointLabelSortingBoost;
    }

    // The label is hidden from the hierarchy, never saved into scenes or prefabs, and cannot be clicked
    private TextMeshPro CreateEditorLabel()
    {
        GameObject labelObject = new GameObject(pointLabelObjectName);
        labelObject.hideFlags = HideFlags.HideAndDontSave;
        labelObject.transform.SetParent(transform, false);

        TextMeshPro createdLabel = labelObject.AddComponent<TextMeshPro>();
        createdLabel.alignment = TextAlignmentOptions.Center;
        createdLabel.textWrappingMode = TextWrappingModes.Normal;
        createdLabel.overflowMode = TextOverflowModes.Truncate;
        createdLabel.enableAutoSizing = true;
        createdLabel.fontSizeMin = pointLabelFontSizeMin;
        createdLabel.fontSizeMax = pointLabelFontSizeMax;
        createdLabel.color = F_Utility_Config_Colours.cfgColourPointLabelText;

        UnityEditor.SceneVisibilityManager.instance.DisablePicking(labelObject, false);
        return createdLabel;
    }
#endif
}
