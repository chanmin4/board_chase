/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Implements the focused EnemyMovementStatsProvider responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        ResolveEnemyStatConfig();
    }

    // Purpose: Updates the enemy stat config state.
    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();
    }

    // Purpose: Resolves the enemy stat config needed by this component.
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
