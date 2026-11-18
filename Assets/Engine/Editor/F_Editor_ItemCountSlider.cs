using UnityEditor;
using UnityEngine;

/// <summary>Draws item counts with an inspector slider bounded by the item's configured maximum stack size.</summary>
[CustomPropertyDrawer(typeof(F_ItemCountSliderAttribute))]
public sealed class F_Editor_ItemCountSlider : PropertyDrawer
{
    [Header("Constants Private")]
    private const int itemCountMinimum = 1;

    /// <summary>Draws the integer property as a slider from one to its configured maximum.</summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        F_ItemCountSliderAttribute itemCountSliderAttribute = (F_ItemCountSliderAttribute)attribute;
        SerializedProperty maximumProperty = property.serializedObject.FindProperty(itemCountSliderAttribute.maxPropertyName);
        int itemCountMaximum = maximumProperty != null ? Mathf.Max(itemCountMinimum, maximumProperty.intValue) : itemCountMinimum;
        int currentItemCount = Mathf.Clamp(property.intValue, itemCountMinimum, itemCountMaximum);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int selectedItemCount = EditorGUI.IntSlider(position, label, currentItemCount, itemCountMinimum, itemCountMaximum);
        if (EditorGUI.EndChangeCheck() || selectedItemCount != property.intValue)
        {
            property.intValue = selectedItemCount;
        }
        EditorGUI.EndProperty();
    }
}
