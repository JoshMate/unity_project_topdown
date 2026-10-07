using System.Collections.Generic;
using UnityEngine;

public static class F_Utility_Helper_Shatter
{
    [Header("Constants Private")]
    private const int cfgShatterMinPolygonVertices = 3;
    private const float cfgShatterMinPolygonArea = 0.00001f;
    private const string cfgShatterPieceNamePrefix = "Shatter_Piece_";

    /// <summary>Cuts a sprite renderer's sprite into random Voronoi pieces and spawns them as scattering effect objects.</summary>
    /// <param name="spriteRenderer">The renderer whose current sprite is shattered. It is left untouched.</param>
    public static void ShatterSprite(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || spriteRenderer.sprite.texture == null)
        {
            return;
        }

        Sprite sprite = spriteRenderer.sprite;
        Texture2D texture = sprite.texture;
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Rect textureRect = sprite.textureRect;
        Vector2 textureOffset = sprite.textureRectOffset;
        Vector2 pivot = sprite.pivot;

        // Local-space area actually covered by sprite pixels, so pieces never sample neighbouring atlas content.
        Vector2 areaMin = (textureOffset - pivot) / pixelsPerUnit;
        Vector2 areaMax = (textureOffset + textureRect.size - pivot) / pixelsPerUnit;

        int pieceCount = Random.Range(F_Utility_Config_Shatter.cfgShatterPieceCountMin, F_Utility_Config_Shatter.cfgShatterPieceCountMax + 1);
        List<Vector2> sites = new List<Vector2>(pieceCount);
        for (int siteIndex = 0; siteIndex < pieceCount; siteIndex++)
        {
            sites.Add(new Vector2(Random.Range(areaMin.x, areaMax.x), Random.Range(areaMin.y, areaMax.y)));
        }

