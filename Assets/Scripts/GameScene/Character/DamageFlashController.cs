using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class DamageFlashController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent;
    [SerializeField] private Renderer[] _targetRenderers;

    [Header("Options")]
    [SerializeField] private bool _enableFlash = false;
    [SerializeField, Range(0f, 1f)] private float _defaultFlashAlpha = 0.5f;

    [Header("Shader")]
    [SerializeField] private string _primaryColorProperty = "_BaseColor";
    [SerializeField] private string[] _fallbackColorProperties =
    {
        "_MainColor",
        "_Color"
    };

    private Material[] _materials;
    private int[] _colorPropertyIds;
    private Color[] _baseColors;
    private float _remainingTime;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();

        if ((_targetRenderers == null || _targetRenderers.Length == 0) &&
            _damageable != null &&
            _damageable.MainMeshRenderer != null)
        {
            _targetRenderers = new[] { _damageable.MainMeshRenderer };
        }
    }

    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();

        CacheMaterials();
        RestoreBaseColors();
    }

    private void OnEnable()
    {
        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += HandleHitReceived;
    }

    private void OnDisable()
    {
        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= HandleHitReceived;

        RestoreBaseColors();
        _remainingTime = 0f;
    }

    private void Update()
    {
        if (!_enableFlash)
        {
            if (_remainingTime > 0f)
            {
                _remainingTime = 0f;
                RestoreBaseColors();
            }

            return;
        }

        if (_remainingTime <= 0f || _damageable == null || _damageable.GetHitEffectConfig == null)
            return;

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);

        float duration = Mathf.Max(0.01f, _damageable.GetHitEffectConfig.GetHitFlashingDuration);
        float t = 1f - (_remainingTime / duration);
        float alpha = 1f - t;

        ApplyFlash(alpha);

        if (_remainingTime <= 0f)
            RestoreBaseColors();
    }

    private void HandleHitReceived(GameObject hitTarget)
    {
        if (!_enableFlash)
            return;

        if (_damageable == null || hitTarget != _damageable.gameObject)
            return;

        if (_damageable.GetHitEffectConfig == null)
            return;

        if (_materials == null || _materials.Length == 0)
            CacheMaterials();

        _remainingTime = Mathf.Max(0.01f, _damageable.GetHitEffectConfig.GetHitFlashingDuration);
        ApplyFlash(1f);
    }

    private void CacheMaterials()
    {
        List<Material> materials = new();
        List<int> colorPropertyIds = new();
        List<Color> baseColors = new();

        AddRendererMaterials(ResolveTargetRenderers(), materials, colorPropertyIds, baseColors);

        if (materials.Count == 0)
        {
            Renderer[] fallbackRenderers = _damageable != null
                ? _damageable.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);

            AddRendererMaterials(fallbackRenderers, materials, colorPropertyIds, baseColors);
        }

        _materials = materials.ToArray();
        _colorPropertyIds = colorPropertyIds.ToArray();
        _baseColors = baseColors.ToArray();
    }

    private Renderer[] ResolveTargetRenderers()
    {
        if (_targetRenderers != null && _targetRenderers.Length > 0)
            return _targetRenderers;

        if (_damageable != null && _damageable.MainMeshRenderer != null)
            return new[] { _damageable.MainMeshRenderer };

        if (_damageable != null)
            return _damageable.GetComponentsInChildren<Renderer>(true);

        return GetComponentsInChildren<Renderer>(true);
    }

    private void AddRendererMaterials(
        Renderer[] renderers,
        List<Material> materials,
        List<int> colorPropertyIds,
        List<Color> baseColors)
    {
        if (renderers == null || renderers.Length == 0)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Material[] mats = renderer.materials;
            for (int j = 0; j < mats.Length; j++)
            {
                Material mat = mats[j];
                if (mat == null)
                    continue;

                if (!TryResolveColorProperty(mat, out int colorPropertyId))
                    continue;

                materials.Add(mat);
                colorPropertyIds.Add(colorPropertyId);
                baseColors.Add(mat.GetColor(colorPropertyId));
            }
        }
    }

    private bool TryResolveColorProperty(Material material, out int colorPropertyId)
    {
        colorPropertyId = -1;

        if (!string.IsNullOrWhiteSpace(_primaryColorProperty) &&
            material.HasProperty(_primaryColorProperty))
        {
            colorPropertyId = Shader.PropertyToID(_primaryColorProperty);
            return true;
        }

        if (_fallbackColorProperties == null)
            return false;

        for (int i = 0; i < _fallbackColorProperties.Length; i++)
        {
            string propertyName = _fallbackColorProperties[i];
            if (string.IsNullOrWhiteSpace(propertyName))
                continue;

            if (!material.HasProperty(propertyName))
                continue;

            colorPropertyId = Shader.PropertyToID(propertyName);
            return true;
        }

        return false;
    }

    private void ApplyFlash(float alpha01)
    {
        if (_damageable == null || _damageable.GetHitEffectConfig == null)
            return;

        Color flashColor = _damageable.GetHitEffectConfig.GetHitFlashingColor;
        float flashAlpha = flashColor.a > 0.001f
            ? flashColor.a
            : _defaultFlashAlpha;
        float blend = Mathf.Clamp01(flashAlpha) * Mathf.Clamp01(alpha01);

        for (int i = 0; i < _materials.Length; i++)
        {
            Material material = _materials[i];
            if (material == null)
                continue;

            Color baseColor = _baseColors[i];
            Color targetColor = new Color(
                flashColor.r,
                flashColor.g,
                flashColor.b,
                baseColor.a);

            material.SetColor(
                _colorPropertyIds[i],
                Color.Lerp(baseColor, targetColor, blend));
        }
    }

    private void RestoreBaseColors()
    {
        if (_materials == null || _baseColors == null)
            return;

        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] == null)
                continue;

            _materials[i].SetColor(_colorPropertyIds[i], _baseColors[i]);
        }
    }
}
