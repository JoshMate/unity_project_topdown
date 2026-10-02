using UnityEngine;

/// <summary>
/// Draws four white crosshair arms around the cursor while a firearm is equipped.
/// The arms spread apart and close together to match the weapon's current accuracy spread.
/// Arms are generated at runtime, so no manual setup is required per weapon.
/// </summary>
public class F_Logic_CursorCrosshair : MonoBehaviour
{
    [Header("Object Refs")]
    public F_PlayerHeldWeapon playerHeldWeapon;
    public F_GUI_CharacterScreen_Manager characterScreenManager;
    public SpriteRenderer sortingReferenceRenderer;

    [Header("Crosshair Look")]
    public Color crosshairColor = Color.white;
    // Length of each arm in world units
    public float armLength = 0.18f;
    // Thickness of each arm in world units
    public float armThickness = 0.04f;

    [Header("Crosshair Spread")]
    // Gap between the cursor center and the arms when the weapon has zero spread
    public float minimumGap = 0.08f;
    // Distance at which the spread cone is measured, converts spread angle to crosshair gap
    public float spreadReferenceDistance = 5f;
    // Maximum gap allowed so the crosshair never grows off screen
    public float maximumGap = 2f;
    // How quickly the arms move toward the target gap
    public float gapSmoothingSpeed = 30f;

    [Header("Reload Ring")]
    public Color reloadRingColor = Color.white;
    // Outer radius of the reload progress ring in world units
    public float reloadRingRadius = 0.3f;
    // Thickness of the reload progress ring in world units
    public float reloadRingThickness = 0.05f;

    [Header("Constants Private")]
    private const int armCount = 4;
    private const int ringSegmentCount = 64;
    private const string ringShaderName = "Sprites/Default";
    private const int sortingOrderOffset = 1;
    private const float textureSize = 1f;

    [Header("Privates")]
    private Mesh ringMesh;
    private Material ringMaterial;
    private MeshRenderer ringRenderer;
    private Transform[] armTransforms;
    private SpriteRenderer[] armRenderers;
    private Sprite armSprite;
    private float currentGap;

    private static readonly Vector2[] armDirections =
    {
        Vector2.up, Vector2.right, Vector2.down, Vector2.left
    };

    private void Start()
    {
        BuildArms();
        BuildReloadRing();
        SetArmsVisible(false);
    }

    private void OnDestroy()
    {
        if (ringMesh != null)
        {
            Destroy(ringMesh);
        }

        if (ringMaterial != null)
        {
            Destroy(ringMaterial);
        }

        if (armSprite != null)
        {
            Destroy(armSprite.texture);
            Destroy(armSprite);
        }
    }

    private void LateUpdate()
    {
        F_Item_Weapon weapon = GetEquippedFirearm();
        bool isMenuClosed = characterScreenManager == null || !characterScreenManager.isMenuOpen;
        bool isReloading = weapon != null && weapon.IsReloading && isMenuClosed;
        UpdateReloadRing(isReloading ? weapon.ReloadProgress : 0f, isReloading);

        bool shouldShow = weapon != null && isMenuClosed && !isReloading;
        SetArmsVisible(shouldShow);
        if (!shouldShow)
        {
            currentGap = minimumGap;
            return;
        }

        float targetGap = CalculateGapForSpread(weapon.CurrentAccuracySpreadRadians);
        currentGap = Mathf.Lerp(currentGap, targetGap, 1f - Mathf.Exp(-gapSmoothingSpeed * Time.deltaTime));
        PositionArms(currentGap);
    }

    private F_Item_Weapon GetEquippedFirearm()
    {
        if (playerHeldWeapon == null)
        {
            return null;
        }

        F_Item_Weapon weapon = playerHeldWeapon.SelectedWeaponItem;
        if (weapon == null)
        {
            return null;
        }

        bool isFirearm = weapon.itemType == enumItemType.WeaponGunOneHanded ||
                         weapon.itemType == enumItemType.WeaponGunTwoHanded;
        return isFirearm ? weapon : null;
    }

    private float CalculateGapForSpread(float spreadRadians)
    {
        float spreadOffset = Mathf.Tan(spreadRadians) * spreadReferenceDistance;
        return Mathf.Clamp(minimumGap + spreadOffset, minimumGap, maximumGap);
    }

