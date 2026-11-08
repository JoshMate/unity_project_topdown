using UnityEngine;

/// <summary>
/// Toggles the hover outline on objects whose sprite renderers use the "TopDown/Sprite Outline" shader.
/// The outline is set per renderer through a MaterialPropertyBlock, so the shared material is never modified.
/// </summary>
public static class F_Utility_Helper_Outline
{
    [Header("Constants Private")]
    private const string outlineEnabledProperty = "_OutlineEnabled";
    private const string outlineColourProperty = "_OutlineColor";
    private const float outlineOn = 1f;
    private const float outlineOff = 0f;

    [Header("Privates")]
    private static readonly int outlineEnabledId = Shader.PropertyToID(outlineEnabledProperty);
    private static readonly int outlineColourId = Shader.PropertyToID(outlineColourProperty);
    private static MaterialPropertyBlock outlinePropertyBlock;

    /// <summary>Shows or hides the outline on every sprite renderer under the root (including the root).</summary>
    /// <param name="root">The object to outline; ignored when null or destroyed.</param>
    /// <param name="isVisible">True to show the outline, false to hide it.</param>
    public static void SetOutlineVisible(GameObject root, bool isVisible)
    {
        if (root == null)
        {
            return;
        }

        outlinePropertyBlock ??= new MaterialPropertyBlock();
        foreach (SpriteRenderer spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.GetPropertyBlock(outlinePropertyBlock);
            outlinePropertyBlock.SetFloat(outlineEnabledId, isVisible ? outlineOn : outlineOff);
            outlinePropertyBlock.SetColor(outlineColourId, F_Utility_Config_Colours.cfgColourInteractOutline);
            spriteRenderer.SetPropertyBlock(outlinePropertyBlock);
        }
    }
}
