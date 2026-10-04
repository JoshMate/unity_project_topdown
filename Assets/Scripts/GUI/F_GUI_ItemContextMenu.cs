using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays a dynamic right-click context menu of actions available for an inventory item.</summary>
/// <remarks>The option list comes from <see cref="F_Item.GetContextMenuActions"/> so new item types can
/// expose additional actions without any change to this component; clicking an option routes through
/// <see cref="F_Utility_Helper_ContextMenu"/> so the executed behavior stays centralized and reusable.</remarks>
public class F_GUI_ItemContextMenu : MonoBehaviour
{
    [Header("Constants Private")]
    private const float menuWidth = 320f;
    private const float borderThickness = 2f;
    private const float contentPadding = 8f;
    private const float headerHeight = 22f;
    private const float headerFontSize = 15f;
    private const float headerSeparatorSpacing = 6f;
    private const float separatorThickness = 2f;
    private const float optionHeight = 28f;
    private const float optionSpacing = 2f;
    private const float optionFontSize = 15f;
    // Only needs to clear the cursor's visible pointer glyph (~24px at the reference resolution), not the
    // sprite's full transparent bounds, so the menu can stay close to the cursor for easy clicking.
    private const float cursorGap = 26f;
    private const float screenEdgePadding = 12f;
    private const float autoCloseDistance = 400f;
    // Space occupied by the item name header and its divider before the option list begins.
    private const float headerBlockHeight = contentPadding + headerHeight + headerSeparatorSpacing + separatorThickness + headerSeparatorSpacing;

    private static readonly Color panelBorderColor = F_Utility_Config_Colours.cfgColourGuiPanelBorder;
    private static readonly Color panelFillColor = F_Utility_Config_Colours.cfgColourGuiPanelFill;
    private static readonly Color headerTextColor = F_Utility_Config_Colours.cfgColourGuiText;
    private static readonly Color headerSeparatorColor = F_Utility_Config_Colours.cfgColourGuiSeparator;
    private static readonly Color optionBackgroundColor = F_Utility_Config_Colours.cfgColourGuiOptionBackground;
    private static readonly Color optionHighlightedColor = new Color(F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier, F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier, F_Utility_Config_Colours.cfgColourGuiOptionHighlightMultiplier, 1f);
    private static readonly Color optionPressedColor = new Color(F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier, F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier, F_Utility_Config_Colours.cfgColourGuiOptionPressedMultiplier, 1f);
    private static readonly Color optionTextColor = F_Utility_Config_Colours.cfgColourGuiText;

    [Header("Context Menu References")]
    public Canvas contextMenuCanvas;
    public RectTransform contextMenuRect;
    public CanvasGroup contextMenuCanvasGroup;
    public Image panelBorderImage;

    [Header("Context Action Sounds")]
    public AudioClip unloadActionSound;

    [Header("Privates")]
    private RectTransform fillRect;
    private TMP_Text headerLabel;
    private RectTransform optionsContainerRect;
    private GameObject backdropObj;
    private F_Item displayedItem;
    private F_PlayerInventory ownerInventory;
    private Vector2 openPointerScreenPosition;
    private readonly List<GameObject> spawnedOptionObjs = new List<GameObject>();

    /// <summary>True while the context menu is currently visible.</summary>
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (contextMenuRect == null)
        {
            contextMenuRect = GetComponent<RectTransform>();
        }

        contextMenuRect.anchorMin = new Vector2(0f, 1f);
        contextMenuRect.anchorMax = new Vector2(0f, 1f);
        contextMenuRect.pivot = new Vector2(0f, 1f);

        if (panelBorderImage == null)
        {
            panelBorderImage = GetComponent<Image>();
        }

        if (panelBorderImage == null)
        {
            panelBorderImage = gameObject.AddComponent<Image>();
        }

        panelBorderImage.color = panelBorderColor;
        panelBorderImage.raycastTarget = false;

