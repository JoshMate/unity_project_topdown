using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A single floating damage or heal number with a leading type icon.
/// Moves upwards at a constant speed, then fades out and destroys itself. Text and icon grow with the amount.
/// After a short delay it can pool into an existing number with the same damage type, source and target ent.
/// New numbers choose a start position that does not overlap other numbers.
/// </summary>
public class F_Effects_DamageNumber : MonoBehaviour
{
    [Header("Constants Private")]
    private const float damageNumberHalf = 0.5f;
    private const float damageNumberTextScale = 0.1f;
    private const int damageNumberTextSortingBoost = 1;

    [Header("Privates")]
    private static readonly List<F_Effects_DamageNumber> activeNumbers = new List<F_Effects_DamageNumber>();
    private Vector3 damageNumberStart;
    private float damageNumberAge;
    private float damageNumberFadeStartAge;
    private float damageNumberFadeDuration;
    private bool damageNumberHasTriedPooling;
    private float damageNumberAmount;
    private bool damageNumberIsHeal;
    private enumDamageType damageNumberType;
    private F_Ent damageNumberEnt;
    private GameObject damageNumberSource;
    private Vector2 damageNumberSize;
    private TMP_Text damageNumberText;
    private SpriteRenderer damageNumberIcon;
    private Color damageNumberBaseColour;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeNumbers.Clear();
    }

    /// <summary>Creates a damage number for an ent, placed so it does not overlap existing numbers.</summary>
    /// <param name="ent">The ent that took the damage or healing.</param>
    /// <param name="source">The object that caused it; may be null.</param>
    /// <param name="damageType">The damage type, used for pooling.</param>
    /// <param name="amount">Damage or heal amount to display.</param>
    /// <param name="isHeal">Whether this is healing rather than damage.</param>
    /// <param name="icon">Optional icon drawn left of the text.</param>
    /// <param name="position">World position the number starts from.</param>
    /// <param name="sortingLayerId">Sorting layer to draw on.</param>
    /// <param name="sortingOrder">Sorting order to draw at.</param>
    public static void Spawn(
        F_Ent ent,
        GameObject source,
        enumDamageType damageType,
        float amount,
        bool isHeal,
        Sprite icon,
        Vector3 position,
        int sortingLayerId,
        int sortingOrder)
    {
        GameObject root = new GameObject(isHeal ? "HealNumber" : "DamageNumber");
        F_Effects_DamageNumber number = root.AddComponent<F_Effects_DamageNumber>();
        number.Build(ent, source, damageType, amount, isHeal, icon, position, sortingLayerId, sortingOrder);
    }

    private void Build(
        F_Ent ent,
        GameObject source,
        enumDamageType damageType,
        float amount,
        bool isHeal,
        Sprite icon,
        Vector3 position,
        int sortingLayerId,
        int sortingOrder)
    {
        damageNumberEnt = ent;
        damageNumberSource = source;
        damageNumberType = damageType;
        damageNumberAmount = amount;
        damageNumberIsHeal = isHeal;
        damageNumberFadeStartAge = F_Utility_Config_Damage.cfgDamageNumberLifetime;
        damageNumberFadeDuration = F_Utility_Config_Damage.cfgDamageNumberFadeDuration;
        damageNumberBaseColour = isHeal
            ? F_Utility_Config_Damage.cfgDamageNumberColourHeal
            : F_Utility_Config_Damage.cfgDamageNumberColourDamage;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localScale = Vector3.one * damageNumberTextScale;
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        damageNumberText = text;
        text.fontSize = F_Utility_Config_Damage.cfgDamageNumberFontSize / damageNumberTextScale;
        text.fontStyle = F_Utility_Config_Damage.cfgDamageNumberFontStyle;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = damageNumberBaseColour;
        text.rectTransform.pivot = new Vector2(0f, damageNumberHalf);

        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
        textRenderer.sortingLayerID = sortingLayerId;
        textRenderer.sortingOrder = sortingOrder + damageNumberTextSortingBoost;

        if (icon != null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(transform, false);
            damageNumberIcon = iconObject.AddComponent<SpriteRenderer>();
            damageNumberIcon.sprite = icon;
            damageNumberIcon.sortingLayerID = sortingLayerId;
            damageNumberIcon.sortingOrder = sortingOrder;
            float iconScale = F_Utility_Config_Damage.cfgDamageNumberIconHeight / icon.bounds.size.y;
            iconObject.transform.localScale = Vector3.one * iconScale;
        }

        Layout();
        damageNumberStart = FindFreeStart(position);
        transform.position = damageNumberStart;
        activeNumbers.Add(this);
    }

    private void OnDestroy()
    {
        activeNumbers.Remove(this);
    }

    // Updates the text, size scaling and icon/text arrangement, centred on the root, then measures the world size
    private void Layout()
    {
        string prefix = damageNumberIsHeal ? F_Utility_Config_Damage.cfgDamageNumberHealPrefix : string.Empty;
        damageNumberText.text = prefix + damageNumberAmount.ToString(F_Utility_Config_Damage.cfgDamageNumberFormat);
        damageNumberText.ForceMeshUpdate();

        float sizeFraction = Mathf.Clamp01(damageNumberAmount / F_Utility_Config_Damage.cfgDamageNumberScaleFullDamage);
        float scale = Mathf.Lerp(
            F_Utility_Config_Damage.cfgDamageNumberScaleMin,
            F_Utility_Config_Damage.cfgDamageNumberScaleMax,
            sizeFraction);
        transform.localScale = Vector3.one * scale;

        float textWidth = damageNumberText.preferredWidth * damageNumberTextScale;
        float textHeight = damageNumberText.preferredHeight * damageNumberTextScale;
        float iconWidth = 0f;
        float iconGap = 0f;
        float iconHeight = 0f;
        if (damageNumberIcon != null)
        {
            Vector3 iconScale = damageNumberIcon.transform.localScale;
            iconWidth = damageNumberIcon.sprite.bounds.size.x * iconScale.x;
            iconHeight = damageNumberIcon.sprite.bounds.size.y * iconScale.y;
            iconGap = F_Utility_Config_Damage.cfgDamageNumberIconGap;
        }

        float groupWidth = iconWidth + iconGap + textWidth;
        float left = -groupWidth * damageNumberHalf;
        if (damageNumberIcon != null)
        {
            damageNumberIcon.transform.localPosition = new Vector3(left + iconWidth * damageNumberHalf, 0f, 0f);
        }
        damageNumberText.transform.localPosition = new Vector3(left + iconWidth + iconGap, 0f, 0f);

        damageNumberSize = new Vector2(groupWidth, Mathf.Max(textHeight, iconHeight)) * scale;
    }

    // Finds the nearest start position (centre, alternating right/left, then upwards) that does not overlap
    // another active number. Every number moves at the same speed, so a free start stays free.
    private Vector3 FindFreeStart(Vector3 desiredStart)
    {
        float padding = F_Utility_Config_Damage.cfgDamageNumberOverlapPadding;
        int slots = Mathf.Max(1, F_Utility_Config_Damage.cfgDamageNumberOverlapSlotsPerRow);
        int rows = Mathf.Max(1, F_Utility_Config_Damage.cfgDamageNumberOverlapMaxRows);

        for (int row = 0; row < rows; row++)
        {
            for (int slot = 0; slot < slots; slot++)
            {
                int column = slot == 0 ? 0 : (slot % 2 == 1 ? (slot + 1) / 2 : -(slot / 2));
                Vector3 candidate = desiredStart + new Vector3(
                    column * (damageNumberSize.x + padding),
                    row * (damageNumberSize.y + padding),
                    0f);

                if (!OverlapsActiveNumbers(candidate, padding))
                {
                    return candidate;
                }
            }
        }

        return desiredStart;
    }

    private bool OverlapsActiveNumbers(Vector3 start, float padding)
    {
        Rect startRect = MakeRect(start, damageNumberSize, padding);
        foreach (F_Effects_DamageNumber other in activeNumbers)
        {
            if (other == null || other == this)
            {
                continue;
            }

            if (startRect.Overlaps(MakeRect(other.transform.position, other.damageNumberSize, 0f)))
            {
                return true;
            }
        }

        return false;
    }

    private static Rect MakeRect(Vector3 centre, Vector2 size, float padding)
    {
        Vector2 paddedSize = size + Vector2.one * (padding * 2f);
        return new Rect((Vector2)centre - paddedSize * damageNumberHalf, paddedSize);
    }

    // Merges this number into a number with the same type, source and ent that is not yet fading;
    // the receiving number switches to the pooled lifetime and fade, restarting its lifetime
    private bool TryPoolIntoExisting()
    {
        if (!F_Utility_Config_Damage.cfgDamageNumberPoolingEnabled)
        {
            return false;
        }

        foreach (F_Effects_DamageNumber other in activeNumbers)
        {
            if (other == null || other == this || other.damageNumberAge >= other.damageNumberFadeStartAge)
            {
                continue;
            }

            if (other.damageNumberType != damageNumberType ||
                other.damageNumberEnt != damageNumberEnt ||
                other.damageNumberSource != damageNumberSource)
            {
                continue;
            }

            other.damageNumberAmount += damageNumberAmount;
            other.damageNumberFadeStartAge = other.damageNumberAge + F_Utility_Config_Damage.cfgDamageNumberPooledLifetime;
            other.damageNumberFadeDuration = F_Utility_Config_Damage.cfgDamageNumberPooledFadeDuration;
            other.Layout();
            return true;
        }

        return false;
    }

    private void Update()
    {
        damageNumberAge += Time.deltaTime;

        if (!damageNumberHasTriedPooling && damageNumberAge >= F_Utility_Config_Damage.cfgDamageNumberPoolDelay)
        {
            damageNumberHasTriedPooling = true;
            if (TryPoolIntoExisting())
            {
                Destroy(gameObject);
                return;
            }
        }

        if (damageNumberAge >= damageNumberFadeStartAge + damageNumberFadeDuration)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = damageNumberStart + Vector3.up * (F_Utility_Config_Damage.cfgDamageNumberMoveSpeed * damageNumberAge);

        float alpha = damageNumberAge > damageNumberFadeStartAge && damageNumberFadeDuration > 0f
            ? 1f - Mathf.Clamp01((damageNumberAge - damageNumberFadeStartAge) / damageNumberFadeDuration)
            : 1f;

        Color textColour = damageNumberBaseColour;
        textColour.a = alpha;
        damageNumberText.color = textColour;
        if (damageNumberIcon != null)
        {
            damageNumberIcon.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
