using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InfectionControlHUD : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private InfectionControlEventChannelSO _infectionControlChangedChannel;

    [Header("UI")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private TextMeshProUGUI _drainText;

    [Header("Display")]
    [SerializeField] private bool _showPercent = true;
    [SerializeField] private bool _showRawValue = false;
    [SerializeField] private bool _smoothFill = true;
    [SerializeField] private float _smoothSpeed = 8f;

    [Header("Colors")]
    [SerializeField] private Color _safeColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color _warningColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _dangerColor = new Color(1f, 0.2f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float _warningThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _dangerThreshold = 0.25f;

    private float _targetFill = 1f;
    private InfectionControlSnapshot _latestSnapshot;

    private void OnEnable()
    {
        if (_infectionControlChangedChannel != null)
            _infectionControlChangedChannel.OnEventRaised += OnInfectionControlChanged;
    }

    private void OnDisable()
    {
        if (_infectionControlChangedChannel != null)
            _infectionControlChangedChannel.OnEventRaised -= OnInfectionControlChanged;
    }

    private void Update()
    {
        if (_fillImage == null)
            return;

        if (!_smoothFill)
        {
            _fillImage.fillAmount = _targetFill;
            return;
        }

        _fillImage.fillAmount = Mathf.MoveTowards(
            _fillImage.fillAmount,
            _targetFill,
            _smoothSpeed * Time.unscaledDeltaTime);
    }

    private void OnInfectionControlChanged(InfectionControlSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        _targetFill = Mathf.Clamp01(snapshot.normalized);

        ApplyTexts(snapshot);
        ApplyColor(_targetFill);

        if (!_smoothFill && _fillImage != null)
            _fillImage.fillAmount = _targetFill;
    }

    private void ApplyTexts(InfectionControlSnapshot snapshot)
    {
        if (_valueText != null)
        {
            if (_showRawValue)
            {
                int current = Mathf.CeilToInt(snapshot.current);
                int max = Mathf.CeilToInt(snapshot.max);
                _valueText.text = $"{current} / {max}";
            }
            else if (_showPercent)
            {
                _valueText.text = $"{Mathf.CeilToInt(snapshot.normalized * 100f)}%";
            }
            else
            {
                _valueText.text = string.Empty;
            }
        }

        if (_drainText != null)
        {
            _drainText.gameObject.SetActive(snapshot.drainPerSecond > 0f);
            _drainText.text = $"-{snapshot.drainPerSecond:0.#}/s";
        }
    }

    private void ApplyColor(float normalized)
    {
        if (_fillImage == null)
            return;

        if (normalized <= _dangerThreshold)
            _fillImage.color = _dangerColor;
        else if (normalized <= _warningThreshold)
            _fillImage.color = _warningColor;
        else
            _fillImage.color = _safeColor;
    }
}
