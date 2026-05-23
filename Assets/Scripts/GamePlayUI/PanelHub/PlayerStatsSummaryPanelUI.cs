using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player stats summary panel.
/// Creates stat rows from inspector definitions and refreshes them from:
/// - PlayerUpgradeUISnapshot: upgrade points / upgrade track levels
/// - PlayerStatsSnapshot: runtime combat, paint, movement, survival stats
///
/// Intended layout:
/// PlayerStatsSummaryPanelUI
///   RowRoot
///     PlayerStatsSummaryRowUI instances
/// </summary>
[DisallowMultipleComponent]
public class PlayerStatsSummaryPanelUI : MonoBehaviour
{
    private enum SummaryStatId
    {
        UnspentUpgradePoints,
        RemovalUpgradeLevel,
        OccupationUpgradeLevel,
        ControlUpgradeLevel,

        AttackDamage,
        NamedBossDamageMultiplier,
        MaxRange,
        AttackShotsPerSecond,
        PaintShotsPerSecond,
        ReloadSpeedMultiplier,
        ReloadDurationSeconds,
        MagazineSize,

        PaintRadius,
        PaintPriority,
        OccupationWinThreshold,

        MoveSpeed,
        DashCooldownSeconds,
        DashDistanceMultiplier,

        MaxHealth,

        ShockwaveCooldownSeconds,

        LeaveVaccineOnEnemyKill,
        PaintBulletLeavesTrail,
        ShockwavePaintsVaccine
    }

    [Serializable]
    private struct SummaryRowDefinition
    {
        [Tooltip("If false, this row is ignored.")]
        public bool enabled;

        [Tooltip("Left-side stat name shown in the panel.")]
        public string displayName;

        [Tooltip("Which player stat this row displays.")]
        public SummaryStatId statId;

        [Tooltip("Format for numeric values. Examples: {0:0}, {0:0.##}, x{0:0.00}, {0:0}%")]
        public string valueFormat;
    }

    [Header("Refs")]
    [Tooltip("Parent transform where stat row prefabs will be instantiated.")]
    [SerializeField] private Transform _rowRoot;

    [Tooltip("Single row prefab. It should have PlayerStatsSummaryRowUI.")]
    [SerializeField] private PlayerStatsSummaryRowUI _rowPrefab;

