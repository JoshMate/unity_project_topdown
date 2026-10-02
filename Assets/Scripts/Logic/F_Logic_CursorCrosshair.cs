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

    [Header("Constants Private")]
    private const int armCount = 4;
    private const int sortingOrderOffset = 1;
    private const float textureSize = 1f;

    [Header("Privates")]
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
        SetArmsVisible(false);
    }

    private void OnDestroy()
    {
        if (armSprite != null)
        {
            Destroy(armSprite.texture);
            Destroy(armSprite);
        }
    }

    private void LateUpdate()
    {
        F_Item_Weapon weapon = GetEquippedFirearm();
        bool shouldShow = weapon != null &&
                          (characterScreenManager == null || !characterScreenManager.isMenuOpen);
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
