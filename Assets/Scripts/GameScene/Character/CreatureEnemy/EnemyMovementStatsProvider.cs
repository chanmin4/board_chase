using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class EnemyMovementStatsProvider : MonoBehaviour
{
    [Header("Enemy Stat Config")]
    [FormerlySerializedAs("_movementStats")]
    [SerializeField] private CreatureEnemyStatConfigSO _enemyStatConfig;

    [Header("Runtime Source")]
    [SerializeField] private Damageable _damageable;

    public CreatureEnemyStatConfigSO EnemyStatConfig => ResolveEnemyStatConfig();

    public float NormalMovementSpeed =>
        EnemyStatConfig != null ? EnemyStatConfig.NormalMovementSpeed : 2.2f;

    public float PlayerChaseMovementSpeed =>
        EnemyStatConfig != null ? EnemyStatConfig.PlayerChaseMovementSpeed : 3.5f;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        ResolveEnemyStatConfig();
    }

    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();
    }

    private CreatureEnemyStatConfigSO ResolveEnemyStatConfig()
    {
        if (_enemyStatConfig != null)
            return _enemyStatConfig;

        ResolveRefs();

        _enemyStatConfig = _damageable != null
            ? _damageable.StatConfig as CreatureEnemyStatConfigSO
            : null;

        return _enemyStatConfig;
    }
}
