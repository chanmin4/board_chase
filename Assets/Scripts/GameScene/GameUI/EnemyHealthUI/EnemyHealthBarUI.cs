using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _target;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasGroupOpacity;

    [Header("Options")]
    [SerializeField] private bool _hideWhenFull = false;
    [SerializeField] private float _emphasisHoldSeconds = 1.2f;

    private float _emphasisUntilTime;

    private void OnEnable()
    {
        if (_target != null)
            _target.OnHealthChanged += OnHealthChanged;

        RefreshImmediate();
    }

    private void OnDisable()
    {
        if (_target != null)
            _target.OnHealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (_uicanvasGroupOpacity == null)
            return;

        if (Time.time <= _emphasisUntilTime)
        {
            _uicanvasGroupOpacity.SetEmphasis();
            return;
        }

        bool shouldHide = _hideWhenFull && _target != null && Mathf.Approximately(_target.HealthNormalized, 1f);
        if (shouldHide)
            _uicanvasGroupOpacity.Hide();
        else
            _uicanvasGroupOpacity.SetDefault();
    }

    private void OnHealthChanged(Damageable damageable)
    {
        RefreshImmediate();
        _emphasisUntilTime = Time.time + _emphasisHoldSeconds;
    }

    private void RefreshImmediate()
    {
        if (_target == null || _fillImage == null)
            return;

        _fillImage.fillAmount = _target.HealthNormalized;
    }

    public void SetTarget(Damageable target)
    {
        if (_target != null)
            _target.OnHealthChanged -= OnHealthChanged;

        _target = target;

        if (_target != null)
            _target.OnHealthChanged += OnHealthChanged;

        RefreshImmediate();
    }
}
