using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class F_GUI_ItemTooltip : MonoBehaviour
{
    [Header("Constants Private")]
    private const float tooltipWidth = 360f;
    private const float contentPadding = 14f;
    private const float borderThickness = 2f;
    private const float iconSize = 56f;
    private const float sectionSpacing = 10f;
    // Must clear the custom cursor sprite's on-screen radius (~45px at the reference resolution) so the
    // tooltip never overlaps the cursor, which always renders on top of other Layer_Cursor UI.
    private const float cursorGap = 52f;
    private const float screenEdgePadding = 12f;
    private const float minimumDetailFontSize = 11f;
    private const float minimumDescriptionFontSize = 11f;

    [Header("Tooltip References")]
    public Canvas tooltipCanvas;
    public RectTransform tooltipRect;
    public CanvasGroup tooltipCanvasGroup;
    public Image itemIconImage;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemDetailsText;
    public RectTransform itemDescriptionRect;
    public RectTransform itemDetailsRect;
    public RectTransform separatorRect;

    [Header("Privates")]
    private F_Item displayedItem;
    private float defaultDescriptionFontSize;
    private float defaultDetailsFontSize;
    private float currentTooltipWidth = tooltipWidth;

    private void Awake()
    {
        if (tooltipRect == null)
        {
            tooltipRect = GetComponent<RectTransform>();
        }

        if (tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (tooltipCanvas == null)
        {
            tooltipCanvas = GetComponentInParent<Canvas>();
        }

        if (itemDescriptionText != null)
        {
            defaultDescriptionFontSize = itemDescriptionText.fontSize;
        }

        if (itemDetailsText != null)
        {
            defaultDetailsFontSize = itemDetailsText.fontSize;
        }

        Hide();
    }

    /// <summary>Displays an item's data and positions the tooltip beside the pointer.</summary>
    public void Show(F_Item item, Vector2 pointerScreenPosition)
    {
        if (item == null || tooltipCanvas == null || tooltipRect == null || tooltipCanvasGroup == null)
        {
            Hide();
            return;
        }

        RefreshItemContent(item);

        tooltipCanvasGroup.alpha = 1f;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;
        PositionBesidePointer(pointerScreenPosition);
    }

    /// <summary>Hides the tooltip without disabling its reusable UI hierarchy.</summary>
    public void Hide()
    {
        displayedItem = null;
        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
        }
    }

    private void RefreshItemContent(F_Item item)
    {
        // Content is refreshed after comparing the item presentation data.

        string itemName = item.itemName ?? string.Empty;
        string itemDescription = item.itemDescription ?? string.Empty;
        Sprite itemSprite = item.itemSprite;

        StringBuilder detailsBuilder = new StringBuilder();
        IReadOnlyList<F_ItemTooltipDetail> details = item.GetTooltipDetails();
        for (int detailIndex = 0; detailIndex < details.Count; detailIndex++)
        {
            if (detailIndex > 0)
            {
                detailsBuilder.Append('\n');
            }

            detailsBuilder.Append(details[detailIndex].Label);
            detailsBuilder.Append(": ");
            detailsBuilder.Append(details[detailIndex].Value);
        }

        string itemDetails = detailsBuilder.ToString();
        if (displayedItem == item && itemNameText.text == itemName &&
            itemDescriptionText.text == itemDescription && itemDetailsText.text == itemDetails &&
            itemIconImage.sprite == itemSprite)
        {
            return;
        }

        displayedItem = item;
        itemNameText.text = itemName;
        itemDescriptionText.text = itemDescription;
        itemDescriptionText.fontSize = defaultDescriptionFontSize;
        itemDetailsText.fontSize = defaultDetailsFontSize;
        itemIconImage.sprite = itemSprite;
        itemIconImage.enabled = itemSprite != null;
        itemDetailsText.text = itemDetails;
        LayoutTooltipContent();
    }

    private void LayoutTooltipContent()
    {
        RectTransform canvasRect = tooltipCanvas.GetComponent<RectTransform>();
        currentTooltipWidth = Mathf.Max(1f, Mathf.Min(tooltipWidth, canvasRect.rect.width - screenEdgePadding * 2f));
        itemDescriptionText.fontSize = defaultDescriptionFontSize;
        itemDetailsText.fontSize = defaultDetailsFontSize;
        float contentWidth = Mathf.Max(1f, currentTooltipWidth - contentPadding * 2f);
        float descriptionTop = contentPadding + iconSize + sectionSpacing;
        itemDescriptionRect.anchoredPosition = new Vector2(contentPadding, -descriptionTop);
        itemDescriptionRect.sizeDelta = new Vector2(contentWidth, 0f);
        itemDescriptionText.ForceMeshUpdate();
        float descriptionHeight = GetPreferredHeight(itemDescriptionText, contentWidth);
        itemDescriptionRect.sizeDelta = new Vector2(contentWidth, descriptionHeight);

        float detailsTop = descriptionTop + descriptionHeight + sectionSpacing;
        separatorRect.anchoredPosition = new Vector2(contentPadding, -detailsTop);
        separatorRect.sizeDelta = new Vector2(contentWidth, borderThickness);

        float detailTextTop = detailsTop + sectionSpacing;
        itemDetailsRect.anchoredPosition = new Vector2(contentPadding, -detailTextTop);
        itemDetailsRect.sizeDelta = new Vector2(contentWidth, 0f);
        itemDetailsText.ForceMeshUpdate();
        float detailsHeight = GetPreferredHeight(itemDetailsText, contentWidth);
        itemDetailsRect.sizeDelta = new Vector2(contentWidth, detailsHeight);

        float tooltipHeight = detailTextTop + detailsHeight + contentPadding;
        float maximumHeight = Mathf.Max(1f, canvasRect.rect.height - screenEdgePadding * 2f);
        if (tooltipHeight > maximumHeight)
        {
            float heightScale = maximumHeight / tooltipHeight;
            itemDescriptionText.fontSize = Mathf.Max(minimumDescriptionFontSize, itemDescriptionText.fontSize * heightScale);
            itemDetailsText.fontSize = Mathf.Max(minimumDetailFontSize, itemDetailsText.fontSize * heightScale);

            itemDescriptionText.ForceMeshUpdate();
            descriptionHeight = GetPreferredHeight(itemDescriptionText, contentWidth);
            itemDescriptionRect.sizeDelta = new Vector2(contentWidth, descriptionHeight);

            detailsTop = descriptionTop + descriptionHeight + sectionSpacing;
            separatorRect.anchoredPosition = new Vector2(contentPadding, -detailsTop);
            detailTextTop = detailsTop + sectionSpacing;
            itemDetailsRect.anchoredPosition = new Vector2(contentPadding, -detailTextTop);
            itemDetailsText.ForceMeshUpdate();
            detailsHeight = GetPreferredHeight(itemDetailsText, contentWidth);
            itemDetailsRect.sizeDelta = new Vector2(contentWidth, detailsHeight);
            tooltipHeight = detailTextTop + detailsHeight + contentPadding;
        }

        tooltipRect.sizeDelta = new Vector2(currentTooltipWidth, tooltipHeight);
    }

    private static float GetPreferredHeight(TMP_Text text, float width)
    {
        if (string.IsNullOrEmpty(text.text))
        {
            return 0f;
        }

        return Mathf.Ceil(text.GetPreferredValues(text.text, width, Mathf.Infinity).y);
    }

    private void PositionBesidePointer(Vector2 pointerScreenPosition)
    {
        RectTransform canvasRect = tooltipCanvas.GetComponent<RectTransform>();
        float availableWidth = canvasRect.rect.width - screenEdgePadding * 2f;
        float maximumHeight = canvasRect.rect.height - screenEdgePadding * 2f;
        if (Mathf.Abs(currentTooltipWidth - Mathf.Min(tooltipWidth, availableWidth)) > 0.5f || tooltipRect.rect.height > maximumHeight)
        {
            LayoutTooltipContent();
        }

        Camera eventCamera = tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tooltipCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerScreenPosition, eventCamera, out Vector2 pointerLocalPosition))
        {
            return;
        }

        Rect canvasBounds = canvasRect.rect;
        float pointerX = pointerLocalPosition.x - canvasBounds.xMin;
        float pointerFromTop = canvasBounds.yMax - pointerLocalPosition.y;
        float tooltipHeight = tooltipRect.rect.height;
        float tooltipX = pointerX + cursorGap;

        if (tooltipX + currentTooltipWidth > canvasBounds.width - screenEdgePadding)
        {
            tooltipX = pointerX - cursorGap - currentTooltipWidth;
        }

        tooltipX = Mathf.Clamp(tooltipX, screenEdgePadding, canvasBounds.width - currentTooltipWidth - screenEdgePadding);
        float tooltipTop = Mathf.Clamp(pointerFromTop - cursorGap, screenEdgePadding, canvasBounds.height - tooltipHeight - screenEdgePadding);
        tooltipRect.anchoredPosition = new Vector2(tooltipX, -tooltipTop);
    }
}
