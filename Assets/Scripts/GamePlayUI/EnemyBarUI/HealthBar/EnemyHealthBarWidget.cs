using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class EnemyHealthBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasgroupopacity;
    [Header("Multiplier")]
    [SerializeField] private TextMeshProUGUI _multiplierText;
    [SerializeField] private string _multiplierFormat = "x{0:0.00}";
    private EnemyScreenSpaceHPUIAnchor _anchor;
    private Damageable _damageable;
    private EnemyHealthBarSettingsSO _settings;
    private float _emphasisUntilTime;
    private float _fillFullWidth;
    private bool _managerVisible = true;
    public RectTransform Root => _root != null ? _root : (RectTransform)transform;

    private void Awake()
    {
        if (_root == null)
            _root = (RectTransform)transform;

        if (_fillRect == null && _fillImage != null)
            _fillRect = _fillImage.rectTransform;

        if (_fillRect != null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;

            // 왼쪽부터 줄어들도록 강제
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
    }

    public void Bind(EnemyScreenSpaceHPUIAnchor anchor)
    {
        Unbind();

        _anchor = anchor;
        _damageable = anchor != null ? anchor.Damageable : null;
        _settings = anchor != null ? anchor.HealthBarSettings : null;

       if (_damageable != null)
        {
            _damageable.OnHealthChanged += OnHealthChanged;
            _damageable.OnDamageMultiplierChanged += OnDamageMultiplierChanged;
        }
        EnsureFillImageMode();
        RefreshImmediate();
    }

    public void Unbind()
    {
        if (_damageable != null)
        {
            _damageable.OnHealthChanged -= OnHealthChanged;
            _damageable.OnDamageMultiplierChanged -= OnDamageMultiplierChanged;
        }
        _anchor = null;
        _damageable = null;
        _settings = null;
    }
    private void OnDamageMultiplierChanged(Damageable damageable)
    {
        RefreshImmediate();
    }

    public void TickVisualState()
    {
        if (_damageable == null || _uicanvasgroupopacity == null)
            return;

        RefreshImmediate();

        if (!_managerVisible)
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        if (_settings != null && _settings.HideWhenDead && _damageable.IsDead)
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        if (Time.time <= _emphasisUntilTime)
        {
            _uicanvasgroupopacity.SetEmphasis();
            return;
        }

        if (_settings != null && _settings.HideWhenFull && Mathf.Approximately(_damageable.HealthNormalized, 1f))
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        _uicanvasgroupopacity.SetDefault();
    }

    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition;
    }

    private void OnHealthChanged(Damageable damageable)
    {
        RefreshImmediate();

        if (_settings != null)
            _emphasisUntilTime = Time.time + _settings.EmphasisDuration;
    }

    private void RefreshImmediate()
    {
        if (_damageable == null)
            return;

        float normalized = _damageable.HealthNormalized;

        if (_fillImage != null)
        {
            EnsureFillImageMode();
            _fillImage.fillAmount = normalized;
        }

        if (_fillRect != null)
        {
            Vector2 size = _fillRect.sizeDelta;
            size.x = _fillFullWidth * normalized;
            _fillRect.sizeDelta = size;
        }
        if (_multiplierText != null)
            _multiplierText.text = string.Format(_multiplierFormat, _damageable.DamageTakenMultiplier);
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void EnsureFillImageMode()
    {
        if (_fillImage == null)
            return;

        if (_fillImage.type != Image.Type.Filled)
            _fillImage.type = Image.Type.Filled;

        if (_fillImage.fillMethod != Image.FillMethod.Horizontal)
            _fillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_fillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
    public void SetManagerVisible(bool visible)
    {
        _managerVisible = visible;

        if (_uicanvasgroupopacity == null)
            return;

        if (!visible)
            _uicanvasgroupopacity.Hide();
    }
}
