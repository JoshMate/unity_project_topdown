using UnityEditor;
using UnityEngine;

/// <summary>
/// Snaps objects to the world grid whenever they are created (prefab drag and drop, duplicate, paste) or moved in edit mode.
/// Grid values live in F_Utility_Config_Editor; toggle with Tools > Snap To Grid.
/// </summary>
[InitializeOnLoad]
public static class F_Editor_GridSnap
{
    private const string snapMenuPath = "Tools/Snap To Grid";
    private const string snapEnabledPrefsKey = "F_Editor_GridSnap_Enabled";
    private const string snapUndoName = "Snap To Grid";

    static F_Editor_GridSnap()
    {
        ObjectChangeEvents.changesPublished += OnChangesPublished;
        EditorApplication.delayCall += () => Menu.SetChecked(snapMenuPath, IsSnapEnabled());
    }

    /// <summary>Turns editor grid snapping on or off for this user.</summary>
    [MenuItem(snapMenuPath)]
    public static void ToggleSnap()
    {
        bool snapEnabled = !IsSnapEnabled();
        EditorPrefs.SetBool(snapEnabledPrefsKey, snapEnabled);
        Menu.SetChecked(snapMenuPath, snapEnabled);
    }

    /// <summary>Returns the world position snapped to the nearest grid cell on X and Y, keeping Z unchanged.</summary>
    public static Vector3 GetSnappedPosition(Vector3 position)
    {
        return new Vector3(SnapAxis(position.x), SnapAxis(position.y), position.z);
    }

    private static bool IsSnapEnabled()
    {
        return EditorPrefs.GetBool(snapEnabledPrefsKey, F_Utility_Config_Editor.cfgEditorSnapEnabledByDefault);
    }

    private static float SnapAxis(float value)
    {
        float cellSize = F_Utility_Config_Editor.cfgEditorSnapCellSize;
        float cellOffset = F_Utility_Config_Editor.cfgEditorSnapCellOffset;
        return Mathf.Round((value - cellOffset) / cellSize) * cellSize + cellOffset;
    }

    // Reacts only to new hierarchies (snapping their root, so prefab internals keep their offsets) and Transform edits
    private static void OnChangesPublished(ref ObjectChangeEventStream stream)
    {
        if (!IsSnapEnabled() || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        for (int eventIndex = 0; eventIndex < stream.length; eventIndex++)
        {
            switch (stream.GetEventType(eventIndex))
            {
                case ObjectChangeKind.CreateGameObjectHierarchy:
                    stream.GetCreateGameObjectHierarchyEvent(eventIndex, out CreateGameObjectHierarchyEventArgs createEvent);
                    if (EditorUtility.EntityIdToObject(createEvent.instanceId) is GameObject createdObject)
                    {
                        SnapTransform(createdObject.transform);
                    }
                    break;

                case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(eventIndex, out ChangeGameObjectOrComponentPropertiesEventArgs changeEvent);
                    if (EditorUtility.EntityIdToObject(changeEvent.instanceId) is Transform changedTransform)
                    {
                        SnapTransform(changedTransform);
                    }
                    break;
            }
        }
    }

    // Skips UI (RectTransform) and anything already on the grid, which also stops the snap re-triggering itself
    private static void SnapTransform(Transform targetTransform)
    {
        if (targetTransform is RectTransform)
        {
            return;
        }

        Vector3 snappedPosition = GetSnappedPosition(targetTransform.position);
        if (snappedPosition == targetTransform.position)
        {
            return;
        }

        Undo.RecordObject(targetTransform, snapUndoName);
        targetTransform.position = snappedPosition;
    }
}
