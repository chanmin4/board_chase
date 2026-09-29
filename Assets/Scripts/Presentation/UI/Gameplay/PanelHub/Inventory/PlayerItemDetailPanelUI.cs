/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents PlayerItemDetailPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerItemDetailPanelUI : MonoBehaviour
{
    [Header("Authored Refs")]
    [Tooltip("CanvasGroup on the pre-authored DescriptionHub parent. It is hidden until an item is selected.")]
    [SerializeField] private CanvasGroup _descriptionHubCanvasGroup;
    [Tooltip("The pre-authored ItemDetailPanel child. Its RectTransform is positioned in the Canvas prefab, not at runtime.")]
    [SerializeField] private RectTransform _panelRoot;

    [Header("Editor Preview")]
    [Tooltip("Keeps the pre-authored DescriptionHub visible while editing Canvas-GamePlay. Play mode always starts hidden until an item is selected.")]
    [SerializeField] private bool _showEditorPreview = true;

    [Header("Style")]
    [SerializeField] private Color _backgroundColor = new(0.015f, 0.07f, 0.09f, 0.96f);
    [SerializeField] private Color _outlineColor = new(0f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color _secondaryTextColor = new(0.65f, 0.9f, 0.95f, 1f);
    [SerializeField] private Color _warningColor = new(1f, 0.35f, 0.25f, 1f);

    private Image _icon;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _summaryText;
    private TextMeshProUGUI _detailsText;
    private LocalizedString _boundName;
    private LocalizedString _boundDescription;
    private string _localizedDescription;
    private ItemDetailSelection _selection;
    private RectTransformLayout _authoredLayout;
    private bool _hasAuthoredLayout;
    private Transform _authoredHubParent;
    private int _authoredHubSiblingIndex;

    private struct RectTransformLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveAuthoredRefs();
        CaptureAuthoredLayout();
        BuildView();
        ShowEmpty();
    }

    // Purpose: Keeps the pre-authored description column visible and populated in the Scene view without changing Play-mode visibility.
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ResolveAuthoredRefs();
        ApplyEditorPreview();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveAuthoredRefs();
        BuildView();
        ItemDetailSelectionBus.SelectionChanged += HandleSelectionChanged;

        if (ItemDetailSelectionBus.Current.HasItem)
            HandleSelectionChanged(ItemDetailSelectionBus.Current);
        else
            ShowEmpty();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ItemDetailSelectionBus.SelectionChanged -= HandleSelectionChanged;
        UnbindLocalizedStrings();
    }

    // Purpose: Handles the incoming selection changed request or event.
    private void HandleSelectionChanged(ItemDetailSelection selection)
    {
        if (!selection.HasItem)
        {
            ShowEmpty();
            return;
        }

        BuildView();
        SetVisible(true);
        UnbindLocalizedStrings();
        _selection = selection;
        _localizedDescription = string.Empty;

        if (_icon != null)
        {
            _icon.sprite = selection.item.PreviewImage;
            _icon.enabled = _icon.sprite != null;
        }

        if (_nameText != null)
            _nameText.text = InventoryItemDisplayUtility.ResolveItemName(selection.item);

        BindLocalizedStrings(selection.item);
        RefreshText();
    }

    // Purpose: Shows the empty UI or visual state.
    private void ShowEmpty()
    {
        BuildView();
        UnbindLocalizedStrings();
        _selection = default;
        SetVisible(false);

        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_nameText != null)
            _nameText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailSelectItem",
                "Select an item");

        if (_summaryText != null)
            _summaryText.text = string.Empty;

        if (_detailsText != null)
            _detailsText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailSelectItemDescription",
                "Select an inventory, shop, or loot slot to view details.");
    }

    // Purpose: Refreshes the displayed or cached text.
    private void RefreshText()
    {
        if (!_selection.HasItem)
            return;

        if (_summaryText != null)
        {
            _summaryText.text = BuildSummary(_selection);
            _summaryText.color = IsBrokenWeapon(_selection)
                ? _warningColor
                : _secondaryTextColor;
        }

        if (_detailsText != null)
        {
            StringBuilder builder = new();
            AppendGameplayDetails(builder, _selection);

            if (!string.IsNullOrWhiteSpace(_localizedDescription))
            {
                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append(_localizedDescription);
            }

            _detailsText.text = builder.Length > 0
                ? builder.ToString()
                : UIRuntimeLocalization.Get(
                    "UI_Inventory",
                    "Inventory_DetailNoDescription",
                    "No details are available.");
        }
    }

    // Purpose: Builds the summary from the current authored data.
    private string BuildSummary(ItemDetailSelection selection)
    {
        string amount = selection.amount > 1 ? $"  x{selection.amount}" : string.Empty;
        string summary = $"{selection.item.Kind}{amount}";

        if (selection.item is RangedWeaponSO weapon)
        {
            float maximum = ResolveWeaponMaximum(selection, weapon);
            float current = ResolveWeaponCurrent(selection, maximum);
            float condition = maximum > 0f ? Mathf.Clamp01(current / maximum) : 1f;
            float performance = Mathf.Lerp(0.5f, 1f, condition);
            summary += $"\n내구도  {current:0.#} / {maximum:0.#}\n공격·정확도 성능  {performance * 100f:0}%";
        }
        else if (selection.item is ArmorItemSO armor)
        {
            float maximum = selection.armorMaxDurability >= 0f
                ? selection.armorMaxDurability
                : armor.MaxDurability;
            float current = selection.armorDurability >= 0f
                ? selection.armorDurability
                : maximum;
            summary += $"\n내구도  {current:0.#} / {maximum:0.#}";
        }
        else if (selection.item is GasMaskItemSO gasMask)
        {
            float maximum = selection.gasMaskMaxFilterDurability >= 0f
                ? selection.gasMaskMaxFilterDurability
                : gasMask.MaxFilterDurability;
            float current = selection.gasMaskFilterDurability >= 0f
                ? Mathf.Clamp(selection.gasMaskFilterDurability, 0f, maximum)
                : maximum;
            summary += $"\n필터 내구도  {current:0.#} / {maximum:0.#}";
        }

        return summary;
    }

    // Purpose: Performs the append gameplay details workflow for this component.
    private static void AppendGameplayDetails(StringBuilder builder, ItemDetailSelection selection)
    {
        switch (selection.item)
        {
            case RangedWeaponSO weapon:
            {
                float maximum = ResolveWeaponMaximum(selection, weapon);
                float current = ResolveWeaponCurrent(selection, maximum);
                float condition = maximum > 0f ? Mathf.Clamp01(current / maximum) : 1f;
                float performance = Mathf.Lerp(0.5f, 1f, condition);

                builder.AppendLine($"총기 종류: {weapon.WeaponType}");
                builder.AppendLine($"사용 탄약: {weapon.RequiredBulletFamily}");
                builder.AppendLine($"발사당 내구도 감소: {weapon.DurabilityLossPerFire:0.##}");
                builder.AppendLine($"현재 공격력·정확도 계수: x{performance:0.##}");
                builder.Append($"다음 수리 후 최대 내구도: {ResolveRepairedMaximum(current, maximum, weapon):0.#}");
                break;
            }

            case ArmorItemSO armor:
                builder.AppendLine($"방어구 클래스: {armor.ArmorClass}");
                builder.Append("피격 시 흡수한 피해만큼 내구도가 감소합니다.");
                break;

            case GasMaskItemSO gasMask:
            {
                float maximum = selection.gasMaskMaxFilterDurability >= 0f
                    ? selection.gasMaskMaxFilterDurability
                    : gasMask.MaxFilterDurability;
                float current = selection.gasMaskFilterDurability >= 0f
                    ? Mathf.Clamp(selection.gasMaskFilterDurability, 0f, maximum)
                    : maximum;
                float nextMaximum = ResolveRepairedMaximum(current, maximum, gasMask.RepairPermanentLossRatio);
                builder.AppendLine($"가스 감염 배율: x{gasMask.GasInfectionMultiplier:0.##}");
                builder.AppendLine($"필터 소모율: {gasMask.FilterDurabilityLossPerRawInfection:0.##}");
                builder.Append($"다음 수리 후 최대 필터 내구도: {nextMaximum:0.#}");
                break;
            }

            case BulletSO bullet:
                builder.AppendLine($"탄종: {bullet.Family}");
                builder.AppendLine($"티어: {(int)bullet.Tier}");
                builder.AppendLine($"관통 클래스: {bullet.PenetrationClass}");
                builder.AppendLine($"HP 피해: {bullet.HealthDamage:0.##}");
                builder.AppendLine($"Infection 피해: {bullet.InfectionDamage:0.##}");
                builder.Append($"Mark: {bullet.MarkAmount:0.##}");
                break;

            case ConsumableItemSO consumable:
                builder.AppendLine($"체력 회복: {consumable.HealAmount:0.##}");
                builder.Append($"사용 시간: {consumable.UseDurationSeconds:0.##}초");
                break;
        }
    }

    // Purpose: Resolves the weapon maximum needed by this component.
    private static float ResolveWeaponMaximum(ItemDetailSelection selection, RangedWeaponSO weapon)
    {
        return selection.weaponMaxDurability > 0f
            ? selection.weaponMaxDurability
            : weapon.MaxDurability;
    }

    // Purpose: Resolves the weapon current needed by this component.
    private static float ResolveWeaponCurrent(ItemDetailSelection selection, float maximum)
    {
        return selection.weaponCurrentDurability >= 0f
            ? Mathf.Clamp(selection.weaponCurrentDurability, 0f, maximum)
            : maximum;
    }

    // Purpose: Resolves the repaired maximum needed by this component.
    private static float ResolveRepairedMaximum(
        float current,
        float maximum,
        RangedWeaponSO weapon)
    {
        if (current >= maximum)
            return maximum;

        return Mathf.Max(
            1f,
            maximum - ((maximum - current) * weapon.RepairPermanentLossRatio));
    }

    // Purpose: Predicts an item-specific repaired maximum without changing runtime condition.
    private static float ResolveRepairedMaximum(
        float current,
        float maximum,
        float repairPermanentLossRatio)
    {
        if (current >= maximum)
            return maximum;

        return Mathf.Max(
            1f,
            maximum - ((maximum - current) * Mathf.Clamp01(repairPermanentLossRatio)));
    }

    // Purpose: Checks whether broken weapon is currently true.
    private static bool IsBrokenWeapon(ItemDetailSelection selection)
    {
        return selection.item is RangedWeaponSO weapon &&
               ResolveWeaponCurrent(
                   selection,
                   ResolveWeaponMaximum(selection, weapon)) <= 0f;
    }

    // Purpose: Binds localized strings to the active runtime or UI target.
    private void BindLocalizedStrings(ItemSO item)
    {
        if (InventoryItemDisplayUtility.TryGetItemNameReference(item, out _boundName))
        {
            _boundName.StringChanged += HandleLocalizedNameChanged;
            _boundName.RefreshString();
        }

        if (item.Description != null && !item.Description.IsEmpty)
        {
            _boundDescription = item.Description;
            _boundDescription.StringChanged += HandleLocalizedDescriptionChanged;
            _boundDescription.RefreshString();
        }
    }

    // Purpose: Removes the localized strings binding.
    private void UnbindLocalizedStrings()
    {
        if (_boundName != null)
            _boundName.StringChanged -= HandleLocalizedNameChanged;

        if (_boundDescription != null)
            _boundDescription.StringChanged -= HandleLocalizedDescriptionChanged;

        _boundName = null;
        _boundDescription = null;
    }

    // Purpose: Handles the incoming localized name changed request or event.
    private void HandleLocalizedNameChanged(string value)
    {
        if (_nameText != null && !string.IsNullOrWhiteSpace(value))
            _nameText.text = value;
    }

    // Purpose: Handles the incoming localized description changed request or event.
    private void HandleLocalizedDescriptionChanged(string value)
    {
        _localizedDescription = value ?? string.Empty;
        RefreshText();
    }

    // Purpose: Builds the view from the current authored data.
    private void BuildView()
    {
        if (_panelRoot == null)
            return;

        ResolveExistingViewRefs();

        Image background = _panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = _backgroundColor;
            background.raycastTarget = false;
        }

        Outline outline = _panelRoot.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = _outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // Description uses one solid column backdrop. The scrolling viewport
        // remains a mask only; tinting it as another black Image produced a
        // second nested background in the Loot workspace.
        ConfigureDetailsViewport();

        if (_icon != null && _nameText != null && _summaryText != null && _detailsText != null)
            return;

        if (_icon == null)
            _icon = CreateIcon(_panelRoot);

        if (_nameText == null)
        {
            _nameText = CreateText("ItemName", _panelRoot, 28f, FontStyles.Bold);
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(118f, -20f);
            nameRect.sizeDelta = new Vector2(-136f, 58f);
        }

        if (_summaryText == null)
        {
            _summaryText = CreateText("ItemSummary", _panelRoot, 19f, FontStyles.Normal);
            _summaryText.color = _secondaryTextColor;
            RectTransform summaryRect = _summaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 1f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.pivot = new Vector2(0f, 1f);
            summaryRect.anchoredPosition = new Vector2(118f, -82f);
            summaryRect.sizeDelta = new Vector2(-136f, 112f);
        }

        if (_detailsText == null)
            CreateDetailsScroll(_panelRoot);
    }

    // Purpose: Keeps the authored Details viewport as a transparent clipping mask rather than a second visual panel.
    private void ConfigureDetailsViewport()
    {
        if (_panelRoot == null)
            return;

        Transform detailsScroll = _panelRoot.Find("DetailsScroll");
        Transform viewport = detailsScroll != null ? detailsScroll.Find("Viewport") : null;
        if (viewport == null || !viewport.TryGetComponent(out Image viewportImage))
            return;

        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = false;
    }

    // Purpose: Reuses the description controls authored into Canvas-GamePlay instead of creating a second runtime-only set.
    private void ResolveExistingViewRefs()
    {
        _icon ??= FindViewComponent<Image>("ItemIcon");
        _nameText ??= FindViewComponent<TextMeshProUGUI>("ItemName");
        _summaryText ??= FindViewComponent<TextMeshProUGUI>("ItemSummary");
        _detailsText ??= FindViewComponent<TextMeshProUGUI>("Details");
    }

    // Purpose: Finds one pre-authored view component by its stable child name.
    private T FindViewComponent<T>(string objectName) where T : Component
    {
        if (_panelRoot == null)
            return null;

        foreach (T component in _panelRoot.GetComponentsInChildren<T>(true))
        {
            if (component != null && component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    // Purpose: Applies the Scene-view-only description sample while leaving all runtime selection behavior unchanged.
    private void ApplyEditorPreview()
    {
        if (_descriptionHubCanvasGroup != null)
        {
            _descriptionHubCanvasGroup.alpha = _showEditorPreview ? 1f : 0f;
            _descriptionHubCanvasGroup.interactable = false;
            _descriptionHubCanvasGroup.blocksRaycasts = false;
        }

        if (!_showEditorPreview || _panelRoot == null)
            return;

        _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRoot.pivot = new Vector2(0.5f, 0.5f);
        _panelRoot.anchoredPosition = Vector2.zero;
        _panelRoot.sizeDelta = new Vector2(360f, 840f);
        _panelRoot.localScale = Vector3.one;

        BuildView();

        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_nameText != null)
            _nameText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewTitle",
                "Description");

        if (_summaryText != null)
        {
            _summaryText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewSummary",
                "Item Details");
            _summaryText.color = _secondaryTextColor;
        }

        if (_detailsText != null)
            _detailsText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewDescription",
                "Scene preview only\n\nSelect an item in Play mode to show its name, stats, condition, and description here.");
    }

    // Purpose: Resolves the authored refs needed by this component.
    private void ResolveAuthoredRefs()
    {
        if (_panelRoot == null)
            _panelRoot = transform as RectTransform;

        if (_descriptionHubCanvasGroup == null)
            _descriptionHubCanvasGroup = GetComponentInParent<CanvasGroup>(true);
    }

    /// <summary>
    /// Uses a compact top-center readout for Shop and a full-height middle column
    /// for Loot. It is still selection-driven, so an empty description hub neither
    /// obscures slots nor consumes drag/drop raycasts.
    /// </summary>
    public void SetContextOverlayPresentation(bool isContextOverlay, bool useLootTransferLayout = false)
    {
        ResolveAuthoredRefs();
        CaptureAuthoredLayout();

        if (_panelRoot == null)
            return;

        if (isContextOverlay && useLootTransferLayout)
        {
            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.anchoredPosition = Vector2.zero;
            _panelRoot.sizeDelta = new Vector2(360f, 840f);
            _panelRoot.localScale = Vector3.one;

            if (_descriptionHubCanvasGroup != null)
                _descriptionHubCanvasGroup.transform.SetAsLastSibling();
        }
        else if (isContextOverlay)
        {
            _panelRoot.anchorMin = new Vector2(0.5f, 1f);
            _panelRoot.anchorMax = new Vector2(0.5f, 1f);
            _panelRoot.pivot = new Vector2(0.5f, 1f);
            _panelRoot.anchoredPosition = new Vector2(0f, -72f);
            _panelRoot.sizeDelta = new Vector2(420f, 250f);
            _panelRoot.localScale = Vector3.one;

            if (_descriptionHubCanvasGroup != null)
                _descriptionHubCanvasGroup.transform.SetAsLastSibling();
        }
        else if (_hasAuthoredLayout)
        {
            _panelRoot.anchorMin = _authoredLayout.anchorMin;
            _panelRoot.anchorMax = _authoredLayout.anchorMax;
            _panelRoot.pivot = _authoredLayout.pivot;
            _panelRoot.anchoredPosition = _authoredLayout.anchoredPosition;
            _panelRoot.sizeDelta = _authoredLayout.sizeDelta;
            _panelRoot.localScale = _authoredLayout.localScale;

            if (_descriptionHubCanvasGroup != null && _authoredHubParent != null)
            {
                _descriptionHubCanvasGroup.transform.SetSiblingIndex(
                    Mathf.Clamp(
                        _authoredHubSiblingIndex,
                        0,
                        Mathf.Max(0, _authoredHubParent.childCount - 1)));
            }
        }
    }

    // Purpose: Caches the scene-authored detail panel layout before context presentation overrides it.
    private void CaptureAuthoredLayout()
    {
        if (_hasAuthoredLayout || _panelRoot == null)
            return;

        _authoredLayout = new RectTransformLayout
        {
            anchorMin = _panelRoot.anchorMin,
            anchorMax = _panelRoot.anchorMax,
            pivot = _panelRoot.pivot,
            anchoredPosition = _panelRoot.anchoredPosition,
            sizeDelta = _panelRoot.sizeDelta,
            localScale = _panelRoot.localScale
        };
        _hasAuthoredLayout = true;

        if (_descriptionHubCanvasGroup != null)
        {
            _authoredHubParent = _descriptionHubCanvasGroup.transform.parent;
            _authoredHubSiblingIndex = _descriptionHubCanvasGroup.transform.GetSiblingIndex();
        }
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_descriptionHubCanvasGroup == null)
            return;

        _descriptionHubCanvasGroup.alpha = visible ? 1f : 0f;
        // Detail is information-only. Keeping it non-blocking lets users drag
        // between the left player grid and right Shop/Loot grid below it.
        _descriptionHubCanvasGroup.interactable = false;
        _descriptionHubCanvasGroup.blocksRaycasts = false;
    }

    // Purpose: Creates the icon owned by this workflow.
    private Image CreateIcon(RectTransform parent)
    {
        GameObject iconObject = new("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -20f);
        rect.sizeDelta = new Vector2(84f, 84f);

        Image image = iconObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // Purpose: Creates the details scroll owned by this workflow.
    private void CreateDetailsScroll(RectTransform parent)
    {
        GameObject scrollObject = new("DetailsScroll", typeof(RectTransform), typeof(ScrollRect));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.SetParent(parent, false);
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(16f, 16f);
        scrollRect.offsetMax = new Vector2(-16f, -208f);

        GameObject viewportObject = new(
            "Viewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = false;

        _detailsText = CreateText("Details", viewportRect, 18f, FontStyles.Normal);
        RectTransform contentRect = _detailsText.rectTransform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -10f);
        contentRect.sizeDelta = new Vector2(-20f, 0f);
        _detailsText.enableWordWrapping = true;
        _detailsText.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter fitter = _detailsText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
    }

    // Purpose: Creates the text owned by this workflow.
    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float fontSize,
        FontStyles style)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }
}
