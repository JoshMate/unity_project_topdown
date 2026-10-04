using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bottom-right HUD panel showing the selected weapon sprite, loaded / reserve ammo, the ammo type sprite and the weapon name.</summary>
/// <remarks>The panel builds its own hierarchy at runtime using the context menu's visual style, so no manual
/// wiring is required. Everything is driven by the selected weapon's configuration (ammo type, capacity, sprite, name).</remarks>
public class F_GUI_HUD_WeaponAmmo : MonoBehaviour
{
    [Header("Constants Private")]
    private const float borderThickness = 3f;
    private const float contentPadding = 12f;
    private const float screenEdgePadding = 24f;
    private const float iconSize = 100f;
    private const float elementSpacing = 12f;
    private const float nameRowHeight = 24f;
    private const float panelWidth = 480f;
    private const float panelHeight = (borderThickness + contentPadding) * 2f + iconSize + elementSpacing + nameRowHeight;
    private const float ammoFontSize = 42f;
    private const float nameFontSize = 21f;
    private const float ammoFontSizeMin = 22f;
    private const string noWeaponMessage = "No Weapon Equiped";
    private const string ammoSeparator = " / ";

    private static readonly Color panelBorderColor = F_Utility_Config_Colours.cfgColourGuiPanelBorder;
    private static readonly Color panelFillColor = F_Utility_Config_Colours.cfgColourGuiPanelFill;
    private static readonly Color textColor = F_Utility_Config_Colours.cfgColourGuiText;
    private static readonly Color emptyAmmoTextColor = F_Utility_Config_Colours.cfgColourHudAmmoEmptyText;

    [Header("Object Refs")]
    public F_PlayerHeldWeapon playerHeldWeapon;
    public F_PlayerInventory playerInventory;

    [Header("Privates")]
    private RectTransform panelRect;
    private GameObject weaponContentObj;
    private Image weaponIconImage;
    private Image ammoIconImage;
    private TMP_Text ammoCountText;
    private TMP_Text weaponNameText;
    private bool isBuilt;

    private void Awake()
    {
        BuildPanel();
    }

    private void Update()
    {
        ResolveReferences();
        RefreshDisplay();
    }

    private void ResolveReferences()
    {
        if (playerHeldWeapon == null)
        {
            playerHeldWeapon = FindFirstObjectByType<F_PlayerHeldWeapon>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<F_PlayerInventory>();
        }
    }

    private void RefreshDisplay()
    {
        F_Item_Weapon weapon = playerHeldWeapon != null ? playerHeldWeapon.SelectedWeaponItem : null;
        bool hasWeapon = weapon != null;

        weaponIconImage.gameObject.SetActive(hasWeapon);
        ammoIconImage.gameObject.SetActive(hasWeapon);
        ammoCountText.gameObject.SetActive(hasWeapon);
        weaponNameText.text = hasWeapon ? weapon.itemName : noWeaponMessage;

        if (!hasWeapon)
        {
            return;
        }

        weaponIconImage.sprite = weapon.itemSprite;
        weaponIconImage.enabled = weapon.itemSprite != null;

        bool showAmmo = weapon.weaponUsesAmmo;
        ammoCountText.gameObject.SetActive(showAmmo);
        ammoIconImage.gameObject.SetActive(showAmmo);
        if (!showAmmo)
        {
            return;
        }

        int reserveAmmo = weapon.weaponAmmoType != null && playerInventory != null
            ? F_Utility_Helper_Inventory.GetItemCountInInventory(playerInventory, weapon.weaponAmmoType)
            : 0;
        ammoCountText.text = weapon.CurrentAmmoLoaded + ammoSeparator + reserveAmmo;
        ammoCountText.color = weapon.CurrentAmmoLoaded <= 0 ? emptyAmmoTextColor : textColor;

        Sprite ammoSprite = weapon.weaponAmmoType != null ? weapon.weaponAmmoType.itemSprite : null;
        ammoIconImage.sprite = ammoSprite;
        ammoIconImage.enabled = ammoSprite != null;
    }

