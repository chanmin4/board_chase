/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Presents the active player status effects in the Stats panel.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a compact player-preview placeholder and a readable, scrollable list of the
/// currently active player status effects to the existing Stats panel content.
/// The preview is deliberately a presentation slot: it does not introduce a second
/// player model or camera, so the eventual authored profile/portrait can replace it.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStatsStatusDetailPanelUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(48f)] private float _previewHeight = 132f;
    [SerializeField, Min(24f)] private float _effectRowMinHeight = 44f;
    [SerializeField, Min(0.05f)] private float _timerRefreshSeconds = 0.2f;

    private readonly List<EffectRowView> _rows = new();
    private RectTransform _contentRoot;
    private RectTransform _effectListRoot;
    private EntityStatusEffectRuntime _runtime;
    private float _nextTimerRefreshAt;

    private sealed class EffectRowView
    {
        public GameObject Root;
        public Image Icon;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;
    }

    // Purpose: Builds the permanent Stats-panel structure once its layout root is available.
    private void Awake()
    {
        BuildPanel();
    }

    // Purpose: Binds the current Player's status runtime when this tab becomes visible.
    private void OnEnable()
    {
        ResolveAndBindRuntime();
        Refresh();
    }

    // Purpose: Releases status subscriptions while the Stats panel is hidden.
    private void OnDisable()
    {
        BindRuntime(null);
    }

    // Purpose: Keeps active-effect countdown labels current without rebuilding every frame.
    private void Update()
    {
        if (_runtime == null)
        {
            ResolveAndBindRuntime();
            return;
        }

        if (Time.unscaledTime < _nextTimerRefreshAt)
            return;

        _nextTimerRefreshAt = Time.unscaledTime + _timerRefreshSeconds;
        Refresh();
    }

    // Purpose: Resolves the existing ScrollRect content that owns the normal stat rows.
    private void BuildPanel()
    {
        if (_contentRoot != null)
            return;

        ScrollRect scrollRect = GetComponent<ScrollRect>();
        _contentRoot = scrollRect != null ? scrollRect.content : null;

        if (_contentRoot == null)
            return;

        Transform existing = _contentRoot.Find("PlayerStatusDetails");
        if (existing != null)
        {
            _effectListRoot = existing.Find("EffectList") as RectTransform;
            return;
        }

        GameObject sectionObject = new GameObject(
            "PlayerStatusDetails",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(LayoutElement));
        RectTransform section = sectionObject.GetComponent<RectTransform>();
        section.SetParent(_contentRoot, false);
        section.SetAsFirstSibling();

        VerticalLayoutGroup sectionLayout = sectionObject.GetComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 5f;
        sectionLayout.padding = new RectOffset(0, 0, 0, 4);
        sectionLayout.childAlignment = TextAnchor.UpperLeft;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = false;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;

        ContentSizeFitter sectionFitter = sectionObject.GetComponent<ContentSizeFitter>();
        sectionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(section, "Title", "STATUS EFFECTS", 19f, FontStyles.Bold, new Color(0.4f, 0.9f, 1f, 1f));
        CreatePreviewSlot(section);
        CreateText(section, "EffectListLabel", "ACTIVE EFFECTS", 14f, FontStyles.Bold, new Color(0.72f, 0.84f, 0.88f, 1f));

        GameObject listObject = new GameObject(
            "EffectList",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        _effectListRoot = listObject.GetComponent<RectTransform>();
        _effectListRoot.SetParent(section, false);

        VerticalLayoutGroup listLayout = listObject.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 3f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        ContentSizeFitter listFitter = listObject.GetComponent<ContentSizeFitter>();
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Purpose: Creates the intentionally replaceable player portrait slot at the top of Stats.
    private void CreatePreviewSlot(RectTransform parent)
    {
        GameObject previewObject = new GameObject(
            "PlayerVisualPreviewSlot",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        RectTransform preview = previewObject.GetComponent<RectTransform>();
        preview.SetParent(parent, false);

        Image background = previewObject.GetComponent<Image>();
        background.color = new Color(0.025f, 0.13f, 0.16f, 0.95f);
        background.raycastTarget = false;

        LayoutElement layout = previewObject.GetComponent<LayoutElement>();
        layout.minHeight = _previewHeight;
        layout.preferredHeight = _previewHeight;

        TextMeshProUGUI label = CreateText(preview, "PreviewLabel", "PLAYER VISUAL\nPREVIEW SLOT", 13f, FontStyles.Bold, new Color(0.65f, 0.86f, 0.9f, 1f));
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.06f, 0.5f);
        labelRect.anchorMax = new Vector2(0.52f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 58f);
        label.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

        GameObject silhouetteObject = new GameObject(
            "SilhouettePlaceholder",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform silhouette = silhouetteObject.GetComponent<RectTransform>();
        silhouette.SetParent(preview, false);
        silhouette.anchorMin = new Vector2(0.74f, 0.5f);
        silhouette.anchorMax = new Vector2(0.74f, 0.5f);
        silhouette.pivot = new Vector2(0.5f, 0.5f);
        silhouette.anchoredPosition = Vector2.zero;
        silhouette.sizeDelta = new Vector2(46f, 94f);

        Image silhouetteImage = silhouetteObject.GetComponent<Image>();
        silhouetteImage.color = new Color(0.17f, 0.7f, 0.86f, 0.85f);
        silhouetteImage.raycastTarget = false;
    }

    // Purpose: Locates the player status runtime without requiring a duplicate UI event channel.
    private void ResolveAndBindRuntime()
    {
        if (_runtime != null)
            return;

        PlayerShooterInfection playerInfection = Object.FindFirstObjectByType<PlayerShooterInfection>();
        BindRuntime(playerInfection != null ? playerInfection.StatusEffects : null);
    }

    // Purpose: Maintains the status-change subscription for the active player only.
    private void BindRuntime(EntityStatusEffectRuntime runtime)
    {
        if (_runtime == runtime)
            return;

        if (_runtime != null)
            _runtime.OnChanged -= HandleStatusChanged;

        _runtime = runtime;

        if (_runtime != null)
            _runtime.OnChanged += HandleStatusChanged;
    }

    // Purpose: Rebuilds visible effect rows after a status is added, refreshed, or removed.
    private void HandleStatusChanged(EntityStatusEffectRuntime runtime)
    {
        Refresh();
    }

    // Purpose: Maps active effects into reusable Stats-panel rows.
    private void Refresh()
    {
        BuildPanel();

        if (_effectListRoot == null)
            return;

        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> effects =
            _runtime != null ? _runtime.ActiveEffects : null;
        int visibleCount = effects != null ? effects.Count : 0;

        EnsureRowCount(Mathf.Max(1, visibleCount));

        if (visibleCount == 0)
        {
            EffectRowView emptyRow = _rows[0];
            emptyRow.Root.SetActive(true);
            emptyRow.Icon.enabled = false;
            emptyRow.Title.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_StatusNoActive",
                "No active status effects");
            emptyRow.Title.color = new Color(0.68f, 0.76f, 0.78f, 1f);
            emptyRow.Description.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_StatusNoActiveDescription",
                "Buffs and injuries currently affecting the player will appear here.");
            SetAdditionalRowsActive(false, 1);
            return;
        }

        for (int i = 0; i < visibleCount; i++)
        {
            EffectRowView row = _rows[i];
            EntityStatusEffectRuntime.ActiveStatusEffect active = effects[i];
            StatusEffectDefinitionSO definition = active.Definition;

            row.Root.SetActive(true);
            row.Icon.enabled = definition != null;
            row.Icon.sprite = definition != null ? StatusEffectIconStripUI.GetIcon(definition) : null;
            row.Icon.color = definition != null ? definition.IconColor : Color.white;

            string polarity = definition != null && definition.IsBuff
                ? UIRuntimeLocalization.Get("UI_Inventory", "Inventory_StatusBuff", "Buff")
                : UIRuntimeLocalization.Get("UI_Inventory", "Inventory_StatusDebuff", "Debuff");
            string effectName = definition != null
                ? definition.Id
                : UIRuntimeLocalization.Get("UI_Inventory", "Inventory_StatusUnknown", "Unknown Status");
            row.Title.text = UIRuntimeLocalization.Format(
                "UI_Inventory",
                "Inventory_StatusRowFormat",
                "{0}  {1}  {2:0.0}s",
                polarity,
                effectName,
                active.RemainingSeconds);
            if (definition != null && definition.PersistentStacks)
                row.Title.text = $"{polarity}  {effectName} · {active.Stacks} 스택 · 치료까지 지속";
            if (definition is InfectionAggravationStatusSO aggravation)
                row.Title.text = $"{polarity}  감염악화 · {active.Stacks} 스택 · 원형 {active.Gauge:0.#}/{aggravation.Settings.baseGaugeCapacity:0.#}";
            row.Title.color = definition != null && definition.IsBuff
                ? new Color(0.43f, 1f, 0.64f, 1f)
                : new Color(1f, 0.58f, 0.45f, 1f);
            row.Description.text = BuildDescription(definition);
        }

        SetAdditionalRowsActive(false, visibleCount);
    }

    // Purpose: Ensures the panel owns enough rows for the active runtime state.
    private void EnsureRowCount(int requiredCount)
    {
        while (_rows.Count < requiredCount)
            _rows.Add(CreateEffectRow(_effectListRoot));
    }

    // Purpose: Creates one compact icon-title-description status row.
    private EffectRowView CreateEffectRow(RectTransform parent)
    {
        GameObject rowObject = new GameObject(
            "StatusEffectRow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.SetParent(parent, false);

        Image background = rowObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.2f, 0.24f, 0.8f);
        background.raycastTarget = false;

        LayoutElement layout = rowObject.GetComponent<LayoutElement>();
        layout.minHeight = _effectRowMinHeight;
        layout.preferredHeight = _effectRowMinHeight;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(row, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        iconRect.sizeDelta = new Vector2(30f, 30f);

        TextMeshProUGUI title = CreateText(row, "Title", string.Empty, 13f, FontStyles.Bold, Color.white);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(44f, -4f);
        titleRect.sizeDelta = new Vector2(-50f, 18f);

        TextMeshProUGUI description = CreateText(row, "Description", string.Empty, 10f, FontStyles.Normal, new Color(0.78f, 0.88f, 0.9f, 1f));
        RectTransform descriptionRect = description.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 0.5f);
        descriptionRect.anchoredPosition = new Vector2(44f, -9f);
        descriptionRect.sizeDelta = new Vector2(-50f, 18f);

        return new EffectRowView
        {
            Root = rowObject,
            Icon = iconObject.GetComponent<Image>(),
            Title = title,
            Description = description
        };
    }

    // Purpose: Hides unused pooled status rows after the visible effect range.
    private void SetAdditionalRowsActive(bool active, int fromIndex)
    {
        for (int i = fromIndex; i < _rows.Count; i++)
        {
            if (_rows[i].Root != null)
                _rows[i].Root.SetActive(active);
        }
    }

    // Purpose: Returns an authored status explanation or a useful generated fallback.
    private static string BuildDescription(StatusEffectDefinitionSO definition)
    {
        if (definition == null)
            return "Status definition is missing.";

        if (!string.IsNullOrWhiteSpace(definition.Description))
            return definition.Description;

        List<string> parts = new();
        StatusEffectModifier[] modifiers = definition.Modifiers;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Length; i++)
                parts.Add($"{ResolveModifierName(modifiers[i].type)} x{modifiers[i].multiplier:0.##}");
        }

        if (definition.ArmorClassDelta != 0)
            parts.Add($"Armor Class {definition.ArmorClassDelta:+#;-#}");
        if (definition.BlocksActions)
            parts.Add("Blocks actions");
        if (definition.SuppressesSoundIndicators)
            parts.Add("Hides sound indicators");
        if (definition.IsInjury)
            parts.Add("Injury");

        return parts.Count > 0 ? string.Join(" | ", parts) : "No modifier description authored.";
    }

    // Purpose: Converts authoring enum names into player-readable modifier labels.
    private static string ResolveModifierName(StatusEffectModifierType type)
    {
        return type switch
        {
            StatusEffectModifierType.MoveSpeedMultiplier => "Move Speed",
            StatusEffectModifierType.InfectionRecoveryMultiplier => "Infection Recovery",
            StatusEffectModifierType.SpreadMultiplier => "Weapon Spread",
            StatusEffectModifierType.DamageTakenMultiplier => "Damage Taken",
            StatusEffectModifierType.VisionArcMultiplier => "Vision Arc",
            _ => type.ToString()
        };
    }

    // Purpose: Creates a consistently styled TMP text element under the requested parent.
    private static TextMeshProUGUI CreateText(
        RectTransform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles style,
        Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }
}
