/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents AmmoCycleListHUD state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AmmoCycleListHUD : MonoBehaviour
{
    // Keep the ammo chooser visually comparable to the weapon quick slots:
    // 20% larger rows, with a 15% wider gap between expanded entries.
    private const float ListWidth = 246f;
    private const float RowHeight = 37.2f;
    private const float RowSpacing = 5.75f;
    private const float BottomOffset = 45f;
    private const float HorizontalPadding = 10.8f;
    private const float AmountWidth = 51.6f;
    private const float KeyWidth = 33.6f;
    private const float KeyBlockGap = 7.2f;
    private const float ContentVerticalInset = 2.4f;
    private const float AmountHeightInset = 7.2f;
    private const float KeyHeightInset = 6f;

    private static readonly Color NormalRowColor = new(0.025f, 0.035f, 0.065f, 0.94f);
    private static readonly Color SelectedRowColor = new(0.08f, 0.12f, 0.18f, 0.98f);
    private static readonly Color AmountColor = new(0.34f, 0.36f, 0.39f, 1f);
    private static readonly Color KeyColor = new(0.92f, 0.94f, 0.97f, 1f);
    private static readonly Color DarkTextColor = new(0.08f, 0.09f, 0.11f, 1f);

    private readonly List<GameObject> _rows = new();
    private RectTransform _root;
    private TMP_Text _textTemplate;
    private RectTransform _lastAnchor;
    private string _lastKeyLabel;
    private int _lastContentSignature;
    private bool _lastExpanded;
    private int _lastHighlightedIndex = -1;

    // Purpose: Refreshes the displayed or cached refresh.
    public void Refresh(
        IReadOnlyList<CompatibleAmmoDisplayEntry> entries,
        RectTransform activeWeaponSlot,
        string cycleKeyLabel,
        bool expanded,
        int highlightedIndex)
    {
        if (entries == null || entries.Count == 0 || activeWeaponSlot == null)
        {
            ClearRows();
            _lastAnchor = null;
            _lastKeyLabel = null;
            _lastContentSignature = 0;
            _lastExpanded = false;
            _lastHighlightedIndex = -1;

            if (_root != null)
                _root.gameObject.SetActive(false);

            return;
        }

        string resolvedKeyLabel = string.IsNullOrWhiteSpace(cycleKeyLabel)
            ? "T"
            : cycleKeyLabel.ToUpperInvariant();
        int contentSignature = CalculateContentSignature(entries);

        if (_root != null &&
            _lastAnchor == activeWeaponSlot &&
            _lastKeyLabel == resolvedKeyLabel &&
            _lastContentSignature == contentSignature &&
            _lastExpanded == expanded &&
            _lastHighlightedIndex == highlightedIndex)
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            return;
        }

        ClearRows();
        _lastAnchor = activeWeaponSlot;
        _lastKeyLabel = resolvedKeyLabel;
        _lastContentSignature = contentSignature;
        _lastExpanded = expanded;
        _lastHighlightedIndex = highlightedIndex;

        EnsureRoot(activeWeaponSlot);
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();

        int selectedIndex = FindSelectedIndex(entries);
        int resolvedHighlightedIndex = Mathf.Clamp(
            highlightedIndex >= 0 ? highlightedIndex : selectedIndex,
            0,
            entries.Count - 1);
        float listHeight = expanded
            ? entries.Count * RowHeight +
              Mathf.Max(0, entries.Count - 1) * RowSpacing
            : RowHeight;
        _root.sizeDelta = new Vector2(ListWidth, listHeight);

        if (!expanded)
        {
            // The collapsed row is the currently selected entry from the
            // inventory list. It is not a separate magazine-only entry.
            float selectedRowY = RowHeight * 0.5f;
            CreateRow(
                entries[selectedIndex],
                "Selected",
                selectedRowY,
                true);
            CreateCycleKeyBlock("Selected", selectedRowY, resolvedKeyLabel);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            float rowY =
                listHeight -
                RowHeight * 0.5f -
                i * (RowHeight + RowSpacing);

            bool isHighlighted = i == resolvedHighlightedIndex;
            string rowId = $"Inventory_{i}";
            CreateRow(entries[i], rowId, rowY, isHighlighted);

            if (isHighlighted)
                CreateCycleKeyBlock(rowId, rowY, resolvedKeyLabel);
        }
    }

    // Purpose: Finds the requested selected index.
    private static int FindSelectedIndex(
        IReadOnlyList<CompatibleAmmoDisplayEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsSelected)
                return i;
        }

        return 0;
    }

    // Purpose: Calculates content signature from the current inputs.
    private static int CalculateContentSignature(
        IReadOnlyList<CompatibleAmmoDisplayEntry> entries)
    {
        unchecked
        {
            int hash = 17;

            for (int i = 0; i < entries.Count; i++)
            {
                CompatibleAmmoDisplayEntry entry = entries[i];
                hash = hash * 31 + (entry.Bullet != null
                    ? entry.Bullet.GetInstanceID()
                    : 0);
                hash = hash * 31 + entry.ReserveAmount;
                hash = hash * 31 + entry.FirstInventorySlotIndex;
                hash = hash * 31 + (entry.IsSelected ? 1 : 0);
            }

            return hash;
        }
    }

    // Purpose: Ensures root is ready before the workflow continues.
    private void EnsureRoot(RectTransform activeWeaponSlot)
    {
        if (_root == null)
        {
            GameObject rootObject = new(
                "AmmoCycleList",
                typeof(RectTransform),
                typeof(CanvasGroup));
            _root = rootObject.GetComponent<RectTransform>();

            CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (_root.parent != activeWeaponSlot)
            _root.SetParent(activeWeaponSlot, false);

        _root.anchorMin = new Vector2(0.5f, 1f);
        _root.anchorMax = new Vector2(0.5f, 1f);
        _root.pivot = new Vector2(0.5f, 0f);
        _root.anchoredPosition = new Vector2(0f, BottomOffset);
        _root.localRotation = Quaternion.identity;
        _root.localScale = Vector3.one;

        _textTemplate = activeWeaponSlot.GetComponentInChildren<TMP_Text>(true);
    }

    // Purpose: Creates the row owned by this workflow.
    private void CreateRow(
        CompatibleAmmoDisplayEntry entry,
        string rowId,
        float anchoredY,
        bool selected)
    {
        string bulletName = entry.Bullet != null ? entry.Bullet.name : "None";
        GameObject rowObject = new(
            $"Ammo_{rowId}_{bulletName}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.SetParent(_root, false);
        row.anchorMin = new Vector2(0f, 0f);
        row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(0f, RowHeight);
        row.anchoredPosition = new Vector2(
            0f,
            anchoredY);

        Image background = rowObject.GetComponent<Image>();
        background.color = selected ? SelectedRowColor : NormalRowColor;
        background.raycastTarget = false;

        float amountRight = HorizontalPadding;
        float nameRight = amountRight + AmountWidth + 7.2f;

        TMP_Text nameText = CreateText(
            "AmmoName",
            row,
            new Vector2(HorizontalPadding, ContentVerticalInset),
            new Vector2(nameRight, ContentVerticalInset),
            TextAlignmentOptions.MidlineLeft);
        nameText.text = entry.Bullet != null
            ? entry.Bullet.DisplayName
            : "Ammo";
        nameText.color = Color.white;

        Image amountBackground = CreatePanel(
            "ReserveAmount",
            row,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-amountRight, 0f),
            new Vector2(AmountWidth, RowHeight - AmountHeightInset),
            AmountColor);
        amountBackground.rectTransform.pivot = new Vector2(1f, 0.5f);

        TMP_Text amountText = CreateText(
            "Text",
            amountBackground.rectTransform,
            new Vector2(3f, 1f),
            new Vector2(3f, 1f),
            TextAlignmentOptions.Center);
        amountText.text = entry.ReserveAmount.ToString();
        amountText.color = Color.white;

        _rows.Add(rowObject);
    }

    // Purpose: Creates the cycle key block owned by this workflow.
    private void CreateCycleKeyBlock(
        string rowId,
        float anchoredY,
        string cycleKeyLabel)
    {
        if (_root == null || string.IsNullOrWhiteSpace(cycleKeyLabel))
            return;

        // This is a sibling of the ammo row, not a child. The ammo name and
        // reserve amount therefore retain their original block width.
        Image keyBackground = CreatePanel(
            $"AmmoCycleKey_{rowId}",
            _root,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(KeyBlockGap, anchoredY),
            new Vector2(KeyWidth, RowHeight - KeyHeightInset),
            KeyColor);
        keyBackground.rectTransform.pivot = new Vector2(0f, 0.5f);

        TMP_Text keyText = CreateText(
            "Text",
            keyBackground.rectTransform,
            new Vector2(2f, 1f),
            new Vector2(2f, 1f),
            TextAlignmentOptions.Center);
        keyText.text = cycleKeyLabel.ToUpperInvariant();
        keyText.color = DarkTextColor;

        _rows.Add(keyBackground.gameObject);
    }

    // Purpose: Creates the text owned by this workflow.
    private TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        Vector2 insetMin,
        Vector2 insetMax,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = insetMin;
        rect.offsetMax = -insetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 18f;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableWordWrapping = false;

        if (_textTemplate != null && _textTemplate.font != null)
            text.font = _textTemplate.font;

        return text;
    }

    // Purpose: Creates the panel owned by this workflow.
    private static Image CreatePanel(
        string objectName,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        GameObject panelObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Purpose: Clears the current rows state.
    private void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            GameObject row = _rows[i];
            if (row == null)
                continue;

            row.SetActive(false);

            if (Application.isPlaying)
                Destroy(row);
            else
                DestroyImmediate(row);
        }

        _rows.Clear();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        ClearRows();
    }
}
