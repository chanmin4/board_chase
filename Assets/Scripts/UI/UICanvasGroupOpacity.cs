using UnityEngine;

[DisallowMultipleComponent]
public class UICanvasGroupOpacity : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private UIOpacitySettingsSO _settings;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private float _targetAlpha;
    [ReadOnly] [SerializeField] private bool _isVisible = true;

    private float _velocity;

    public CanvasGroup CanvasGroup => _canvasGroup;
    public UIOpacitySettingsSO Settings => _settings;
    public float CurrentAlpha => _canvasGroup != null ? _canvasGroup.alpha : 0f;

    private void Reset()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        ApplyImmediate(_settings != null ? _settings.DefaultAlpha : 1f);
    }

    private void Update()
    {
        if (_canvasGroup == null)
            return;

        float duration = _isVisible
            ? (_settings != null ? _settings.FadeInDuration : 0f)
            : (_settings != null ? _settings.FadeOutDuration : 0f);

        if (duration <= 0f)
        {
            _canvasGroup.alpha = _targetAlpha;
            return;
        }

        _canvasGroup.alpha = Mathf.SmoothDamp(
            _canvasGroup.alpha,
            _targetAlpha,
            ref _velocity,
            duration);
    }

    public void ApplyImmediate(float alpha)
    {
        alpha = ClampAlpha(alpha);
        _targetAlpha = alpha;

        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    public void SetDefault()
    {
        if (_settings == null)
            return;

        _isVisible = true;
        _targetAlpha = _settings.DefaultAlpha;
    }

    public void SetEmphasis()
    {
        if (_settings == null)
            return;

        _isVisible = true;
        _targetAlpha = _settings.EmphasisAlpha;
    }

    public void Show(float? alphaOverride = null)
    {
        _isVisible = true;
        _targetAlpha = ClampAlpha(alphaOverride ?? (_settings != null ? _settings.DefaultAlpha : 1f));
    }

    public void Hide()
    {
        _isVisible = false;
        _targetAlpha = _settings != null ? _settings.HiddenAlpha : 0f;
    }

    public void SetAlpha(float alpha)
    {
        _isVisible = alpha > 0f;
        _targetAlpha = ClampAlpha(alpha);
    }

    private float ClampAlpha(float alpha)
    {
        if (_settings == null)
            return Mathf.Clamp01(alpha);

        return Mathf.Clamp(alpha, _settings.MinAlpha, _settings.MaxAlpha);
    }
}
