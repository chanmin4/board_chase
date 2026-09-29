/*
 * Role hierarchy: Presentation > UI > Gameplay > ScreenEffect
 * Primary responsibility: Presents LowHealthScreenEffectUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LowHealthScreenEffectUI : MonoBehaviour
{
    [Header("Need Ref - Events")]
    [Tooltip("Player health snapshot event. This effect uses HP only; room gas has its own FS_InfectionGas material.")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;

    [Tooltip("Optional refresh request. If raised, this script reads the latest PlayerHealthSnapshot.")]
    [SerializeField] private VoidEventChannelSO _requestRefreshHealthUI;

    [Header("Need Ref - Fullscreen Materials")]
    [SerializeField] private List<Material> _materials = new List<Material>();

    [Header("Shader Properties")]
    [SerializeField] private string _enabledPropertyName = "_enabled";
    [SerializeField] private string _alphaPropertyName = "_Alpha";

    [Header("Health Danger")]
    [SerializeField, Range(0f, 1f)] private float _healthWarning01 = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _healthCritical01 = 0.05f;

    [Header("Visual")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;
    [SerializeField, Min(0.01f)] private float _fadeSpeed = 6f;

    [Header("Gas Visual Priority")]
    [Tooltip("At maximum external gas pressure, Low HP remains visible at this fraction so the gas effect stays the primary pressure cue.")]
    [SerializeField, Range(0f, 1f)] private float _lowHealthVisibilityAtMaximumGas = 0.3f;

    [Header("Options")]
    [SerializeField] private bool _disableEffectOnDisable = true;

    [Header("Runtime Debug")]
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentHealth01 = 1f;
    [SerializeField, Range(0f, 1f)] private float _gasVisualPressure01;
    [SerializeField] private float _dangerScore01;
    [SerializeField] private float _targetAlpha;
    [SerializeField] private float _currentAlpha;

    private int _enabledPropertyId;
    private int _alphaPropertyId;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CachePropertyIds();
        ForceOff();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        CachePropertyIds();
        ForceOff();

        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised += HandleHealthChanged;

        if (_requestRefreshHealthUI != null)
            _requestRefreshHealthUI.OnEventRaised += RefreshFromCurrentSnapshot;

        RefreshFromCurrentSnapshot();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised -= HandleHealthChanged;

        if (_requestRefreshHealthUI != null)
            _requestRefreshHealthUI.OnEventRaised -= RefreshFromCurrentSnapshot;

        if (_disableEffectOnDisable)
            ForceOff();
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        CachePropertyIds();

        _healthCritical01 = Mathf.Min(_healthCritical01, _healthWarning01);
        _healthWarning01 = Mathf.Max(_healthWarning01, _healthCritical01);

        if (!Application.isPlaying)
            ForceOff();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        RefreshGasPriority();
        _currentAlpha = Mathf.Lerp(
            _currentAlpha,
            _targetAlpha,
            1f - Mathf.Exp(-_fadeSpeed * Time.unscaledDeltaTime));

        ApplyAlpha(_currentAlpha);
    }

    // Purpose: Handles the incoming health changed request or event.
    private void HandleHealthChanged(PlayerHealthSnapshot snapshot)
    {
        _maxHealth = snapshot.maxHealth;
        _currentHealth = snapshot.currentHealth;
        RecalculateDanger();
    }

    // Purpose: Refreshes the displayed or cached from current snapshot.
    private void RefreshFromCurrentSnapshot()
    {
        if (_playerHealthChanged == null || !_playerHealthChanged.HasCurrent)
            return;

        HandleHealthChanged(_playerHealthChanged.Current);
    }

    // Purpose: Performs the recalculate danger workflow for this component.
    private void RecalculateDanger()
    {
        _currentHealth01 = _maxHealth > 0f
            ? Mathf.Clamp01(_currentHealth / _maxHealth)
            : 0f;

        float healthDanger01 = CalculateLowHealthDanger01();
        _dangerScore01 = healthDanger01;
        RefreshGasPriority();
    }

    // Purpose: Calculates low health danger01 from the current inputs.
    private float CalculateLowHealthDanger01()
    {
        if (_currentHealth01 > _healthWarning01)
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(
            _healthWarning01,
            _healthCritical01,
            _currentHealth01));
    }

    // Purpose: Re-evaluates only the visual overlap rule while room gas changes independently of HP events.
    private void RefreshGasPriority()
    {
        _gasVisualPressure01 = BiochemicalGasScreenEffectUI.CurrentVisualPressure01;
        float lowHealthVisibility = Mathf.Lerp(
            1f,
            _lowHealthVisibilityAtMaximumGas,
            _gasVisualPressure01);
        _targetAlpha = _dangerScore01 * _maxAlpha * lowHealthVisibility;
    }

    // Purpose: Performs the force off workflow for this component.
    private void ForceOff()
    {
        _dangerScore01 = 0f;
        _targetAlpha = 0f;
        _currentAlpha = 0f;
        ApplyAlpha(0f);
    }

    // Purpose: Applies the requested alpha change.
    private void ApplyAlpha(float alpha)
    {
        if (_materials == null)
            return;

        float clampedAlpha = Mathf.Clamp01(alpha);
        int enabledValue = clampedAlpha > 0.001f ? 1 : 0;

        for (int i = 0; i < _materials.Count; i++)
        {
            Material material = _materials[i];

            if (material == null)
                continue;

            material.SetInt(_enabledPropertyId, enabledValue);
            material.SetFloat(_alphaPropertyId, clampedAlpha);
        }
    }

    // Purpose: Caches property ids for later reuse.
    private void CachePropertyIds()
    {
        _enabledPropertyId = Shader.PropertyToID(_enabledPropertyName);
        _alphaPropertyId = Shader.PropertyToID(_alphaPropertyName);
    }
}
