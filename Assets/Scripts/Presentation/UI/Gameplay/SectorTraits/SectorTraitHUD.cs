/*
 * Role hierarchy: Presentation > UI > Gameplay > Sector Traits
 * Primary responsibility: Shows the active stage and room traits beneath the
 * compact minimap timer, and briefly warns the player when the sector changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SectorTraitHUD : MonoBehaviour
{
    [Header("Trait List Runtime Template")]
    [Tooltip("Offset from the compact timer's lower edge. Change this after adjusting the minimap timer layout.")]
    [SerializeField] private Vector2 _traitListOffset = new(0f, -6f);
    [SerializeField, Min(160f)] private float _traitListWidth = 350f;
    [SerializeField, Min(18f)] private float _traitRowHeight = 26f;
    [Tooltip("Each active trait owns one visual slot. The list root itself deliberately has no shared background.")]
    [SerializeField] private Color _traitSlotBackgroundColor = new(0.018f, 0.047f, 0.071f, 0.9f);
    [SerializeField] private Color _traitTextColor = new(0.8f, 0.94f, 1f, 1f);

    [Header("Sector Entry Warning Runtime Template")]
    [SerializeField] private Vector2 _warningViewportAnchor = new(0.5f, 0.72f);
    [SerializeField] private Vector2 _warningSize = new(520f, 96f);
    [SerializeField, Min(0.5f)] private float _warningDuration = 2.2f;
    [SerializeField, Min(0.1f)] private float _warningFadeOutSeconds = 0.35f;
    [SerializeField] private Color _warningBackgroundColor = new(0.08f, 0.015f, 0.015f, 0.9f);
    [SerializeField] private Color _warningTitleColor = new(1f, 0.73f, 0.28f, 1f);
    [SerializeField] private Color _warningDescriptionColor = new(0.94f, 0.94f, 0.94f, 1f);

    private readonly List<TraitRowView> _traitRows = new();
    private readonly List<string> _activeTraitLabels = new();
    private readonly List<string> _activeTraitDescriptions = new();

    private SectorStateManager _sectorStateManager;
    private RectTransform _traitListRoot;
    private CanvasGroup _warningCanvasGroup;
    private TextMeshProUGUI _warningTitleText;
    private TextMeshProUGUI _warningDescriptionText;
    private int _lastTraitSignature = int.MinValue;
    private float _warningHideTime;

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        RefreshPresentation(force: true);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        RefreshPresentation(force: false);
        UpdateWarningFade();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_warningCanvasGroup != null)
            _warningCanvasGroup.gameObject.SetActive(false);
    }

    // Purpose: Refreshes the displayed or cached presentation.
    private void RefreshPresentation(bool force)
    {
        EnsureTraitList();
        EnsureWarningBanner();

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        SectorRuntime sector = _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;
        SectorRoomTraitRuntime traits = sector != null
            ? sector.GetComponent<SectorRoomTraitRuntime>()
            : null;

        int signature = BuildTraitSignature(sector, traits);
        if (!force && signature == _lastTraitSignature)
            return;

        _lastTraitSignature = signature;
        BuildActiveTraitText(traits);
        RefreshTraitRows();

        if (_activeTraitLabels.Count > 0)
            ShowWarning();
        else if (_warningCanvasGroup != null)
            _warningCanvasGroup.gameObject.SetActive(false);
    }

    // Purpose: Builds the trait signature from the current authored data.
    private static int BuildTraitSignature(SectorRuntime sector, SectorRoomTraitRuntime traits)
    {
        if (sector == null || traits == null)
            return 0;

        unchecked
        {
            int signature = sector.GetInstanceID();
            signature = signature * 397 ^ (int)traits.SelectedRoomTrait;
            signature = signature * 397 ^ (traits.HasBiochemicalGas ? 1 : 0);
            signature = signature * 397 ^ traits.TimerFailureLeakStacks;
            signature = signature * 397 ^ (traits.HasDebugRoomTraitOverride ? 1 : 0);
            return signature;
        }
    }

    // Purpose: Builds the active trait text from the current authored data.
    private void BuildActiveTraitText(SectorRoomTraitRuntime traits)
    {
        _activeTraitLabels.Clear();
        _activeTraitDescriptions.Clear();

        if (traits == null)
            return;

        // Stage-wide traits are listed before local room traits so the order is
        // stable: gas first, then the trait of the room the player entered.
        if (traits.HasBiochemicalGas)
        {
            string leakSuffix = traits.TimerFailureLeakStacks > 0
                ? $" (LEAK x{traits.TimerFailureLeakStacks})"
                : string.Empty;
            _activeTraitLabels.Add($"BIOCHEMICAL GAS{leakSuffix}");
            _activeTraitDescriptions.Add(
                traits.TimerFailureLeakStacks > 0
                    ? "Timer failure intensified the gas. Clear or leave this room to end the leak."
                    : "Infection rises while inside this stage. Gas masks reduce the exposure.");
        }

        switch (traits.SelectedRoomTrait)
        {
            case SectorRoomTraitType.Darkness:
                _activeTraitLabels.Add("DARKNESS");
                _activeTraitDescriptions.Add("Vision range is reduced in this sector.");
                break;

            case SectorRoomTraitType.Armed:
                _activeTraitLabels.Add("ARMED");
                _activeTraitDescriptions.Add("Enemies have increased damage, health, speed, and armor.");
                break;

            case SectorRoomTraitType.AmmoScavenger:
                _activeTraitLabels.Add("AMMO SCAVENGER");
                _activeTraitDescriptions.Add("Each shot consumes two rounds when possible.");
                break;

            case SectorRoomTraitType.Fear:
                _activeTraitLabels.Add("FEAR");
                _activeTraitDescriptions.Add("Incoming hits have a much higher injury chance.");
                break;
        }
    }

    // Purpose: Ensures trait list is ready before the workflow continues.
    private void EnsureTraitList()
    {
        if (_traitListRoot != null)
            return;

        if (transform is not RectTransform ownerRoot)
            return;

        GameObject listObject = new(
            "SectorTraitList",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        listObject.layer = gameObject.layer;
        listObject.transform.SetParent(ownerRoot, false);

        _traitListRoot = listObject.GetComponent<RectTransform>();
        _traitListRoot.anchorMin = new Vector2(0.5f, 0f);
        _traitListRoot.anchorMax = new Vector2(0.5f, 0f);
        _traitListRoot.pivot = new Vector2(0.5f, 1f);
        _traitListRoot.anchoredPosition = _traitListOffset;
        _traitListRoot.sizeDelta = new Vector2(_traitListWidth, 0f);

        VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = listObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        listObject.SetActive(false);
    }

    // Purpose: Refreshes the displayed or cached trait rows.
    private void RefreshTraitRows()
    {
        if (_traitListRoot == null)
            return;

        while (_traitRows.Count < _activeTraitLabels.Count)
            _traitRows.Add(CreateTraitRow(_traitListRoot));

        for (int i = 0; i < _traitRows.Count; i++)
        {
            bool active = i < _activeTraitLabels.Count;
            TraitRowView row = _traitRows[i];
            row.gameObject.SetActive(active);
            if (active)
                row.Text.text = _activeTraitLabels[i];
        }

        _traitListRoot.gameObject.SetActive(_activeTraitLabels.Count > 0);
    }

    // Purpose: Creates the trait row owned by this workflow.
    private TraitRowView CreateTraitRow(Transform parent)
    {
        GameObject rowObject = new(
            "TraitSlot",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        rowObject.layer = gameObject.layer;
        rowObject.transform.SetParent(parent, false);

        Image background = rowObject.GetComponent<Image>();
        background.color = _traitSlotBackgroundColor;
        background.raycastTarget = false;

        GameObject textObject = new(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(rowObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI row = textObject.GetComponent<TextMeshProUGUI>();
        row.font = TMP_Settings.defaultFontAsset;
        row.fontSize = 17f;
        row.enableAutoSizing = true;
        row.fontSizeMin = 12f;
        row.fontSizeMax = 17f;
        row.alignment = TextAlignmentOptions.Midline;
        row.color = _traitTextColor;
        row.textWrappingMode = TextWrappingModes.NoWrap;
        row.overflowMode = TextOverflowModes.Ellipsis;
        row.raycastTarget = false;

        LayoutElement element = rowObject.GetComponent<LayoutElement>();
        element.minHeight = _traitRowHeight;
        element.preferredHeight = _traitRowHeight;
        return new TraitRowView(rowObject, row);
    }

    // Purpose: Ensures warning banner is ready before the workflow continues.
    private void EnsureWarningBanner()
    {
        if (_warningCanvasGroup != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        GameObject warningObject = new(
            "SectorTraitWarning",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        warningObject.layer = gameObject.layer;
        warningObject.transform.SetParent(canvas.transform, false);

        RectTransform warningRect = warningObject.GetComponent<RectTransform>();
        warningRect.anchorMin = _warningViewportAnchor;
        warningRect.anchorMax = _warningViewportAnchor;
        warningRect.pivot = new Vector2(0.5f, 0.5f);
        warningRect.anchoredPosition = Vector2.zero;
        warningRect.sizeDelta = _warningSize;

        Image background = warningObject.GetComponent<Image>();
        background.color = _warningBackgroundColor;
        background.raycastTarget = false;

        _warningCanvasGroup = warningObject.GetComponent<CanvasGroup>();
        _warningCanvasGroup.blocksRaycasts = false;
        _warningCanvasGroup.interactable = false;

        _warningTitleText = CreateWarningText(
            warningObject.transform,
            "WarningTitle",
            new Vector2(10f, 51f),
            new Vector2(-10f, -8f),
            26f,
            _warningTitleColor,
            TextAlignmentOptions.Midline);
        _warningDescriptionText = CreateWarningText(
            warningObject.transform,
            "WarningDescription",
            new Vector2(18f, 8f),
            new Vector2(-18f, -43f),
            16f,
            _warningDescriptionColor,
            TextAlignmentOptions.Midline);

        warningObject.SetActive(false);
    }

    // Purpose: Creates the warning text owned by this workflow.
    private TextMeshProUGUI CreateWarningText(
        Transform parent,
        string objectName,
        Vector2 minOffset,
        Vector2 maxOffset,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = minOffset;
        textRect.offsetMax = maxOffset;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(11f, fontSize - 6f);
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    // Purpose: Shows the warning UI or visual state.
    private void ShowWarning()
    {
        if (_warningCanvasGroup == null || _warningTitleText == null || _warningDescriptionText == null)
            return;

        _warningTitleText.text = _activeTraitLabels.Count == 1
            ? $"WARNING: {_activeTraitLabels[0]}"
            : "WARNING: ROOM CONDITIONS";
        _warningDescriptionText.text = BuildWarningDescription();
        _warningCanvasGroup.alpha = 1f;
        _warningCanvasGroup.gameObject.SetActive(true);
        _warningHideTime = Time.unscaledTime + _warningDuration;
    }

    // Purpose: Updates the warning fade from the current runtime state.
    private void UpdateWarningFade()
    {
        if (_warningCanvasGroup == null || !_warningCanvasGroup.gameObject.activeSelf)
            return;

        float remaining = _warningHideTime - Time.unscaledTime;
        if (remaining <= 0f)
        {
            _warningCanvasGroup.gameObject.SetActive(false);
            return;
        }

        _warningCanvasGroup.alpha = remaining < _warningFadeOutSeconds
            ? Mathf.Clamp01(remaining / _warningFadeOutSeconds)
            : 1f;
    }

    // Purpose: Keeps multi-trait warnings readable without merging independent traits into one label.
    private string BuildWarningDescription()
    {
        StringBuilder builder = new();
        for (int i = 0; i < _activeTraitLabels.Count; i++)
        {
            if (i > 0)
                builder.Append('\n');

            builder.Append(_activeTraitLabels[i]);
            builder.Append(": ");
            builder.Append(_activeTraitDescriptions[i]);
        }

        return builder.ToString();
    }

    private sealed class TraitRowView
    {
        public TraitRowView(GameObject gameObject, TextMeshProUGUI text)
        {
            this.gameObject = gameObject;
            Text = text;
        }

        public GameObject gameObject { get; }
        public TextMeshProUGUI Text { get; }
    }
}
