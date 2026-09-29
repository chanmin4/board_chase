/*
 * Role hierarchy: Presentation > UI > Gameplay > DamageNumber
 * Primary responsibility: Presents DamageNumberPopupUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DamageNumberPopupUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private CanvasGroup _group;

    [Header("Entry")]
    [Tooltip("Single icon used by this popup. The manager decides whether it is damage or heal.")]
    [SerializeField] private Image _iconImage;

    [Tooltip("Single number text used by this popup.")]
    [SerializeField] private TextMeshProUGUI _amountText;

    private Vector2 _baseAnchoredPosition;
    private Vector2 _pairOffset;
    private Vector2 _stackOffset;
    private Vector2 _floatOffset;
    private Vector3 _baseScale;
    private float _duration = 0.8f;
    private float _fadeStartNormalized = 0.75f;
    private float _popDuration = 0.08f;
    private float _popScale = 1.25f;
    private float _elapsed;
    private float _stackAlphaMultiplier = 1f;

    public bool IsVisible => gameObject.activeSelf;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _rectTransform = transform as RectTransform;
        _group = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureRefs();
        _baseScale = transform.localScale;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        _elapsed = 0f;
    }

    // Purpose: Plays the requested play action or effect.
    public void Play(
        Vector2 floatOffset,
        float duration,
        float fadeStartNormalized,
        float popScale,
        float popDuration)
    {
        EnsureRefs();

        _floatOffset = floatOffset;
        _duration = Mathf.Max(0.01f, duration);
        _fadeStartNormalized = Mathf.Clamp01(fadeStartNormalized);
        _popScale = Mathf.Max(1f, popScale);
        _popDuration = Mathf.Max(0.001f, popDuration);
        _elapsed = 0f;
        _pairOffset = Vector2.zero;
        _stackOffset = Vector2.zero;
        _stackAlphaMultiplier = 1f;

        ClearEntry();
        SetAlpha(1f);
        transform.localScale = _baseScale * _popScale;
        gameObject.SetActive(true);
    }

    // Purpose: Updates the base position state.
    public void SetBasePosition(Vector2 anchoredPosition)
    {
        _baseAnchoredPosition = anchoredPosition;
        ApplyPosition();
    }

    // Purpose: Updates the pair offset state.
    public void SetPairOffset(Vector2 pairOffset)
    {
        _pairOffset = pairOffset;
        ApplyPosition();
    }

    // Purpose: Updates the stack offset state.
    public void SetStackOffset(Vector2 stackOffset, float alphaMultiplier)
    {
        _stackOffset = stackOffset;
        _stackAlphaMultiplier = Mathf.Clamp01(alphaMultiplier);
        ApplyPosition();
    }

    // Purpose: Updates the amount state.
    public void SetAmount(
        float amount,
        string numberFormat,
        Sprite icon,
        Color textColor,
        Color iconColor)
    {
        if (amount <= 0f)
            return;

        if (_amountText != null)
        {
            string format = string.IsNullOrWhiteSpace(numberFormat)
                ? "{0:0.0}"
                : numberFormat;

            _amountText.text = string.Format(format, amount);
            _amountText.color = textColor;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.color = iconColor;
            _iconImage.enabled = icon != null;
        }
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        ApplyPosition();
        ApplyScale();

        SetAlpha(ResolveAlpha(t) * _stackAlphaMultiplier);

        if (t >= 1f)
            gameObject.SetActive(false);
    }

    // Purpose: Applies the requested position change.
    private void ApplyPosition()
    {
        if (_rectTransform == null)
            return;

        float t = Mathf.Clamp01(_elapsed / _duration);
        Vector2 lift = Vector2.Lerp(Vector2.zero, _floatOffset, t);
        _rectTransform.anchoredPosition =
            _baseAnchoredPosition + _pairOffset + _stackOffset + lift;
    }

    // Purpose: Applies the requested scale change.
    private void ApplyScale()
    {
        if (_elapsed >= _popDuration)
        {
            transform.localScale = _baseScale;
            return;
        }

        float t = Mathf.Clamp01(_elapsed / _popDuration);
        float scale = Mathf.Lerp(_popScale, 1f, t);
        transform.localScale = _baseScale * scale;
    }

    // Purpose: Resolves the alpha needed by this component.
    private float ResolveAlpha(float normalizedTime)
    {
        if (normalizedTime <= _fadeStartNormalized)
            return 1f;

        float fadeLength = Mathf.Max(0.001f, 1f - _fadeStartNormalized);
        float fadeT = Mathf.Clamp01((normalizedTime - _fadeStartNormalized) / fadeLength);
        return 1f - fadeT;
    }

    // Purpose: Clears the current entry state.
    private void ClearEntry()
    {
        if (_amountText != null)
            _amountText.text = string.Empty;

        if (_iconImage != null)
            _iconImage.enabled = false;
    }

    // Purpose: Updates the alpha state.
    private void SetAlpha(float alpha)
    {
        if (_group != null)
            _group.alpha = Mathf.Clamp01(alpha);
    }

    // Purpose: Ensures refs is ready before the workflow continues.
    private void EnsureRefs()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_group == null)
            _group = GetComponent<CanvasGroup>();

        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();
    }
}
