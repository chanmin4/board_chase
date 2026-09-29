/*
 * Role hierarchy: Presentation > UI > HUD > World > EnemyHealthUI
 * Primary responsibility: Presents EnemyHealthBarUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_target != null)
            _target.OnHealthChanged += OnHealthChanged;

        RefreshImmediate();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_target != null)
            _target.OnHealthChanged -= OnHealthChanged;
    }

    // Purpose: Processes this component's per-frame runtime work.
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

    // Purpose: Responds to the health changed callback.
    private void OnHealthChanged(Damageable damageable)
    {
        RefreshImmediate();
        _emphasisUntilTime = Time.time + _emphasisHoldSeconds;
    }

    // Purpose: Refreshes the displayed or cached immediate.
    private void RefreshImmediate()
    {
        if (_target == null || _fillImage == null)
            return;

        _fillImage.fillAmount = _target.HealthNormalized;
    }

    // Purpose: Updates the target state.
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
