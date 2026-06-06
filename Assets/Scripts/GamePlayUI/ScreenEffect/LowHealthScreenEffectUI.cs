using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LowHealthScreenEffectUI : MonoBehaviour
{
    [Header("Need Ref - Events")]
    [Tooltip("Player health snapshot event. Uses current HP and current Infection to calculate danger.")]
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

    [Header("Infection Danger")]
    [SerializeField, Range(0f, 1f)] private float _infectionWarningOfCurrentHealth01 = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _infectionCriticalOfCurrentHealth01 = 1f;

    [Header("Visual")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;
    [SerializeField, Min(0.01f)] private float _fadeSpeed = 6f;

    [Header("Options")]
    [SerializeField] private bool _disableEffectOnDisable = true;

    [Header("Runtime Debug")]
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentInfection;
    [SerializeField] private float _currentHealth01 = 1f;
    [SerializeField] private float _infectionOfCurrentHealth01;
    [SerializeField] private float _dangerScore01;
    [SerializeField] private float _targetAlpha;
    [SerializeField] private float _currentAlpha;

    private int _enabledPropertyId;
    private int _alphaPropertyId;

    private void Awake()
    {
        CachePropertyIds();
        ForceOff();
    }

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

    private void OnDisable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised -= HandleHealthChanged;

        if (_requestRefreshHealthUI != null)
            _requestRefreshHealthUI.OnEventRaised -= RefreshFromCurrentSnapshot;

        if (_disableEffectOnDisable)
            ForceOff();
    }

    private void OnValidate()
    {
        CachePropertyIds();

        _healthCritical01 = Mathf.Min(_healthCritical01, _healthWarning01);
        _healthWarning01 = Mathf.Max(_healthWarning01, _healthCritical01);

        _infectionCriticalOfCurrentHealth01 = Mathf.Max(
            _infectionCriticalOfCurrentHealth01,
            _infectionWarningOfCurrentHealth01);

        _infectionWarningOfCurrentHealth01 = Mathf.Min(
            _infectionWarningOfCurrentHealth01,
            _infectionCriticalOfCurrentHealth01);

        if (!Application.isPlaying)
            ForceOff();
    }

    private void Update()
    {
        _currentAlpha = Mathf.Lerp(
            _currentAlpha,
            _targetAlpha,
            1f - Mathf.Exp(-_fadeSpeed * Time.unscaledDeltaTime));

        ApplyAlpha(_currentAlpha);
    }

    private void HandleHealthChanged(PlayerHealthSnapshot snapshot)
    {
        _maxHealth = snapshot.maxHealth;
        _currentHealth = snapshot.currentHealth;
        _currentInfection = snapshot.currentInfection;

        RecalculateDanger();
    }

    private void RefreshFromCurrentSnapshot()
    {
        if (_playerHealthChanged == null || !_playerHealthChanged.HasCurrent)
            return;

        HandleHealthChanged(_playerHealthChanged.Current);
    }

    private void RecalculateDanger()
    {
        _currentHealth01 = _maxHealth > 0f
            ? Mathf.Clamp01(_currentHealth / _maxHealth)
            : 0f;

        _infectionOfCurrentHealth01 = _currentHealth > 0f
            ? Mathf.Clamp01(_currentInfection / _currentHealth)
            : 1f;

        float healthDanger01 = CalculateLowHealthDanger01();
        float infectionDanger01 = CalculateInfectionDanger01();

        _dangerScore01 = Mathf.Max(healthDanger01, infectionDanger01);
        _targetAlpha = _dangerScore01 * _maxAlpha;
    }

    private float CalculateLowHealthDanger01()
    {
        if (_currentHealth01 > _healthWarning01)
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(
            _healthWarning01,
            _healthCritical01,
            _currentHealth01));
    }

    private float CalculateInfectionDanger01()
    {
        if (_infectionOfCurrentHealth01 < _infectionWarningOfCurrentHealth01)
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(
            _infectionWarningOfCurrentHealth01,
            _infectionCriticalOfCurrentHealth01,
            _infectionOfCurrentHealth01));
    }

    private void ForceOff()
    {
        _dangerScore01 = 0f;
        _targetAlpha = 0f;
        _currentAlpha = 0f;
        ApplyAlpha(0f);
    }

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

    private void CachePropertyIds()
    {
        _enabledPropertyId = Shader.PropertyToID(_enabledPropertyName);
        _alphaPropertyId = Shader.PropertyToID(_alphaPropertyName);
    }
}