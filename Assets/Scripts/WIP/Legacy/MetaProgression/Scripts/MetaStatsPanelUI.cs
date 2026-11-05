// WIP legacy compatibility only. Active growth uses quest rewards and experience, not this system.
/*
 * Role hierarchy: Presentation > UI > Gameplay > MetaSystem
 * Primary responsibility: Presents MetaStatsPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
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
        new MetaSummaryRowDefinition { enabled = true, displayName = "Health Damage Coef.", statId = MetaSummaryStatId.AttackDamage, valueFormat = "x{0:0.##}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Magazine", statId = MetaSummaryStatId.MagazineSize, valueFormat = "{0:0}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Move Speed", statId = MetaSummaryStatId.MoveSpeed, valueFormat = "{0:0.##}" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Reload", statId = MetaSummaryStatId.ReloadDurationSeconds, valueFormat = "{0:0.##}s" },
        new MetaSummaryRowDefinition { enabled = true, displayName = "Shot Speed", statId = MetaSummaryStatId.ShotsPerSecond, valueFormat = "{0:0.##}/s" }
    };

    private readonly List<PlayerStatsSummaryRowUI> _rows = new();
    private readonly Dictionary<PlayerStatId, StatAccumulator> _accumulators = new();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        enabled=false; // Serialization-only compatibility component; current stats come from PlayerStatsRuntime.
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        enabled=false;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_progressChangedChannel != null)
            _progressChangedChannel.OnEventRaised -= HandleMetaProgressChanged;
    }

    // Purpose: Refreshes the displayed or cached rows.
    public void RefreshRows()
    {
        if(!enabled)return;
        if (_rows.Count == 0)
            BuildRows();

        RebuildAccumulators();

        int rowIndex = 0;

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            MetaSummaryRowDefinition definition = _rowDefinitions[i];

            if (!definition.enabled || !IsCharacterBaseRow(definition.statId))
                continue;

            if (rowIndex >= _rows.Count || _rows[rowIndex] == null)
            {
                rowIndex++;
                continue;
            }

            _rows[rowIndex].Bind(
                null,
                definition.displayName,
                ResolveValue(definition));
            rowIndex++;
        }
    }

    // Purpose: Handles the incoming meta progress changed request or event.
    private void HandleMetaProgressChanged(MetaProgressSnapshot snapshot)
    {
        RefreshRows();
    }

    // Purpose: Builds the rows from the current authored data.
    private void BuildRows()
    {
        if (_rowRoot == null || _rowPrefab == null || _rowDefinitions == null)
            return;

        _rows.Clear();

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            if (!_rowDefinitions[i].enabled || !IsCharacterBaseRow(_rowDefinitions[i].statId))
                continue;

            PlayerStatsSummaryRowUI row = Instantiate(_rowPrefab, _rowRoot);
            _rows.Add(row);
        }
    }

    // Purpose: Repairs or rebuilds accumulators from the current source data.
    private void RebuildAccumulators()
    {
        _accumulators.Clear();

        if (_catalog == null || _saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        // WIP: do not read retired meta levels into a live UI calculation.
    }

    // Purpose: Resolves the value needed by this component.
    private string ResolveValue(MetaSummaryRowDefinition definition)
    {
        PlayerStatsConfigSO config = _playerStatsConfig;

        float value = definition.statId switch
        {
            MetaSummaryStatId.MaxHealth => Resolve(PlayerStatId.MaxHealth, config != null ? config.MaxHealth : 100f, 1f),
            MetaSummaryStatId.AttackDamage => 0f, // Old meta weapon rows are hidden; use the equipped runtime stats.
            MetaSummaryStatId.MagazineSize => 0f, // WIP compatibility; magazine is weapon-owned.
            MetaSummaryStatId.MoveSpeed => Resolve(PlayerStatId.MoveSpeed, config != null ? config.MoveSpeed : 8f, 0f),
            MetaSummaryStatId.ReloadDurationSeconds => 0f,
            MetaSummaryStatId.ShotsPerSecond => 0f,
            _ => 0f
        };

        return FormatNumber(definition.valueFormat, value);
    }

    private static bool IsCharacterBaseRow(MetaSummaryStatId id) =>
        id == MetaSummaryStatId.MaxHealth || id == MetaSummaryStatId.MoveSpeed;

    // Purpose: Applies the requested modifier change.
    private void ApplyModifier(PlayerStatModifier modifier)
    {
        if (!ShooterStatPolicy.IsCharacterStat(modifier.stat)) return;
        _accumulators.TryGetValue(modifier.stat, out StatAccumulator accumulator);
        accumulator.Apply(modifier);
        _accumulators[modifier.stat] = accumulator;
    }

    // Purpose: Resolves the resolve needed by this component.
    private float Resolve(PlayerStatId stat, float baseValue, float minValue)
    {
        if (!_accumulators.TryGetValue(stat, out StatAccumulator accumulator))
            return Mathf.Max(minValue, baseValue);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    // Purpose: Performs the format number workflow for this component.
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
        // Purpose: Applies the requested apply change.
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

        // Purpose: Resolves the resolve needed by this component.
        public float Resolve(float baseValue)
        {
            if (_hasOverride)
                return _overrideValue;

            return (baseValue + _flatAdd) * (1f + _percentAdd * 0.01f);
        }
    }
}
