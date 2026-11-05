/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Presents PlayerStatsSummaryPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permanent player progression summary panel.
/// It deliberately excludes weapon, ammunition, passive-item, and temporary
/// status values. Those remain visible on their respective item/detail UIs.
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
        MaxHealthBonus = 0,
        AttackDamageBonus = 1,
        FireRateBonus = 2,
        ReloadSpeedBonus = 3,
        MoveSpeedBonus = 4,
        CurrentArmorClass = 5,
        MeleeDamage = 6,
        GasDefenseLevel = 7,
    }

    [Serializable]
    private struct SummaryRowDefinition
    {
        [Tooltip("If false, this row is ignored.")]
        public bool enabled;

        [Tooltip("Left-side stat name shown in the panel.")]
        public string displayName;

        [Tooltip("Optional icon shown before the stat name. Leave empty to use the row prefab's placeholder icon.")]
        public Sprite icon;

        [Tooltip("Which player stat this row displays.")]
        public SummaryStatId statId;

        [Tooltip("Format for numeric values. Permanent percentage rows normally use {0:+0;-0;0}%.")]
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

    [Tooltip("Optional player runtime. When unassigned, it is found at runtime.")]
    [SerializeField] private PlayerStatsRuntime _playerStatsRuntime;

    [Tooltip("Optional player Damageable. Used only to show the final armor class including equipped armor and temporary effects.")]
    [SerializeField] private Damageable _playerDamageable;

    [Tooltip("Optional player status runtime. Keeps final Armor Class current while a temporary armor effect changes.")]
    [SerializeField] private EntityStatusEffectRuntime _playerStatusEffects;


    [Header("Rows")]
    [SerializeField] private SummaryRowDefinition[] _rowDefinitions =
    {
        new SummaryRowDefinition { enabled = true, displayName = "MAX HP", statId = SummaryStatId.MaxHealthBonus, valueFormat = "{0:+0;-0;0}%" },
        new SummaryRowDefinition { enabled = true, displayName = "DAMAGE", statId = SummaryStatId.AttackDamageBonus, valueFormat = "{0:+0;-0;0}%" },
        new SummaryRowDefinition { enabled = true, displayName = "FIRE RATE", statId = SummaryStatId.FireRateBonus, valueFormat = "{0:+0;-0;0}%" },
        new SummaryRowDefinition { enabled = true, displayName = "RELOAD SPEED", statId = SummaryStatId.ReloadSpeedBonus, valueFormat = "{0:+0;-0;0}%" },
        new SummaryRowDefinition { enabled = true, displayName = "MOVE SPEED", statId = SummaryStatId.MoveSpeedBonus, valueFormat = "{0:+0;-0;0}%" },
        new SummaryRowDefinition { enabled = true, displayName = "ARMOR CLASS", statId = SummaryStatId.CurrentArmorClass, valueFormat = "{0:0.#}" },
        new SummaryRowDefinition { enabled = true, displayName = "MELEE DAMAGE", statId = SummaryStatId.MeleeDamage, valueFormat = "{0:0.#}" },
    };

    private readonly List<PlayerStatsSummaryRowUI> _rows = new();

    private PlayerStatsSnapshot _latestStatsSnapshot;
    private bool _hasStatsSnapshot;
    private bool _statusEffectsSubscribed;
    private PlayerInventoryRuntime _inventoryRuntime;
    private int _lastGasDefense = -1;
    private float _nextGasRefresh;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_rowDefinitions == null) _rowDefinitions = Array.Empty<SummaryRowDefinition>();
        if (!Array.Exists(_rowDefinitions, row => row.statId == SummaryStatId.GasDefenseLevel))
        {
            Array.Resize(ref _rowDefinitions, _rowDefinitions.Length + 1);
            _rowDefinitions[_rowDefinitions.Length - 1] = new SummaryRowDefinition
            { enabled = true, displayName = "가스방어력", statId = SummaryStatId.GasDefenseLevel, valueFormat = "{0:0}" };
        }
        BuildRows();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextGasRefresh) return;
        _nextGasRefresh = Time.unscaledTime + 0.25f;
        ResolvePlayerRuntimeReferences();
        int defense = _inventoryRuntime != null ? _inventoryRuntime.GasDefenseLevel : 0;
        if (defense != _lastGasDefense) { _lastGasDefense = defense; RefreshRows(); }
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolvePlayerRuntimeReferences();
        SubscribeStatusEffectChanges();

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

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised -= HandleStatsChanged;

        UnsubscribeStatusEffectChanges();
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(PlayerStatsSnapshot snapshot)
    {
        _latestStatsSnapshot = snapshot;
        _hasStatsSnapshot = true;
        RefreshRows();
    }

    // Purpose: Handles the incoming stats changed request or event.
    private void HandleStatsChanged(PlayerStatsSnapshot snapshot)
    {
        Bind(snapshot);
    }

    // Purpose: Builds the rows from the current authored data.
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

    // Purpose: Refreshes the displayed or cached rows.
    private void RefreshRows()
    {
        ResolvePlayerRuntimeReferences();

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
            _rows[rowIndex].Bind(definition.icon, definition.displayName, value);

            rowIndex++;
        }
    }

    // Purpose: Resolves the value needed by this component.
    private string ResolveValue(SummaryRowDefinition definition)
    {
        switch (definition.statId)
        {
            case SummaryStatId.GasDefenseLevel:
                return FormatNumber(definition, _inventoryRuntime != null ? _inventoryRuntime.GasDefenseLevel : 0);
            case SummaryStatId.MaxHealthBonus:
                return FormatNumber(definition, GetQuestPercentAdd(PlayerStatId.MaxHealth));

            case SummaryStatId.AttackDamageBonus:
                return FormatNumber(definition, GetQuestPercentAdd(PlayerStatId.AttackDamage));

            case SummaryStatId.FireRateBonus:
                return FormatNumber(definition, GetQuestPercentAdd(PlayerStatId.ShotsPerSecond));

            case SummaryStatId.ReloadSpeedBonus:
                return FormatNumber(definition, -GetQuestPercentAdd(PlayerStatId.ReloadDurationSeconds));

            case SummaryStatId.MoveSpeedBonus:
                return FormatNumber(definition, GetQuestPercentAdd(PlayerStatId.MoveSpeed));

            case SummaryStatId.CurrentArmorClass:
                return ResolveArmorClassValue();

            case SummaryStatId.MeleeDamage:
                return FormatNumber(definition, _playerStatsRuntime != null
                    ? _playerStatsRuntime.MeleeDamage : _latestStatsSnapshot.weapon.meleeDamage);


            default:
                return "-";
        }
    }

    // Purpose: Finds the active player sources without requiring scene-specific inspector wiring.
    private void ResolvePlayerRuntimeReferences()
    {
        if (_playerStatsRuntime == null)
            _playerStatsRuntime = FindFirstObjectByType<PlayerStatsRuntime>();
        if (_inventoryRuntime == null && _playerStatsRuntime != null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(_playerStatsRuntime);

        if (_playerDamageable == null && _playerStatsRuntime != null)
            _playerDamageable = EntityComponentResolver.Resolve<Damageable>(_playerStatsRuntime);

        if (_playerStatusEffects == null && _playerStatsRuntime != null)
            _playerStatusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(_playerStatsRuntime);

        if (isActiveAndEnabled)
            SubscribeStatusEffectChanges();
    }

    // Purpose: Subscribes to temporary armor changes while this summary panel is visible.
    private void SubscribeStatusEffectChanges()
    {
        if (_statusEffectsSubscribed || _playerStatusEffects == null)
            return;

        _playerStatusEffects.OnChanged += HandlePlayerStatusEffectsChanged;
        _statusEffectsSubscribed = true;
    }

    // Purpose: Releases the temporary armor-change listener while hidden.
    private void UnsubscribeStatusEffectChanges()
    {
        if (!_statusEffectsSubscribed)
            return;

        if (_playerStatusEffects != null)
            _playerStatusEffects.OnChanged -= HandlePlayerStatusEffectsChanged;

        _statusEffectsSubscribed = false;
    }

    // Purpose: Refreshes final armor class after a player status effect changes.
    private void HandlePlayerStatusEffectsChanged(EntityStatusEffectRuntime runtime)
    {
        RefreshRows();
    }

    // Purpose: Returns the permanent quest percentage for one player stat.
    private float GetQuestPercentAdd(PlayerStatId stat)
    {
        return _playerStatsRuntime != null
            ? _playerStatsRuntime.GetPersistentQuestPercentAdd(stat)
            : 0f;
    }

    // Purpose: Formats final current armor with its permanent quest contribution.
    private string ResolveArmorClassValue()
    {
        float currentArmorClass = _playerDamageable != null
            ? _playerDamageable.EffectiveArmorClass
            : _hasStatsSnapshot ? _latestStatsSnapshot.survival.armorClass : 0f;
        float questArmorBonus = _playerStatsRuntime != null
            ? _playerStatsRuntime.GetPersistentQuestFlatAdd(PlayerStatId.ArmorClass)
            : 0f;

        return $"{Mathf.Max(0f, currentArmorClass):0.#} ({questArmorBonus:+0.#;-0.#;0} Quest)";
    }

    // Purpose: Performs the format number workflow for this component.
    private static string FormatNumber(SummaryRowDefinition definition, float value)
    {
        string format = string.IsNullOrWhiteSpace(definition.valueFormat)
            ? "{0:0.##}"
            : definition.valueFormat;

        return string.Format(format, value);
    }

}