    private void BuildPanel()
    {
        if (isBuilt)
        {
            return;
        }

        isBuilt = true;

        panelRect = GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-screenEdgePadding, screenEdgePadding);

        Image borderImage = GetComponent<Image>();
        if (borderImage == null)
        {
            borderImage = gameObject.AddComponent<Image>();
        }

        borderImage.color = panelBorderColor;
        borderImage.raycastTarget = false;

        RectTransform fillRect = CreateUiObject("WeaponAmmo_Fill", transform, typeof(Image));
        StretchToParent(fillRect, borderThickness);
        Image fillImage = fillRect.GetComponent<Image>();
        fillImage.color = panelFillColor;
        fillImage.raycastTarget = false;

        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        RectTransform contentRect = CreateUiObject("WeaponAmmo_Content", fillRect);
        StretchToParent(contentRect, contentPadding);
        weaponContentObj = contentRect.gameObject;

        RectTransform weaponIconRect = CreateUiObject("WeaponAmmo_WeaponIcon", contentRect, typeof(Image));
        weaponIconRect.anchorMin = new Vector2(0f, 1f);
        weaponIconRect.anchorMax = new Vector2(0f, 1f);
        weaponIconRect.pivot = new Vector2(0f, 1f);
        weaponIconRect.anchoredPosition = Vector2.zero;
        weaponIconRect.sizeDelta = new Vector2(iconSize, iconSize);
        weaponIconImage = weaponIconRect.GetComponent<Image>();
        weaponIconImage.preserveAspect = true;
        weaponIconImage.raycastTarget = false;

        RectTransform ammoIconRect = CreateUiObject("WeaponAmmo_AmmoIcon", contentRect, typeof(Image));
        ammoIconRect.anchorMin = new Vector2(1f, 1f);
        ammoIconRect.anchorMax = new Vector2(1f, 1f);
        ammoIconRect.pivot = new Vector2(1f, 1f);
        ammoIconRect.anchoredPosition = Vector2.zero;
        ammoIconRect.sizeDelta = new Vector2(iconSize, iconSize);
        ammoIconImage = ammoIconRect.GetComponent<Image>();
        ammoIconImage.preserveAspect = true;
        ammoIconImage.raycastTarget = false;

        RectTransform ammoTextRect = CreateUiObject("WeaponAmmo_CountLabel", contentRect, typeof(TextMeshProUGUI));
        ammoTextRect.anchorMin = new Vector2(0f, 1f);
        ammoTextRect.anchorMax = new Vector2(1f, 1f);
        ammoTextRect.pivot = new Vector2(0.5f, 1f);
        ammoTextRect.offsetMin = new Vector2(iconSize + elementSpacing, -iconSize);
        ammoTextRect.offsetMax = new Vector2(-(iconSize + elementSpacing), 0f);
        ammoCountText = ConfigureLabel(ammoTextRect.GetComponent<TMP_Text>(), ammoFontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        ammoCountText.enableAutoSizing = true;
        ammoCountText.fontSizeMax = ammoFontSize;
        ammoCountText.fontSizeMin = ammoFontSizeMin;

        RectTransform nameRect = CreateUiObject("WeaponAmmo_NameLabel", contentRect, typeof(TextMeshProUGUI));
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = new Vector2(0f, nameRowHeight);
        weaponNameText = ConfigureLabel(nameRect.GetComponent<TMP_Text>(), nameFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        weaponNameText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent, params System.Type[] extraComponents)
    {
        GameObject uiObj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        for (int componentIndex = 0; componentIndex < extraComponents.Length; componentIndex++)
        {
            uiObj.AddComponent(extraComponents[componentIndex]);
        }

        uiObj.transform.SetParent(parent, false);
        return uiObj.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static TMP_Text ConfigureLabel(TMP_Text label, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        label.color = textColor;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }
}
