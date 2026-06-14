using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerTimerHUD : MonoBehaviour
{
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
    [SerializeField] private string _stageBreakEntryKey = "PlayerTimer_StageBreakTime";
    [SerializeField] private string _vaccineCountdownEntryKey = "PlayerTimer_VaccineCountdown";
    [SerializeField] private string _virusCountdownEntryKey = "PlayerTimer_VirusCountdown";
    [SerializeField] private string _neutralCountdownEntryKey = "PlayerTimer_NeutralCountdown";
    [Header("Display")]
    [SerializeField] private bool _showStageName = true;
    [SerializeField] private bool _showCondition = true;
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
    [SerializeField] private Color _breakTimeColor = new Color(0.85f, 0.55f, 1f, 1f);
    
    private float _targetFill;
    private float _currentFill;
    private bool _hasSnapshot;
    private bool _prefixStageNumberOnLocalizedText;
    private int _localizedStageNumber;
    private void OnEnable()
    {
        if (_stageProgressSnapshotChangedChannel != null)
        {
            _stageProgressSnapshotChangedChannel.OnEventRaised += OnStageProgressSnapshotChanged;

            if (_stageProgressSnapshotChangedChannel.HasCurrent)
                OnStageProgressSnapshotChanged(_stageProgressSnapshotChangedChannel.Current);
        }

        if (_stageLocalizeEvent != null)
            _stageLocalizeEvent.OnUpdateString.AddListener(OnStageLocalizedStringUpdated);

        ApplyInstant();
    }

    private void OnDisable()
    {
        if (_stageProgressSnapshotChangedChannel != null)
            _stageProgressSnapshotChangedChannel.OnEventRaised -= OnStageProgressSnapshotChanged;

        if (_stageLocalizeEvent != null)
            _stageLocalizeEvent.OnUpdateString.RemoveListener(OnStageLocalizedStringUpdated);
    }

    private void Update()
    {
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

    private void OnStageProgressSnapshotChanged(StageProgressSnapshot snapshot)
    {
        _hasSnapshot = true;

        bool showTimer = snapshot.showPlayerTimer || snapshot.isResting;

        _targetFill = !showTimer
            ? 0f
            : snapshot.isResting
                ? 1f - Mathf.Clamp01(snapshot.restProgress01)
                : 1f - Mathf.Clamp01(snapshot.progress01);

        ApplyTexts(snapshot);
        ApplyColor(snapshot);

        if (!_smoothFill)
            ApplyInstant();
    }

    private void ApplyTexts(StageProgressSnapshot snapshot)
    {
        bool showTimer = snapshot.showPlayerTimer || snapshot.isResting;

        if (_timeText != null)
        {
            _timeText.text = showTimer
                ? FormatTime(snapshot.isResting
                    ? snapshot.restRemainingSeconds
                    : snapshot.remainingSeconds)
                : string.Empty;
        }

        if (_fillImage != null)
            _fillImage.enabled = showTimer;

        if (_conditionText != null)
            _conditionText.text = string.Empty;

        ApplyStageText(snapshot);
    }
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

        if (snapshot.isResting)
        {
            ApplyStageLocalizedKey(_stageBreakEntryKey, false, 0);
            return;
        }

        ApplyStageLocalizedKey(_stageEntryKey, true, Mathf.Max(1, snapshot.stageIndex));
    }


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

    private void OnStageLocalizedStringUpdated(string localizedText)
    {
        if (_prefixStageNumberOnLocalizedText)
            SetStageText($"{_localizedStageNumber} {localizedText}");
        else
            SetStageText(localizedText);
    }

    private void SetStageText(string text)
    {
        if (_stageText != null)
            _stageText.text = text;
    }


    private void ApplyColor(StageProgressSnapshot snapshot)
    {
        if (_fillImage == null)
            return;

        if (!snapshot.showPlayerTimer && !snapshot.isResting)
        {
            _fillImage.color = _normalColor;
            return;
        }

        if (snapshot.isResting)
        {
            _fillImage.color = _breakTimeColor;
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

    private void ApplyInstant()
    {
        _currentFill = _targetFill;

        if (_fillImage != null)
            _fillImage.fillAmount = _currentFill;
    }
}