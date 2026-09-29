/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Implements the focused UICanvasGroupOpacity responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        ApplyImmediate(_settings != null ? _settings.DefaultAlpha : 1f);
    }

    // Purpose: Processes this component's per-frame runtime work.
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

    // Purpose: Applies the requested immediate change.
    public void ApplyImmediate(float alpha)
    {
        alpha = ClampAlpha(alpha);
        _targetAlpha = alpha;

        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    // Purpose: Updates the default state.
    public void SetDefault()
    {
        if (_settings == null)
            return;

        _isVisible = true;
        _targetAlpha = _settings.DefaultAlpha;
    }

    // Purpose: Updates the emphasis state.
    public void SetEmphasis()
    {
        if (_settings == null)
            return;

        _isVisible = true;
        _targetAlpha = _settings.EmphasisAlpha;
    }

    // Purpose: Shows the show UI or visual state.
    public void Show(float? alphaOverride = null)
    {
        _isVisible = true;
        _targetAlpha = ClampAlpha(alphaOverride ?? (_settings != null ? _settings.DefaultAlpha : 1f));
    }

    // Purpose: Hides the hide UI or visual state.
    public void Hide()
    {
        _isVisible = false;
        _targetAlpha = _settings != null ? _settings.HiddenAlpha : 0f;
    }
    // Purpose: Hides the immediate UI or visual state.
    public void HideImmediate()
    {
        _isVisible = false;
        _targetAlpha = _settings != null ? _settings.HiddenAlpha : 0f;
        _velocity = 0f;

        if (_canvasGroup != null)
            _canvasGroup.alpha = _targetAlpha;
    }

    // Purpose: Updates the alpha state.
    public void SetAlpha(float alpha)
    {
        _isVisible = alpha > 0f;
        _targetAlpha = ClampAlpha(alpha);
    }

    // Purpose: Performs the clamp alpha workflow for this component.
    private float ClampAlpha(float alpha)
    {
        if (_settings == null)
            return Mathf.Clamp01(alpha);

        return Mathf.Clamp(alpha, _settings.MinAlpha, _settings.MaxAlpha);
    }
}
