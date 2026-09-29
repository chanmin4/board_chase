/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused EnemyCommonSettings responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class EnemyCommonSettings
{
    [Header("Identity")]
    [SerializeField] private string _id = "enemy_common";
    [SerializeField] private string _displayName = "Enemy";

    [Header("WIP Legacy Numeric Currency")]
    [Tooltip("Retained only to read old enemy assets. Enemy rewards now come from loot tables and physical CurrencyItemSO drops.")]
    [FormerlySerializedAs("_runCurrencyOnDeath")]
    [SerializeField, HideInInspector] private int _wipLegacyRunCurrencyOnDeath;

    [Header("Drops")]
    [SerializeField] private DroppableRewardConfigSO _droppableRewardConfig;

    [Header("Enemy Screen Space UI")]
    [SerializeField] private EnemyScreenSpaceWidgetLayout _healthBarUI =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 50f));

    [SerializeField] private EnemyScreenSpaceWidgetLayout _infectionCastBarUI =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 40f));

    public string Id => _id;
    public string DisplayName => _displayName;
    public DroppableRewardConfigSO DroppableRewardConfig => _droppableRewardConfig;
    public EnemyScreenSpaceWidgetLayout HealthBarUI => _healthBarUI;
    public EnemyScreenSpaceWidgetLayout InfectionCastBarUI => _infectionCastBarUI;
}