    [Header("Listening")]
    [Tooltip("Optional. Runtime player stats event. Keeps this panel updated when stats change.")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;


    [Header("Rows")]
    [SerializeField] private SummaryRowDefinition[] _rowDefinitions =
    {
        new SummaryRowDefinition { enabled = true, displayName = "Points", statId = SummaryStatId.UnspentUpgradePoints, valueFormat = "{0:0}" },
        new SummaryRowDefinition { enabled = true, displayName = "Removal Lv", statId = SummaryStatId.RemovalUpgradeLevel, valueFormat = "{0}" },
        new SummaryRowDefinition { enabled = true, displayName = "Occupation Lv", statId = SummaryStatId.OccupationUpgradeLevel, valueFormat = "{0}" },
        new SummaryRowDefinition { enabled = true, displayName = "Control Lv", statId = SummaryStatId.ControlUpgradeLevel, valueFormat = "{0}" },

        new SummaryRowDefinition { enabled = true, displayName = "Attack Damage", statId = SummaryStatId.AttackDamage, valueFormat = "{0:0.##}" },
        new SummaryRowDefinition { enabled = true, displayName = "Boss Damage", statId = SummaryStatId.NamedBossDamageMultiplier, valueFormat = "x{0:0.00}" },
        new SummaryRowDefinition { enabled = true, displayName = "Range", statId = SummaryStatId.MaxRange, valueFormat = "{0:0.##}" },
        new SummaryRowDefinition { enabled = true, displayName = "Attack Speed", statId = SummaryStatId.AttackShotsPerSecond, valueFormat = "{0:0.##}/s" },
        new SummaryRowDefinition { enabled = true, displayName = "Paint Speed", statId = SummaryStatId.PaintShotsPerSecond, valueFormat = "{0:0.##}/s" },
        new SummaryRowDefinition { enabled = true, displayName = "Reload", statId = SummaryStatId.ReloadDurationSeconds, valueFormat = "{0:0.##}s" },
        new SummaryRowDefinition { enabled = true, displayName = "Magazine", statId = SummaryStatId.MagazineSize, valueFormat = "{0:0}" },

        new SummaryRowDefinition { enabled = true, displayName = "Paint Radius", statId = SummaryStatId.PaintRadius, valueFormat = "{0:0.##}" },
        new SummaryRowDefinition { enabled = true, displayName = "Occupation", statId = SummaryStatId.OccupationWinThreshold, valueFormat = "{0:0}%" },

        new SummaryRowDefinition { enabled = true, displayName = "Move Speed", statId = SummaryStatId.MoveSpeed, valueFormat = "{0:0.##}" },
        new SummaryRowDefinition { enabled = true, displayName = "Dash Cooldown", statId = SummaryStatId.DashCooldownSeconds, valueFormat = "{0:0.##}s" },
        new SummaryRowDefinition { enabled = true, displayName = "Dash Distance", statId = SummaryStatId.DashDistanceMultiplier, valueFormat = "x{0:0.00}" },

        new SummaryRowDefinition { enabled = true, displayName = "Max HP", statId = SummaryStatId.MaxHealth, valueFormat = "{0:0}" },
        new SummaryRowDefinition { enabled = true, displayName = "Shockwave CD", statId = SummaryStatId.ShockwaveCooldownSeconds, valueFormat = "{0:0.##}s" }
    };

    private readonly List<PlayerStatsSummaryRowUI> _rows = new();

    private PlayerUpgradeUISnapshot _latestUpgradeSnapshot;
    private PlayerStatsSnapshot _latestStatsSnapshot;
    private bool _hasUpgradeSnapshot;
    private bool _hasStatsSnapshot;

    private void Awake()
    {
        BuildRows();
    }

    private void OnEnable()
    {
        if (_statsChangedChannel != null)
        {
            _statsChangedChannel.OnEventRaised += HandleStatsChanged;

            if (_statsChangedChannel.HasCurrent)
            {
                _latestStatsSnapshot = _statsChangedChannel.Current;
                _hasStatsSnapshot = true;
            }
        }

        RefreshRows();
    }

    private void OnDisable()
    {
        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised -= HandleStatsChanged;
    }

    public void Bind(PlayerUpgradeUISnapshot snapshot)
    {
        _latestUpgradeSnapshot = snapshot;
        _hasUpgradeSnapshot = true;
        RefreshRows();
    }

    public void Bind(PlayerStatsSnapshot snapshot)
    {
        _latestStatsSnapshot = snapshot;
        _hasStatsSnapshot = true;
        RefreshRows();
    }

    private void HandleStatsChanged(PlayerStatsSnapshot snapshot)
    {
        Bind(snapshot);
    }

    private void BuildRows()
    {
        _rows.Clear();

        if (_rowRoot == null || _rowPrefab == null || _rowDefinitions == null)
            return;

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            if (!_rowDefinitions[i].enabled)
                continue;

            PlayerStatsSummaryRowUI row = Instantiate(_rowPrefab, _rowRoot);
            _rows.Add(row);
        }
    }

    private void RefreshRows()
    {
        if (_rows.Count == 0)
            BuildRows();

        if (_rowDefinitions == null)
            return;

        int rowIndex = 0;

        for (int i = 0; i < _rowDefinitions.Length; i++)
        {
            SummaryRowDefinition definition = _rowDefinitions[i];

            if (!definition.enabled)
                continue;

            if (rowIndex >= _rows.Count || _rows[rowIndex] == null)
            {
                rowIndex++;
                continue;
            }

            string value = ResolveValue(definition);
            _rows[rowIndex].Bind(definition.displayName, value);

            rowIndex++;
        }
    }

