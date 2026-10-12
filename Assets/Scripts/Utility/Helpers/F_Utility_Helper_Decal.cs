using UnityEngine;

public static class F_Utility_Helper_Decal
{
    [Header("Privates")]
    private static Sprite decalSprite;

    /// <summary>Spawns a tinted decal on the floor decal layer.</summary>
    /// <param name="worldPosition">World position the decal is centred on.</param>
    /// <param name="colour">Tint colour of the decal.</param>
    /// <param name="diameter">Decal diameter in world units.</param>
    public static void CreateFloorDecal(Vector3 worldPosition, Color colour, float diameter)
    {
        GameObject decalObject = new GameObject(F_Utility_Config_Decal.cfgDecalObjectName);
        decalObject.transform.position = worldPosition;
        decalObject.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        SpriteRenderer decalRenderer = decalObject.AddComponent<SpriteRenderer>();
        decalRenderer.sprite = GetDecalSprite();
        decalRenderer.color = colour;
        decalRenderer.sortingLayerID = SortingLayer.NameToID(F_Logic_Globals.layerFloorDecal);
        decalRenderer.sortingOrder = F_Utility_Config_Decal.cfgDecalSortingOrder;

        float spriteDiameter = decalRenderer.sprite.bounds.size.x;
        decalObject.transform.localScale = Vector3.one * (diameter / spriteDiameter);
    }

    /// <summary>Gets the combined world-space bounds of every sprite renderer under an object.</summary>
    /// <param name="target">The object whose renderers are measured.</param>
    /// <returns>The combined bounds, or a zero-size bounds at the object position when it has no sprites.</returns>
    public static Bounds GetSpriteBounds(GameObject target)
    {
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>())
        {
            if (spriteRenderer.sprite == null)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(spriteRenderer.bounds);
            }
            else
            {
                bounds = spriteRenderer.bounds;
                hasBounds = true;
            }
        }

        return bounds;
    }

    // Lazily builds a soft, slightly irregular white blob that is tinted per decal.
    private static Sprite GetDecalSprite()
    {
        if (decalSprite != null)
        {
            return decalSprite;
        }

        int size = F_Utility_Config_Decal.cfgDecalTextureSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float halfSize = size * 0.5f;
        float noisePhase = Random.Range(0f, 100f);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x + 0.5f - halfSize, y + 0.5f - halfSize);
                float angle = Mathf.Atan2(offset.y, offset.x);
                float edgeScale = 1f - F_Utility_Config_Decal.cfgDecalEdgeNoise * Mathf.PerlinNoise(
                    noisePhase + Mathf.Cos(angle) * F_Utility_Config_Decal.cfgDecalEdgeNoiseFrequency,
                    noisePhase + Mathf.Sin(angle) * F_Utility_Config_Decal.cfgDecalEdgeNoiseFrequency);
                float normalisedDistance = offset.magnitude / (halfSize * edgeScale);
                float fade = Mathf.InverseLerp(1f, F_Utility_Config_Decal.cfgDecalSolidRadiusFraction, normalisedDistance);
                byte alpha = (byte)(Mathf.Clamp01(fade) * F_Utility_Config_Decal.cfgDecalAlpha * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        decalSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), F_Utility_Config_Decal.cfgDecalPixelsPerUnit);
        return decalSprite;
    }
}