    private void BuildArms()
    {
        Texture2D whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        armSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), textureSize);

        armTransforms = new Transform[armCount];
        armRenderers = new SpriteRenderer[armCount];

        for (int armIndex = 0; armIndex < armCount; armIndex++)
        {
            GameObject armObject = new GameObject("Crosshair_Arm_" + armIndex);
            armObject.transform.SetParent(transform, false);

            SpriteRenderer armRenderer = armObject.AddComponent<SpriteRenderer>();
            armRenderer.sprite = armSprite;
            armRenderer.color = crosshairColor;
            if (sortingReferenceRenderer != null)
            {
                armRenderer.sortingLayerID = sortingReferenceRenderer.sortingLayerID;
                armRenderer.sortingOrder = sortingReferenceRenderer.sortingOrder + sortingOrderOffset;
            }

            bool isVerticalArm = armDirections[armIndex].y != 0f;
            armObject.transform.localScale = isVerticalArm
                ? new Vector3(armThickness, armLength, 1f)
                : new Vector3(armLength, armThickness, 1f);

            armTransforms[armIndex] = armObject.transform;
            armRenderers[armIndex] = armRenderer;
        }

        currentGap = minimumGap;
    }

    private void PositionArms(float gap)
    {
        float armCenterOffset = gap + armLength * 0.5f;
        for (int armIndex = 0; armIndex < armCount; armIndex++)
        {
            armTransforms[armIndex].localPosition = armDirections[armIndex] * armCenterOffset;
            armRenderers[armIndex].color = crosshairColor;
        }
    }
    private void BuildReloadRing()
    {
        GameObject ringObject = new GameObject("Reload_ProgressRing");
        ringObject.transform.SetParent(transform, false);

        ringMesh = new Mesh { name = "ReloadRingMesh" };
        ringMesh.MarkDynamic();
        ringObject.AddComponent<MeshFilter>().sharedMesh = ringMesh;

        ringMaterial = new Material(Shader.Find(ringShaderName));
        ringMaterial.color = reloadRingColor;
        ringRenderer = ringObject.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = ringMaterial;
        if (sortingReferenceRenderer != null)
        {
            ringRenderer.sortingLayerID = sortingReferenceRenderer.sortingLayerID;
            ringRenderer.sortingOrder = sortingReferenceRenderer.sortingOrder + sortingOrderOffset;
        }

        ringRenderer.enabled = false;
    }

    // Shows the ring filled clockwise from the top by the given progress (0 to 1), or hides it
    private void UpdateReloadRing(float progress, bool isVisible)
    {
        if (ringRenderer == null)
        {
            return;
        }

        if (ringRenderer.enabled != isVisible)
        {
            ringRenderer.enabled = isVisible;
        }

        if (!isVisible)
        {
            return;
        }

        float clampedProgress = Mathf.Clamp01(progress);
        int stepCount = Mathf.Max(1, Mathf.CeilToInt(ringSegmentCount * clampedProgress));
        float totalAngle = clampedProgress * Mathf.PI * 2f;
        float outerRadius = reloadRingRadius;
        float innerRadius = Mathf.Max(0f, reloadRingRadius - reloadRingThickness);

        Vector3[] vertices = new Vector3[(stepCount + 1) * 2];
        int[] triangles = new int[stepCount * 6];
        for (int stepIndex = 0; stepIndex <= stepCount; stepIndex++)
        {
            float angle = totalAngle * stepIndex / stepCount;
            // Start at 12 o'clock and sweep clockwise
            Vector3 direction = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
            vertices[stepIndex * 2] = direction * outerRadius;
            vertices[stepIndex * 2 + 1] = direction * innerRadius;
        }

        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            int vertexIndex = stepIndex * 2;
            int triangleIndex = stepIndex * 6;
            // Clockwise winding when viewed from the camera (-Z)
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        ringMesh.Clear();
        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.RecalculateBounds();
        ringMaterial.color = reloadRingColor;
    }



    private void SetArmsVisible(bool isVisible)
    {
        if (armRenderers == null)
        {
            return;
        }

        for (int armIndex = 0; armIndex < armRenderers.Length; armIndex++)
        {
            if (armRenderers[armIndex].enabled != isVisible)
            {
                armRenderers[armIndex].enabled = isVisible;
            }
        }
    }
}