    private string ResolveValue(SummaryRowDefinition definition)
    {
        switch (definition.statId)
        {
            case SummaryStatId.UnspentUpgradePoints:
                return _hasUpgradeSnapshot
                    ? FormatNumber(definition, _latestUpgradeSnapshot.unspentPoints)
                    : "-";

            case SummaryStatId.RemovalUpgradeLevel:
                return _hasUpgradeSnapshot ? FormatUpgradeLevel(_latestUpgradeSnapshot.removal) : "-";

            case SummaryStatId.OccupationUpgradeLevel:
                return _hasUpgradeSnapshot ? FormatUpgradeLevel(_latestUpgradeSnapshot.occupation) : "-";

            case SummaryStatId.ControlUpgradeLevel:
                return _hasUpgradeSnapshot ? FormatUpgradeLevel(_latestUpgradeSnapshot.control) : "-";
        }

        if (!_hasStatsSnapshot)
            return "-";

        switch (definition.statId)
        {
            case SummaryStatId.AttackDamage:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.attackDamage);

            case SummaryStatId.NamedBossDamageMultiplier:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.namedBossDamageMultiplier);

            case SummaryStatId.MaxRange:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.maxRange);

            case SummaryStatId.AttackShotsPerSecond:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.attackShotsPerSecond);

            case SummaryStatId.PaintShotsPerSecond:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.paintShotsPerSecond);

            case SummaryStatId.ReloadSpeedMultiplier:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.reloadSpeedMultiplier);

            case SummaryStatId.ReloadDurationSeconds:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.reloadDurationSeconds);

            case SummaryStatId.MagazineSize:
                return FormatNumber(definition, _latestStatsSnapshot.weapon.magazineSize);

            case SummaryStatId.PaintRadius:
                return FormatNumber(definition, _latestStatsSnapshot.paint.paintRadius);

            case SummaryStatId.PaintPriority:
                return FormatNumber(definition, _latestStatsSnapshot.paint.paintPriority);

            case SummaryStatId.OccupationWinThreshold:
                return FormatNumber(definition, _latestStatsSnapshot.paint.occupationWinThreshold * 100f);

            case SummaryStatId.MoveSpeed:
                return FormatNumber(definition, _latestStatsSnapshot.movement.moveSpeed);

            case SummaryStatId.DashCooldownSeconds:
                return FormatNumber(definition, _latestStatsSnapshot.movement.dashCooldownSeconds);

            case SummaryStatId.DashDistanceMultiplier:
                return FormatNumber(definition, _latestStatsSnapshot.movement.dashDistanceMultiplier);

            case SummaryStatId.MaxHealth:
                return FormatNumber(definition, _latestStatsSnapshot.survival.maxHealth);

            case SummaryStatId.ShockwaveCooldownSeconds:
                return FormatNumber(definition, _latestStatsSnapshot.shockwave.cooldownSeconds);

            case SummaryStatId.LeaveVaccineOnEnemyKill:
                return FormatBool(_latestStatsSnapshot.features.leaveVaccineOnEnemyKill);

            case SummaryStatId.PaintBulletLeavesTrail:
                return FormatBool(_latestStatsSnapshot.features.paintBulletLeavesTrail);

            case SummaryStatId.ShockwavePaintsVaccine:
                return FormatBool(_latestStatsSnapshot.features.shockwavePaintsVaccine);

            default:
                return "-";
        }
    }

    private static string FormatUpgradeLevel(PlayerUpgradeTrackViewData data)
    {
        return $"{data.currentLevel} / {data.maxLevel}";
    }

    private static string FormatNumber(SummaryRowDefinition definition, float value)
    {
        string format = string.IsNullOrWhiteSpace(definition.valueFormat)
            ? "{0:0.##}"
            : definition.valueFormat;

        return string.Format(format, value);
    }

    private static string FormatBool(bool value)
    {
        return value ? "ON" : "OFF";
    }
}