using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MetaStatsPanelUI : MonoBehaviour
{
    private enum MetaSummaryStatId
    {
        MaxHealth,
        AttackDamage,
        MagazineSize,
        MoveSpeed,
        ReloadDurationSeconds,
        ShotsPerSecond
    }

    [Serializable]
    private struct MetaSummaryRowDefinition
    {
        public bool enabled;
        public string displayName;
        public MetaSummaryStatId statId;
        public string valueFormat;
    }

    [Header("Refs")]
    [SerializeField] private PlayerStatsConfigSO _playerStatsConfig;
    [SerializeField] private MetaUpgradeCatalogSO _catalog;
    [SerializeField] private SaveSystem _saveSystem;

    [Header("UI")]
    [SerializeField] private Transform _rowRoot;
    [SerializeField] private PlayerStatsSummaryRowUI _rowPrefab;

    [Header("Events")]
    [SerializeField] private MetaProgressChangedEventChannelSO _progressChangedChannel;

    [Header("Rows")]
    [SerializeField] private MetaSummaryRowDefinition[] _rowDefinitions =
    {
        new MetaSummaryRowDefinition { enabled = true, displayName = "Max HP", statId = MetaSummaryStatId.MaxHealth, valueFormat = "{0:0}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Attack Damage", statId = MetaSummaryStatId.AttackDamage, valueFormat = "{0:0.##}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Magazine", statId = MetaSummaryStatId.MagazineSize, valueFormat = "{0:0}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Move Speed", statId = MetaSummaryStatId.MoveSpeed, valueFormat = "{0:0.##}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Reload", statId = MetaSummaryStatId.ReloadDurationSeconds, valueFormat = "{0:0.##}s" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Shot Speed", statId = MetaSummaryStatId.ShotsPerSecond, valueFormat = "{0:0.##}/s" }
    };

    private readonly List<PlayerStatsSummaryRowUI> _rows = new();
    private readonly Dictionary<PlayerStatId, StatAccumulator> _accumulators = new();

    private void Awake()
    {
        BuildRows();
    }

    private void OnEnable()
    {
        if (_progressChangedChannel != null)
            _progressChangedChannel.OnEventRaised += HandleMetaProgressChanged;

        RefreshRows();
    }

    private void OnDisable()
    {
        if (_progressChangedChannel != null)
            _progressChangedChannel.OnEventRaised -= HandleMetaProgressChanged;
    }

    public void RefreshRows()
    {
        if (_rows.Count == 0)
            BuildRows();

        RebuildAccumulators();

        int rowIndex = 0;

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            MetaSummaryRowDefinition definition = _rowDefinitions[i];

            if (!definition.enabled)
                continue;

            if (rowIndex >= _rows.Count || _rows[rowIndex] == null)
            {
                rowIndex++;
                continue;
            }

            _rows[rowIndex].Bind(definition.displayName, ResolveValue(definition));
            rowIndex++;
        }
    }

    private void HandleMetaProgressChanged(MetaProgressSnapshot snapshot)
    {
        RefreshRows();
    }

    private void BuildRows()
    {
        if (_rowRoot == null || _rowPrefab == null || _rowDefinitions == null)
            return;

        _rows.Clear();

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            if (!_rowDefinitions[i].enabled)
                continue;

            PlayerStatsSummaryRowUI row = Instantiate(_rowPrefab, _rowRoot);
            _rows.Add(row);
        }
    }

    private void RebuildAccumulators()
    {
        _accumulators.Clear();

        if (_catalog == null || _saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        _catalog.ApplyMetaModifiers(_saveSystem.saveData.MetaUpgrades, ApplyModifier);
    }

    private string ResolveValue(MetaSummaryRowDefinition definition)
    {
        PlayerStatsConfigSO config = _playerStatsConfig;

        float value = definition.statId switch
        {
            MetaSummaryStatId.MaxHealth => Resolve(PlayerStatId.MaxHealth, config != null ? config.MaxHealth : 100f, 1f),
            MetaSummaryStatId.AttackDamage => Resolve(PlayerStatId.AttackDamage, config != null ? config.AttackDamage : 10f, 0f),
            MetaSummaryStatId.MagazineSize => Mathf.RoundToInt(Resolve(PlayerStatId.MagazineSize, config != null ? config.MagazineSize : 6, 1f)),
            MetaSummaryStatId.MoveSpeed => Resolve(PlayerStatId.MoveSpeed, config != null ? config.MoveSpeed : 8f, 0f),
            MetaSummaryStatId.ReloadDurationSeconds => Resolve(PlayerStatId.ReloadDurationSeconds, config != null ? config.ReloadDurationSeconds : 1.2f, 0.01f),
            MetaSummaryStatId.ShotsPerSecond => Resolve(PlayerStatId.ShotsPerSecond, config != null ? config.ShotsPerSecond : 2f, 0.01f),
            _ => 0f
        };

        return FormatNumber(definition.valueFormat, value);
    }

    private void ApplyModifier(PlayerStatModifier modifier)
    {
        _accumulators.TryGetValue(modifier.stat, out StatAccumulator accumulator);
        accumulator.Apply(modifier);
        _accumulators[modifier.stat] = accumulator;
    }

    private float Resolve(PlayerStatId stat, float baseValue, float minValue)
    {
        if (!_accumulators.TryGetValue(stat, out StatAccumulator accumulator))
            return Mathf.Max(minValue, baseValue);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    private static string FormatNumber(string format, float value)
    {
        if (string.IsNullOrWhiteSpace(format))
            format = "{0:0.##}";

        return string.Format(format, value);
    }

    private struct StatAccumulator
    {
        private float _flatAdd;
        private float _percentAdd;
        private bool _hasOverride;
        private float _overrideValue;
        private float _percentMultiply;
        public void Apply(PlayerStatModifier modifier)
        {
            switch (modifier.type)
            {
                case StatModifierType.FlatAdd:
                    _flatAdd += modifier.value;
                    break;

                case StatModifierType.PercentAdd:
                    _percentAdd += modifier.value;
                    break;
                case StatModifierType.PercentMultiply:
                    _percentMultiply *= modifier.value;
                    break;
                case StatModifierType.Override:
                    _hasOverride = true;
                    _overrideValue = modifier.value;
                    break;
            }
        }

        public float Resolve(float baseValue)
        {
            if (_hasOverride)
                return _overrideValue;

            return (baseValue + _flatAdd) * (1f + _percentAdd * 0.01f);
        }
    }
}
