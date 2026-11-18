using System;
using UnityEngine;

/// <summary>Displays an integer field as a slider using a sibling integer or enum field as its upper bound.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class F_ItemCountSliderAttribute : PropertyAttribute
{
    /// <summary>The sibling serialized field that supplies the slider's maximum value.</summary>
    public string maxPropertyName { get; }

    /// <summary>Creates a slider attribute whose maximum is read from the named sibling field.</summary>
    public F_ItemCountSliderAttribute(string maxPropertyName)
    {
        this.maxPropertyName = maxPropertyName;
    }
}
