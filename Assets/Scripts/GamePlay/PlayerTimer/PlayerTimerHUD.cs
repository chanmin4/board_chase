using TMPro;
using UnityEngine;
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
    [SerializeField] private Color _blockedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color _completedColor = new Color(1f, 0.85f, 0.2f, 1f);

    private float _targetFill;
    private float _currentFill;
    private bool _hasSnapshot;

    private void OnEnable()
    {
        if (_stageProgressSnapshotChangedChannel != null)
            _stageProgressSnapshotChangedChannel.OnEventRaised += OnStageProgressSnapshotChanged;

        ApplyInstant();
    }

    private void OnDisable()
    {
        if (_stageProgressSnapshotChangedChannel != null)
            _stageProgressSnapshotChangedChannel.OnEventRaised -= OnStageProgressSnapshotChanged;
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

        // progress01 = 완료 진행도.
        // 타이머 바를 "남은 시간"으로 보여주고 싶으면 1 - progress01.
        _targetFill = 1f - Mathf.Clamp01(snapshot.progress01);

        ApplyTexts(snapshot);
        ApplyColor(snapshot);

        if (!_smoothFill)
            ApplyInstant();
    }

    private void ApplyTexts(StageProgressSnapshot snapshot)
    {
        if (_timeText != null)
            _timeText.text = FormatTime(snapshot.remainingSeconds);

        if (_conditionText != null)
        {
            if (_showCondition)
            {
                _conditionText.text =
                    $"{snapshot.currentPlayerOwnedCount}/{snapshot.requiredPlayerOwnedCount}";
            }
            else
            {
                _conditionText.text = string.Empty;
            }
        }

        if (_stageText != null)
        {
            if (_showStageName)
            {
                string name = string.IsNullOrEmpty(snapshot.displayName)
                    ? $"Stage {snapshot.stageIndex}"
                    : snapshot.displayName;

                _stageText.text = name;
            }
            else
            {
                _stageText.text = string.Empty;
            }
        }
    }

    private void ApplyColor(StageProgressSnapshot snapshot)
    {
        if (_fillImage == null)
            return;

        if (snapshot.isCompleted)
        {
            _fillImage.color = _completedColor;
            return;
        }

        if (!snapshot.requirementMet)
        {
            _fillImage.color = _blockedColor;
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
