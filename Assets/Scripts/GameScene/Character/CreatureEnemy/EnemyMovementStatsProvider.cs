using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class EnemyMovementStatsProvider : MonoBehaviour
{
    [Header("Enemy Stat Config")]
    [FormerlySerializedAs("_movementStats")]
    [SerializeField] private CreatureEnemyStatConfigSO _enemyStatConfig;

    public CreatureEnemyStatConfigSO EnemyStatConfig => _enemyStatConfig;

    public float NormalMovementSpeed =>
        _enemyStatConfig != null ? _enemyStatConfig.NormalMovementSpeed : 2.2f;

    public float PlayerChaseMovementSpeed =>
        _enemyStatConfig != null ? _enemyStatConfig.PlayerChaseMovementSpeed : 3.5f;

    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }
}