        if (contextMenuCanvasGroup == null)
        {
            contextMenuCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (contextMenuCanvasGroup == null)
        {
            contextMenuCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (contextMenuCanvas == null)
        {
            contextMenuCanvas = GetComponentInParent<Canvas>();
        }

        EnsureFillAndContainer();
        EnsureBackdrop();
        Hide();
    }

    /// <summary>Builds and shows the context menu for an item's available actions, positioned beside the pointer.</summary>
    /// <param name="item">The item whose context menu actions should be listed.</param>
    /// <param name="pointerScreenPosition">The screen position that triggered the menu (e.g. a right-click).</param>
    /// <param name="playerInventory">The inventory that currently owns the item.</param>
    public void Show(F_Item item, Vector2 pointerScreenPosition, F_PlayerInventory playerInventory)
    {
        if (item == null || contextMenuCanvas == null || contextMenuRect == null || contextMenuCanvasGroup == null)
        {
            Hide();
            return;
        }

        displayedItem = item;
        ownerInventory = playerInventory;
        openPointerScreenPosition = pointerScreenPosition;
        UpdateHeader(item);
        BuildOptions(item.GetContextMenuActions());

        contextMenuCanvasGroup.alpha = 1f;
        contextMenuCanvasGroup.interactable = true;
        contextMenuCanvasGroup.blocksRaycasts = true;
        IsOpen = true;

        if (backdropObj != null)
        {
            backdropObj.SetActive(true);
        }

        PositionBesidePointer(pointerScreenPosition);
    }

    /// <summary>Hides the context menu and clears its dynamically built options.</summary>
    public void Hide()
    {
        displayedItem = null;
        ownerInventory = null;
        IsOpen = false;
        ClearOptions();

        if (contextMenuCanvasGroup != null)
        {
            contextMenuCanvasGroup.alpha = 0f;
            contextMenuCanvasGroup.interactable = false;
            contextMenuCanvasGroup.blocksRaycasts = false;
        }

        if (backdropObj != null)
        {
            backdropObj.SetActive(false);
        }
    }

    /// <summary>Closes the menu once the pointer strays far enough from where it was opened.</summary>
    /// <param name="currentPointerScreenPosition">The pointer's current screen position, in pixels.</param>
    public void CloseIfPointerMovedAway(Vector2 currentPointerScreenPosition)
    {
        if (!IsOpen)
        {
            return;
        }

        if (Vector2.Distance(currentPointerScreenPosition, openPointerScreenPosition) > autoCloseDistance)
        {
            Hide();
        }
    }

    private void EnsureFillAndContainer()
    {
        Transform existingFill = transform.Find("ContextMenu_Fill");
        if (existingFill != null)
        {
            fillRect = existingFill.GetComponent<RectTransform>();
            Transform existingHeader = existingFill.Find("ContextMenu_Header");
            headerLabel = existingHeader != null ? existingHeader.GetComponent<TMP_Text>() : null;
            Transform existingContainer = existingFill.Find("ContextMenu_OptionsContainer");
            optionsContainerRect = existingContainer != null ? existingContainer.GetComponent<RectTransform>() : null;
            return;
        }

        GameObject fillObj = new GameObject(
            "ContextMenu_Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObj.transform.SetParent(transform, false);

        fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(borderThickness, borderThickness);
        fillRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = panelFillColor;
        fillImage.raycastTarget = false;

        GameObject headerObj = new GameObject(
            "ContextMenu_Header",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(fillObj.transform, false);

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(contentPadding, headerRect.offsetMin.y);
        headerRect.offsetMax = new Vector2(-contentPadding, headerRect.offsetMax.y);
        headerRect.anchoredPosition = new Vector2(0f, -contentPadding);
        headerRect.sizeDelta = new Vector2(headerRect.sizeDelta.x, headerHeight);

        headerLabel = headerObj.GetComponent<TMP_Text>();
        headerLabel.color = headerTextColor;
        headerLabel.fontSize = headerFontSize;
        headerLabel.fontStyle = FontStyles.Bold;
        headerLabel.alignment = TextAlignmentOptions.MidlineLeft;
        headerLabel.raycastTarget = false;
        headerLabel.textWrappingMode = TextWrappingModes.NoWrap;
        headerLabel.overflowMode = TextOverflowModes.Ellipsis;

        GameObject separatorObj = new GameObject(
            "ContextMenu_HeaderSeparator",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        separatorObj.transform.SetParent(fillObj.transform, false);

        RectTransform separatorRect = separatorObj.GetComponent<RectTransform>();
        separatorRect.anchorMin = new Vector2(0f, 1f);
        separatorRect.anchorMax = new Vector2(1f, 1f);
        separatorRect.pivot = new Vector2(0.5f, 1f);
        separatorRect.offsetMin = new Vector2(contentPadding, separatorRect.offsetMin.y);
        separatorRect.offsetMax = new Vector2(-contentPadding, separatorRect.offsetMax.y);
        separatorRect.anchoredPosition = new Vector2(0f, -(contentPadding + headerHeight + headerSeparatorSpacing));
        separatorRect.sizeDelta = new Vector2(separatorRect.sizeDelta.x, separatorThickness);

        Image separatorImage = separatorObj.GetComponent<Image>();
        separatorImage.color = headerSeparatorColor;
        separatorImage.raycastTarget = false;

        GameObject containerObj = new GameObject("ContextMenu_OptionsContainer", typeof(RectTransform));
        containerObj.transform.SetParent(fillObj.transform, false);

        optionsContainerRect = containerObj.GetComponent<RectTransform>();
        optionsContainerRect.anchorMin = new Vector2(0f, 1f);
        optionsContainerRect.anchorMax = new Vector2(1f, 1f);
        optionsContainerRect.pivot = new Vector2(0.5f, 1f);
        optionsContainerRect.offsetMin = new Vector2(contentPadding, optionsContainerRect.offsetMin.y);
        optionsContainerRect.offsetMax = new Vector2(-contentPadding, optionsContainerRect.offsetMax.y);
        optionsContainerRect.anchoredPosition = new Vector2(0f, -headerBlockHeight);
    }

    private void EnsureBackdrop()
    {
        if (contextMenuRect.parent == null)
        {
            return;
        }

        Transform existingBackdrop = contextMenuRect.parent.Find("ContextMenu_Backdrop");
        if (existingBackdrop != null)
        {
            backdropObj = existingBackdrop.gameObject;
            return;
        }

        backdropObj = new GameObject(
            "ContextMenu_Backdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        backdropObj.transform.SetParent(contextMenuRect.parent, false);
        backdropObj.transform.SetAsFirstSibling();

        RectTransform backdropRect = backdropObj.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropImage = backdropObj.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0f);

        Button backdropButton = backdropObj.GetComponent<Button>();
        backdropButton.targetGraphic = backdropImage;
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);
    }

    /// <summary>Sets the header text to the item's name, appending its stack count in brackets when above one.</summary>
    private void UpdateHeader(F_Item item)
    {
        if (headerLabel == null || item == null)
        {
            return;
        }

        headerLabel.text = item.itemCount > 1
            ? $"{item.itemName} ({item.itemCount})"
            : item.itemName;
    }

    private void ClearOptions()
    {
        for (int optionIndex = 0; optionIndex < spawnedOptionObjs.Count; optionIndex++)
        {
            if (spawnedOptionObjs[optionIndex] != null)
            {
                Destroy(spawnedOptionObjs[optionIndex]);
            }
        }

        spawnedOptionObjs.Clear();
    }

    private void BuildOptions(List<enumItemContextAction> actions)
    {
        ClearOptions();

        if (actions == null || optionsContainerRect == null)
        {
            return;
        }

        for (int optionIndex = 0; optionIndex < actions.Count; optionIndex++)
        {
            CreateOptionButton(actions[optionIndex], optionIndex);
        }

        float contentHeight = headerBlockHeight +
            actions.Count * optionHeight +
            Mathf.Max(0, actions.Count - 1) * optionSpacing +
            contentPadding;
        contextMenuRect.sizeDelta = new Vector2(menuWidth, contentHeight);
    }

    private void CreateOptionButton(enumItemContextAction action, int optionIndex)
    {
        GameObject optionObj = new GameObject(
            $"ContextMenu_Option_{action}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        optionObj.transform.SetParent(optionsContainerRect, false);

        RectTransform optionRect = optionObj.GetComponent<RectTransform>();
        optionRect.anchorMin = new Vector2(0f, 1f);
        optionRect.anchorMax = new Vector2(1f, 1f);
        optionRect.pivot = new Vector2(0.5f, 1f);
        float optionTop = optionIndex * (optionHeight + optionSpacing);
        optionRect.anchoredPosition = new Vector2(0f, -optionTop);
        optionRect.sizeDelta = new Vector2(0f, optionHeight);

        Image optionBackground = optionObj.GetComponent<Image>();
        optionBackground.color = optionBackgroundColor;

        Button optionButton = optionObj.GetComponent<Button>();
        optionButton.targetGraphic = optionBackground;
        ColorBlock optionColors = optionButton.colors;
        optionColors.normalColor = Color.white;
        optionColors.highlightedColor = optionHighlightedColor;
        optionColors.pressedColor = optionPressedColor;
        optionColors.selectedColor = Color.white;
        optionColors.fadeDuration = 0.05f;
        optionButton.colors = optionColors;
        optionButton.onClick.AddListener(() => OnOptionClicked(action));

        GameObject labelObj = new GameObject(
            "ContextMenu_OptionLabel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(optionObj.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(contentPadding, 0f);
        labelRect.offsetMax = new Vector2(-contentPadding, 0f);

        TMP_Text optionLabel = labelObj.GetComponent<TMP_Text>();
        optionLabel.text = F_Utility_Helper_ContextMenu.GetActionLabel(action);
        optionLabel.color = optionTextColor;
        optionLabel.fontSize = optionFontSize;
        optionLabel.alignment = TextAlignmentOptions.MidlineLeft;
        optionLabel.raycastTarget = false;

        spawnedOptionObjs.Add(optionObj);
    }

    private AudioClip GetActionSound(enumItemContextAction action)
    {
        switch (action)
        {
            case enumItemContextAction.Unload:
                return unloadActionSound;
            default:
                return null;
        }
    }

    private void OnOptionClicked(enumItemContextAction action)
    {
        F_Item item = displayedItem;
        F_PlayerInventory playerInventory = ownerInventory;
        Hide();
        F_Utility_Helper_ContextMenu.ExecuteAction(action, item, playerInventory, GetActionSound(action));
    }

    private void PositionBesidePointer(Vector2 pointerScreenPosition)
    {
        RectTransform canvasRect = contextMenuCanvas.GetComponent<RectTransform>();
        Camera eventCamera = contextMenuCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : contextMenuCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerScreenPosition, eventCamera, out Vector2 pointerLocalPosition))
        {
            return;
        }

        Rect canvasBounds = canvasRect.rect;
        float pointerX = pointerLocalPosition.x - canvasBounds.xMin;
        float pointerFromTop = canvasBounds.yMax - pointerLocalPosition.y;
        float menuWidth = contextMenuRect.rect.width;
        float menuHeight = contextMenuRect.rect.height;
        float menuX = pointerX + cursorGap;

        if (menuX + menuWidth > canvasBounds.width - screenEdgePadding)
        {
            menuX = pointerX - cursorGap - menuWidth;
        }

        menuX = Mathf.Clamp(menuX, screenEdgePadding, canvasBounds.width - menuWidth - screenEdgePadding);
        float menuTop = Mathf.Clamp(pointerFromTop - cursorGap, screenEdgePadding, canvasBounds.height - menuHeight - screenEdgePadding);
        contextMenuRect.anchoredPosition = new Vector2(menuX, -menuTop);
    }
}
