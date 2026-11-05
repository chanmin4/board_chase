/*
 * Role hierarchy: Presentation > UI > Gameplay > PlayerTimer
 * Primary responsibility: Presents PlayerTimerHUD state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerTimerHUD : MonoBehaviour
{
    private const float CompactWidth = 350f;
    private const float CompactHeight = 54f;
    private const string CompactStringTable = "UI_PlayerTimer";

    [Header("Events")]
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressSnapshotChangedChannel;

    [Header("UI")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _conditionText;
    [SerializeField] private TextMeshProUGUI _stageText;

    [Header("Stage Text Localization")]
    [SerializeField] private LocalizeStringEvent _stageLocalizeEvent;
    [SerializeField] private TableReference _stageStringTable = "UI_PlayerTimer";
    [SerializeField] private string _stageEntryKey = "PlayerTimer_Stage";
    [SerializeField] private string _vaccineCountdownEntryKey = "PlayerTimer_VaccineCountdown";
    [SerializeField] private string _virusCountdownEntryKey = "PlayerTimer_VirusCountdown";
    [SerializeField] private string _neutralCountdownEntryKey = "PlayerTimer_NeutralCountdown";
    [Header("Display")]
    [SerializeField] private bool _showStageName = true;
    [SerializeField] private bool _showDecimalSeconds = false;

    [Header("Smoothing")]
    [SerializeField] private bool _smoothFill = true;
    [SerializeField, Range(0.5f, 20f)] private float _smoothSpeed = 8f;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new Color(0.2f, 0.75f, 1f, 1f);
    [SerializeField] private Color _readyColor = new Color(0.25f, 1f, 0.45f, 1f);
    [SerializeField] private Color _filledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color _completedColor = new Color(1f, 0.85f, 0.2f, 1f);
    
    private float _targetFill;
    private float _currentFill;
    private bool _hasSnapshot;
    private bool _prefixStageNumberOnLocalizedText;
    private int _localizedStageNumber;
    private bool _compactPresentationBuilt;
    private TextMeshProUGUI _compactLocationText;
    private TextMeshProUGUI _compactTimeText;
    private SectorStateManager _sectorStateManager;
    private SectorRuntime _lastCompactSector;
    private int _lastCompactStageIndex = -1;
    private bool _lastCompactIsHideout;
    private int _lastRaidDisplaySecond = -1;
    private bool _lastRaidGasReleased;
    private PlayerShooterInfection _medicalPlayer;
    private float _nextMedicalLookup;
    private string _lastMedicalLabel;
    private StageProgressSnapshot _latestSnapshot;
    private Coroutine _localizationRefreshRoutine;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureCompactMiniMapPresentation();
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        // The HUD and MiniMap are sibling prefab instances, so their Awake order
        // is not guaranteed. Retry once after every scene object has awakened.
        EnsureCompactMiniMapPresentation();
        RefreshCompactPresentation(force: true);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        EnsureCompactMiniMapPresentation();

        if (_stageProgressSnapshotChangedChannel != null)
        {
            _stageProgressSnapshotChangedChannel.OnEventRaised += OnStageProgressSnapshotChanged;

            if (_stageProgressSnapshotChangedChannel.HasCurrent)
                OnStageProgressSnapshotChanged(_stageProgressSnapshotChangedChannel.Current);
        }

        if (_stageLocalizeEvent != null)
            _stageLocalizeEvent.OnUpdateString.AddListener(OnStageLocalizedStringUpdated);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RequestLocalizationRefresh();

        ApplyInstant();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_stageProgressSnapshotChangedChannel != null)
            _stageProgressSnapshotChangedChannel.OnEventRaised -= OnStageProgressSnapshotChanged;

        if (_stageLocalizeEvent != null)
            _stageLocalizeEvent.OnUpdateString.RemoveListener(OnStageLocalizedStringUpdated);

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_localizationRefreshRoutine != null)
        {
            StopCoroutine(_localizationRefreshRoutine);
            _localizationRefreshRoutine = null;
        }
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        EnsureCompactMiniMapPresentation();
        RefreshCompactPresentation(force: false);
        RefreshMedicalTimer();

        if (!_hasSnapshot || _fillImage == null)
            return;

        if (!_smoothFill)
        {
            ApplyInstant();
            return;
        }

        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _currentFill = Mathf.MoveTowards(_currentFill, _targetFill, _smoothSpeed * dt);
        _fillImage.fillAmount = _currentFill;
    }

    private void RefreshMedicalTimer()
    {
        if (_medicalPlayer == null && Time.unscaledTime >= _nextMedicalLookup)
        {
            _nextMedicalLookup = Time.unscaledTime + 1f;
            _medicalPlayer = FindFirstObjectByType<PlayerShooterInfection>();
        }
        if (_compactTimeText == null) return;
        string label = _medicalPlayer == null ? "" : !GameProgressionRuntime.InRaid ? "안전구역" :
            _medicalPlayer.IsAmpouleActive ? "앰플 " + FormatClockTime(Mathf.Ceil(_medicalPlayer.AmpouleRemainingSeconds)) :
            _medicalPlayer.CurrentBiochemicalGasInfectionPerSecond > 0f ? "감염 노출 중" : "감염 관리";
        if (label == _lastMedicalLabel && _compactTimeText.text == label) return;
        _lastMedicalLabel = label;
        _compactTimeText.text = label;
    }

    // Purpose: Responds to the stage progress snapshot changed callback.
    private void OnStageProgressSnapshotChanged(StageProgressSnapshot snapshot)
    {
        _hasSnapshot = true;
        _latestSnapshot = snapshot;

        bool showTimer = false; // The old room deadline is retired.

        _targetFill = !showTimer
            ? 0f
            : 1f - Mathf.Clamp01(snapshot.progress01);

        ApplyTexts(snapshot);
        ApplyColor(snapshot);
        RefreshCompactPresentation(force: true);

        if (!_smoothFill)
            ApplyInstant();
    }

    // Purpose: Applies the requested texts change.
    private void ApplyTexts(StageProgressSnapshot snapshot)
    {
        bool showTimer = false;

        if (_timeText != null)
        {
            _timeText.text = showTimer
                ? FormatTime(snapshot.remainingSeconds)
                : string.Empty;
        }

        if (_fillImage != null)
            _fillImage.enabled = showTimer;

        if (_conditionText != null)
            _conditionText.text = string.Empty;

        ApplyStageText(snapshot);

        RefreshMedicalTimer();
    }
    // Purpose: Applies the requested stage text change.
    private void ApplyStageText(StageProgressSnapshot snapshot)
    {
        if (!_showStageName)
        {
            SetStageText(string.Empty);
            return;
        }

        if (snapshot.isResolveCountdown)
        {
            switch (snapshot.resolveCountdownOwner)
            {
                case SectorOwner.Player:
                    ApplyStageLocalizedKey(_vaccineCountdownEntryKey, false, 0);
                    break;

                case SectorOwner.Virus:
                    ApplyStageLocalizedKey(_virusCountdownEntryKey, false, 0);
                    break;

                default:
                    ApplyStageLocalizedKey(_neutralCountdownEntryKey, false, 0);
                    break;
            }

            return;
        }

        ApplyStageLocalizedKey(_stageEntryKey, true, Mathf.Max(1, snapshot.stageIndex));
    }


    // Purpose: Applies the requested stage localized key change.
    private void ApplyStageLocalizedKey(string key, bool prefixStageNumber, int stageNumber)
    {
        _prefixStageNumberOnLocalizedText = prefixStageNumber;
        _localizedStageNumber = stageNumber;

        if (_stageLocalizeEvent == null || string.IsNullOrWhiteSpace(key))
        {
            SetStageText(prefixStageNumber ? $"{stageNumber} Stage" : string.Empty);
            return;
        }

        _stageLocalizeEvent.StringReference.TableReference = _stageStringTable;
        _stageLocalizeEvent.StringReference.TableEntryReference = key;
        _stageLocalizeEvent.RefreshString();
    }

    // Purpose: Responds to the stage localized string updated callback.
    private void OnStageLocalizedStringUpdated(string localizedText)
    {
        if (_prefixStageNumberOnLocalizedText)
            SetStageText($"{_localizedStageNumber} {localizedText}");
        else
            SetStageText(localizedText);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        _lastRaidDisplaySecond = -1;
        RequestLocalizationRefresh();
    }

    // Purpose: Reapplies code-generated stage and room labels after the active table loads.
    private void RequestLocalizationRefresh()
    {
        if (_localizationRefreshRoutine != null)
            StopCoroutine(_localizationRefreshRoutine);

        _localizationRefreshRoutine = StartCoroutine(RefreshLocalizedTextWhenReady());
    }

    private IEnumerator RefreshLocalizedTextWhenReady()
    {
        yield return UIRuntimeLocalization.EnsureTableLoaded(CompactStringTable, "PlayerTimer_StageFormat");

        if (isActiveAndEnabled)
        {
            if (_hasSnapshot)
                ApplyTexts(_latestSnapshot);

            RefreshCompactPresentation(force: true);
        }

        _localizationRefreshRoutine = null;
    }

    // Purpose: Updates the stage text state.
    private void SetStageText(string text)
    {
        if (_stageText != null)
            _stageText.text = text;
    }


    // Purpose: Applies the requested color change.
    private void ApplyColor(StageProgressSnapshot snapshot)
    {
        if (_fillImage == null)
            return;

        if (!snapshot.showPlayerTimer)
        {
            _fillImage.color = _normalColor;
            return;
        }

        if (snapshot.isCompleted)
        {
            _fillImage.color = _completedColor;
            return;
        }

        if (!snapshot.requirementMet)
        {
            _fillImage.color = _filledColor;
            return;
        }

        _fillImage.color = snapshot.hasNextStage ? _normalColor : _readyColor;
    }
    // Purpose: Performs the format time workflow for this component.
    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);

        if (_showDecimalSeconds)
            return $"{seconds:0.0}s";

        int rounded = Mathf.CeilToInt(seconds);
        int min = rounded / 60;
        int sec = rounded % 60;

        if (min > 0)
            return $"{min}:{sec:00}";

        return $"{sec}s";
    }

    // Purpose: Performs the format clock time workflow for this component.
    private static string FormatClockTime(float seconds)
    {
        int rounded = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{rounded / 60:00}:{rounded % 60:00}";
    }

    // Purpose: Ensures compact mini map presentation is ready before the workflow continues.
    private void EnsureCompactMiniMapPresentation()
    {
        if (_compactPresentationBuilt)
            return;

        MiniMapUI miniMap = FindFirstObjectByType<MiniMapUI>(FindObjectsInactive.Include);
        if (miniMap == null)
            return;

        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        TMP_FontAsset font = _timeText != null && _timeText.font != null
            ? _timeText.font
            : TMP_Settings.defaultFontAsset;

        // The former top-wide progress bar is retired. Keep its serialized refs
        // intact for GUID/prefab compatibility, but remove the old visuals.
        for (int i = 0; i < root.childCount; i++)
            root.GetChild(i).gameObject.SetActive(false);

        root.SetParent(miniMap.transform, false);
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -10f);
        root.sizeDelta = new Vector2(CompactWidth, CompactHeight);
        root.localScale = Vector3.one;
        root.SetAsLastSibling();

        GameObject rowObject = new(
            "StageRoomTimerRow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(HorizontalLayoutGroup));
        rowObject.layer = gameObject.layer;
        rowObject.transform.SetParent(root, false);

        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.one;
        rowRect.anchoredPosition = Vector2.zero;
        rowRect.sizeDelta = Vector2.zero;

        Image background = rowObject.GetComponent<Image>();
        background.color = new Color(0.018f, 0.047f, 0.071f, 0.94f);
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.spacing = 9f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        _compactLocationText = CreateCompactText(
            rowObject.transform,
            "StageRoomText",
            font,
            TextAlignmentOptions.MidlineLeft,
            flexibleWidth: 1f,
            preferredWidth: 0f);

        GameObject clockObject = new(
            "ClockIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(LayoutElement),
            typeof(StageClockIconGraphic));
        clockObject.layer = gameObject.layer;
        clockObject.transform.SetParent(rowObject.transform, false);
        LayoutElement clockLayout = clockObject.GetComponent<LayoutElement>();
        clockLayout.preferredWidth = 26f;
        clockLayout.preferredHeight = 26f;
        StageClockIconGraphic clock = clockObject.GetComponent<StageClockIconGraphic>();
        clock.color = new Color(0.72f, 0.92f, 1f, 1f);
        clock.raycastTarget = false;

        _compactTimeText = CreateCompactText(
            rowObject.transform,
            "StageTimeText",
            font,
            TextAlignmentOptions.MidlineRight,
            flexibleWidth: 0f,
            preferredWidth: 120f);
        _compactTimeText.text = _hasSnapshot
            ? FormatClockTime(_latestSnapshot.remainingSeconds)
            : "00:00";

        _compactPresentationBuilt = true;
    }

    // Purpose: Creates the compact text owned by this workflow.
    private TextMeshProUGUI CreateCompactText(
        Transform parent,
        string objectName,
        TMP_FontAsset font,
        TextAlignmentOptions alignment,
        float flexibleWidth,
        float preferredWidth)
    {
        GameObject textObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 22f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 22f;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        LayoutElement element = textObject.GetComponent<LayoutElement>();
        element.flexibleWidth = flexibleWidth;
        element.preferredWidth = preferredWidth;
        return text;
    }

    // Purpose: Refreshes the displayed or cached compact presentation.
    private void RefreshCompactPresentation(bool force)
    {
        if (!_compactPresentationBuilt || _compactLocationText == null)
            return;

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        SectorRuntime sector = _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;

        _lastCompactSector = sector;
        bool isHideout = sector != null && sector.GetComponent<HideoutRuntime>() != null;
        int stageIndex = Mathf.Max(1, _latestSnapshot.stageIndex > 0 ? _latestSnapshot.stageIndex :
            _sectorStateManager != null ? _sectorStateManager.CurrentStage : 1);
        if (!force && _lastCompactStageIndex == stageIndex && _lastCompactIsHideout == isHideout) return;
        _lastCompactStageIndex = stageIndex;
        _lastCompactIsHideout = isHideout;
        if (isHideout)
        {
            _compactLocationText.text = LocalizeCompact("PlayerTimer_Hideout", "Hideout");
            return;
        }

        _compactLocationText.text = FormatCompactStage(stageIndex);
    }

    private static string FormatCompactStage(int stageIndex)
    {
        return UIRuntimeLocalization.Format(
            CompactStringTable,
            "PlayerTimer_StageFormat",
            "Stage {0}",
            stageIndex);
    }

    private static string LocalizeCompact(string key, string fallback)
    {
        return UIRuntimeLocalization.Get(CompactStringTable, key, fallback);
    }

    // Purpose: Performs the format room coordinate workflow for this component.
    private static string FormatRoomCoordinate(Vector2Int coord)
    {
        int column = Mathf.Max(0, coord.x);
        string letters = string.Empty;

        do
        {
            letters = (char)('A' + column % 26) + letters;
            column = column / 26 - 1;
        }
        while (column >= 0);

        return $"{letters}{Mathf.Max(0, coord.y) + 1}";
    }

    // Purpose: Performs the format room type workflow for this component.
    private static string FormatRoomType(StageRoomType roomType)
    {
        return roomType switch
        {
            StageRoomType.Normal => LocalizeCompact("PlayerTimer_RoomSector", "Sector"),
            StageRoomType.Treasure => LocalizeCompact("PlayerTimer_RoomTreasure", "Treasure"),
            StageRoomType.Shop => LocalizeCompact("PlayerTimer_RoomShop", "Shop"),
            StageRoomType.Boss => LocalizeCompact("PlayerTimer_RoomBoss", "Boss"),
            StageRoomType.Named => LocalizeCompact("PlayerTimer_RoomNamed", "Named"),
            StageRoomType.BigMonsterWave => LocalizeCompact("PlayerTimer_RoomWave", "Wave"),
            _ => string.Empty
        };
    }

    // Purpose: Applies the requested instant change.
    private void ApplyInstant()
    {
        _currentFill = _targetFill;

        if (_fillImage != null)
            _fillImage.fillAmount = _currentFill;
    }
}
