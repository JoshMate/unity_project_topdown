using UnityEngine;

/// <summary>Shared helpers for building runtime GUI elements.</summary>
public static class F_Utility_Helper_Gui
{
    /// <summary>Anchors a rect to the area a sprite occupies within its source texture, so the sprite's surrounding empty space is preserved when the parent is scaled.</summary>
    /// <param name="rect">The rect to anchor; it fills the parent, which represents the full source texture.</param>
    /// <param name="sprite">The sprite whose texture rect defines the occupied area. When null, the rect stretches to the full parent.</param>
    public static void AnchorRectToSpriteArea(RectTransform rect, Sprite sprite)
    {
        Vector2 anchorMin = Vector2.zero;
        Vector2 anchorMax = Vector2.one;

        if (sprite != null && sprite.texture != null && sprite.texture.width > 0 && sprite.texture.height > 0)
        {
            Rect spriteRect = sprite.rect;
            float textureWidth = sprite.texture.width;
            float textureHeight = sprite.texture.height;
            anchorMin = new Vector2(spriteRect.xMin / textureWidth, spriteRect.yMin / textureHeight);
            anchorMax = new Vector2(spriteRect.xMax / textureWidth, spriteRect.yMax / textureHeight);
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