        Vector2 areaBottomLeft = areaMin;
        Vector2 areaTopRight = areaMax;
        Vector2 flip = new Vector2(spriteRenderer.flipX ? -1f : 1f, spriteRenderer.flipY ? -1f : 1f);
        Transform rendererTransform = spriteRenderer.transform;
        Vector3 explosionOrigin = rendererTransform.position;
        for (int siteIndex = 0; siteIndex < sites.Count; siteIndex++)
        {
            List<Vector2> polygon = new List<Vector2>
            {
                areaBottomLeft,
                new Vector2(areaTopRight.x, areaBottomLeft.y),
                areaTopRight,
                new Vector2(areaBottomLeft.x, areaTopRight.y)
            };

            for (int otherIndex = 0; otherIndex < sites.Count && polygon.Count >= cfgShatterMinPolygonVertices; otherIndex++)
            {
                if (otherIndex != siteIndex)
                {
                    polygon = ClipToCloserHalfPlane(polygon, sites[siteIndex], sites[otherIndex]);
                }
            }

            if (polygon.Count < cfgShatterMinPolygonVertices || GetPolygonArea(polygon) < cfgShatterMinPolygonArea)
            {
                continue;
            }

            SpawnPiece(spriteRenderer, texture, polygon, flip, pixelsPerUnit, pivot, textureOffset, textureRect, explosionOrigin);
        }
    }

    // Keeps the part of the polygon closer to siteA than siteB (Sutherland-Hodgman against the bisector line).
    private static List<Vector2> ClipToCloserHalfPlane(List<Vector2> polygon, Vector2 siteA, Vector2 siteB)
    {
        Vector2 direction = siteB - siteA;
        Vector2 midpoint = (siteA + siteB) * 0.5f;
        List<Vector2> clipped = new List<Vector2>(polygon.Count + 1);

        for (int vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
        {
            Vector2 current = polygon[vertexIndex];
            Vector2 next = polygon[(vertexIndex + 1) % polygon.Count];
            float currentSide = Vector2.Dot(current - midpoint, direction);
            float nextSide = Vector2.Dot(next - midpoint, direction);

            if (currentSide <= 0f)
            {
                clipped.Add(current);
            }

            if ((currentSide < 0f && nextSide > 0f) || (currentSide > 0f && nextSide < 0f))
            {
                float t = currentSide / (currentSide - nextSide);
                clipped.Add(Vector2.Lerp(current, next, t));
            }
        }

        return clipped;
    }

    private static float GetPolygonArea(List<Vector2> polygon)
    {
        float doubleArea = 0f;
        for (int vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
        {
            Vector2 current = polygon[vertexIndex];
            Vector2 next = polygon[(vertexIndex + 1) % polygon.Count];
            doubleArea += current.x * next.y - next.x * current.y;
        }

        return Mathf.Abs(doubleArea) * 0.5f;
    }

    private static void SpawnPiece(
        SpriteRenderer spriteRenderer,
        Texture2D texture,
        List<Vector2> polygon,
        Vector2 flip,
        float pixelsPerUnit,
        Vector2 pivot,
        Vector2 textureOffset,
        Rect textureRect,
        Vector3 explosionOrigin)
    {
        Vector2 centroid = Vector2.zero;
        foreach (Vector2 point in polygon)
        {
            centroid += point;
        }
        centroid /= polygon.Count;

        // Vertex 0 is the centroid, followed by the ring; the fan keeps convex Voronoi cells intact.
        Vector3[] vertices = new Vector3[polygon.Count + 1];
        Vector2[] uvs = new Vector2[polygon.Count + 1];
        int[] triangles = new int[polygon.Count * 3];
        Vector2 textureSize = new Vector2(texture.width, texture.height);

        vertices[0] = Vector3.zero;
        uvs[0] = GetUv(centroid, pixelsPerUnit, pivot, textureOffset, textureRect, textureSize);
        for (int pointIndex = 0; pointIndex < polygon.Count; pointIndex++)
        {
            Vector2 relative = polygon[pointIndex] - centroid;
            vertices[pointIndex + 1] = new Vector3(relative.x * flip.x, relative.y * flip.y, 0f);
            uvs[pointIndex + 1] = GetUv(polygon[pointIndex], pixelsPerUnit, pivot, textureOffset, textureRect, textureSize);

            triangles[pointIndex * 3] = 0;
            triangles[pointIndex * 3 + 1] = pointIndex + 1;
            triangles[pointIndex * 3 + 2] = (pointIndex + 1) % polygon.Count + 1;
        }

        Mesh mesh = new Mesh { vertices = vertices, uv = uvs, triangles = triangles };
        mesh.RecalculateBounds();

        Material material = new Material(spriteRenderer.sharedMaterial) { mainTexture = texture, color = spriteRenderer.color };

        Transform rendererTransform = spriteRenderer.transform;
        GameObject pieceObject = new GameObject(cfgShatterPieceNamePrefix + spriteRenderer.name);
        pieceObject.transform.SetPositionAndRotation(
            rendererTransform.TransformPoint(new Vector3(centroid.x * flip.x, centroid.y * flip.y, 0f)),
            rendererTransform.rotation);
        pieceObject.transform.localScale = rendererTransform.lossyScale;

        pieceObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = pieceObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        meshRenderer.sortingOrder = spriteRenderer.sortingOrder;

        Vector2 outward = (Vector2)(pieceObject.transform.position - explosionOrigin);
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Random.insideUnitCircle.normalized;
        Vector2 velocity = outward * Random.Range(F_Utility_Config_Shatter.cfgShatterScatterSpeedMin, F_Utility_Config_Shatter.cfgShatterScatterSpeedMax);
        float spin = Random.Range(-F_Utility_Config_Shatter.cfgShatterSpinSpeedMax, F_Utility_Config_Shatter.cfgShatterSpinSpeedMax);
        float lifetime = Random.Range(F_Utility_Config_Shatter.cfgShatterLifetimeMin, F_Utility_Config_Shatter.cfgShatterLifetimeMax);

        pieceObject.AddComponent<F_Effects_ShatterPiece>().Initialise(velocity, spin, lifetime, material, mesh);
    }

    // Converts a sprite-local point into a normalised texture coordinate.
    private static Vector2 GetUv(Vector2 localPoint, float pixelsPerUnit, Vector2 pivot, Vector2 textureOffset, Rect textureRect, Vector2 textureSize)
    {
        Vector2 texturePixel = localPoint * pixelsPerUnit + pivot - textureOffset + textureRect.position;
        return new Vector2(texturePixel.x / textureSize.x, texturePixel.y / textureSize.y);
    }
}
