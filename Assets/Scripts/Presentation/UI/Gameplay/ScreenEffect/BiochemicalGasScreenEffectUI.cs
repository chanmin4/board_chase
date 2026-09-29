/*
 * Role hierarchy: Presentation > UI > Gameplay > ScreenEffect
 * Primary responsibility: Presents the current room's biochemical gas pressure
 * without changing gameplay. The ordinary low-HP effect remains independent.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the dedicated FS_InfectionGas full-screen material from the raw room
/// gas pressure. It intentionally ignores gas-mask mitigation: a strong room
/// still looks dangerous even when the mask successfully protects Infection.
/// </summary>
[DisallowMultipleComponent]
public sealed class BiochemicalGasScreenEffectUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;

    [Header("Full-Screen Materials")]
    [Tooltip("FS_InfectionGas must also be assigned to an active Full Screen Pass Renderer Feature.")]
    [SerializeField] private List<Material> _materials = new();
    [SerializeField] private string _enabledPropertyName = "_enabled";
    [SerializeField] private string _alphaPropertyName = "_Alpha";

    [Header("Gas Pressure")]
    [Tooltip("The raw room gas Infection per second that reaches the strongest screen effect. This is deliberately measured before gas-mask mitigation.")]
    [SerializeField, Min(0.01f)] private float _maxRawGasInfectionPerSecond = 17.5f;

    [Header("Visual")]
    [Tooltip("Visible baseline as soon as any room gas is active. A value near 0.8 makes even +1/s pressure immediately readable.")]
    [SerializeField, Range(0f, 1f)] private float _minimumActiveAlpha = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _maximumAlpha = 1f;
    [SerializeField, Min(0.01f)] private float _fadeSpeed = 4f;
    [SerializeField, Range(0f, 0.2f)] private float _pulseAmplitude = 0.02f;
    [SerializeField, Min(0f)] private float _pulseFrequency = 1.35f;

    [Header("Runtime Debug")]
    [SerializeField] private float _rawGasInfectionPerSecond;
    [SerializeField] private float _appliedGasInfectionPerSecond;
    [SerializeField] private bool _isFilterActive;
    [SerializeField, Range(0f, 1f)] private float _visualPressure01;
    [SerializeField] private float _targetAlpha;
    [SerializeField] private float _currentAlpha;

    private int _enabledPropertyId;
    private int _alphaPropertyId;

    /// <summary>
    /// The current raw environmental gas pressure used by LowHealthScreenEffectUI
    /// to preserve gas as the dominant visual when both effects are active.
    /// </summary>
    public static float CurrentVisualPressure01 { get; private set; }

    // Purpose: Caches dependencies and begins fully transparent.
    private void Awake()
    {
        CachePropertyIds();
        ResolveRefs();
        ForceOff();
    }

    // Purpose: Keeps the presentation in sync with the active room's gas pressure.
    private void Update()
    {
        ResolveRefs();

        _rawGasInfectionPerSecond =
            SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond;

        _isFilterActive = _inventoryRuntime != null &&
            _inventoryRuntime.HasUsableGasMask;

        _appliedGasInfectionPerSecond = _inventoryRuntime != null
            ? _inventoryRuntime.PreviewBiochemicalGasExposure(_rawGasInfectionPerSecond)
            : _rawGasInfectionPerSecond;

        _visualPressure01 = CalculateVisualPressure01(_rawGasInfectionPerSecond);
        CurrentVisualPressure01 = _visualPressure01;
        _targetAlpha = CalculateTargetAlpha(_visualPressure01);
        _currentAlpha = Mathf.Lerp(
            _currentAlpha,
            _targetAlpha,
            1f - Mathf.Exp(-_fadeSpeed * Time.unscaledDeltaTime));

        ApplyMaterialAlpha(_currentAlpha);
    }

    // Purpose: Clears the overlay when this UI object is disabled.
    private void OnDisable()
    {
        ForceOff();
    }

    // Purpose: Converts raw room gas pressure into a normalized visual intensity.
    private float CalculateVisualPressure01(float rawGasPerSecond)
    {
        if (rawGasPerSecond <= 0.001f)
            return 0f;

        return Mathf.Clamp01(rawGasPerSecond / _maxRawGasInfectionPerSecond);
    }

    // Purpose: Calculates material alpha from the normalized gas pressure.
    private float CalculateTargetAlpha(float pressure01)
    {
        if (pressure01 <= 0.001f)
            return 0f;

        float alpha = Mathf.Lerp(_minimumActiveAlpha, _maximumAlpha, pressure01);

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * _pulseFrequency) * _pulseAmplitude;
        return Mathf.Clamp01(alpha * pulse);
    }

    // Purpose: Resolves scene references safely after scene loads or script reloads.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
        {
            _inventoryRuntime = FindFirstObjectByType<PlayerInventoryRuntime>(
                FindObjectsInactive.Exclude);
        }
    }

    // Purpose: Clears every assigned full-screen material and the shared overlap state.
    private void ForceOff()
    {
        _rawGasInfectionPerSecond = 0f;
        _appliedGasInfectionPerSecond = 0f;
        _visualPressure01 = 0f;
        CurrentVisualPressure01 = 0f;
        _targetAlpha = 0f;
        _currentAlpha = 0f;
        ApplyMaterialAlpha(0f);
    }

    // Purpose: Applies gas intensity to the material used by the renderer feature.
    private void ApplyMaterialAlpha(float alpha)
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

    // Purpose: Caches shader property ids once for repeated material updates.
    private void CachePropertyIds()
    {
        _enabledPropertyId = Shader.PropertyToID(_enabledPropertyName);
        _alphaPropertyId = Shader.PropertyToID(_alphaPropertyName);
    }
}
