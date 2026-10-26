public static class F_Utility_Config_Editor
{
    // Constants Public: Editor grid snapping (edit mode only, never affects the built game)
    // Size of one grid cell in pixels; matches the 64x64 world tiles
    public const float cfgEditorSnapCellPixels = 64f;
    // Pixels per unit used by the world sprites
    public const float cfgEditorSnapPixelsPerUnit = 64f;
    // Size of one grid cell in world units
    public const float cfgEditorSnapCellSize = cfgEditorSnapCellPixels / cfgEditorSnapPixelsPerUnit;
    // Offset applied after snapping; 0 places centre-pivoted sprites exactly on a cell, 0.5 would snap to cell corners instead
    public const float cfgEditorSnapCellOffset = 0f;
    // Whether snapping is on when the project is first opened; toggled per user via Tools > Snap To Grid
    public const bool cfgEditorSnapEnabledByDefault = true;
}
