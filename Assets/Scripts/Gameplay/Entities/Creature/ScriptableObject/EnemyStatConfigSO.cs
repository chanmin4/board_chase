/*
 * Role hierarchy: Gameplay > Entities > Creature > ScriptableObject
 * Primary responsibility: Defines the EnemyStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public abstract class EnemyStatConfigSO : EntityStatConfigSO
{
    [Header("Enemy")]
    [SerializeField] private EnemyCommonSettings _enemy = new();

    public abstract Enemy EnemyPrefab { get; }
    public bool IsValid => EnemyPrefab != null;

    public string Id => _enemy.Id;
    public string DisplayName => _enemy.DisplayName;
    public DroppableRewardConfigSO DroppableRewardConfig => _enemy.DroppableRewardConfig;
    public EnemyScreenSpaceWidgetLayout HealthBarUI => _enemy.HealthBarUI;
    public EnemyScreenSpaceWidgetLayout InfectionCastBarUI => _enemy.InfectionCastBarUI;
}
