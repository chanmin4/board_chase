/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Handles EnemyContactDamage combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Hitboxes")]
    [SerializeField] private EnemyContactAttack[] _hitboxes;

    [Header("Enemy Stat Config")]
    [FormerlySerializedAs("_settings")]
    [SerializeField] private CreatureEnemyStatConfigSO _enemyStatConfig;

    private EnemyMovementStatsProvider _movementStatsProvider;
    private float _nextHitTime;

    // Purpose: Updates the enemy stat config state.
    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_hitboxes == null || _hitboxes.Length == 0)
            _hitboxes = GetComponentsInChildren<EnemyContactAttack>(true);
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CacheMovementStatsProvider();
        RegisterHitboxes();
    }

    // Purpose: Registers hitboxes with its owning runtime service.
    private void RegisterHitboxes()
    {
        if (_hitboxes == null)
            return;

        for (int i = 0; i < _hitboxes.Length; i++)
        {
            if (_hitboxes[i] != null)
                _hitboxes[i].Initialize(this);
        }
    }

    // Purpose: Attempts to damage and reports whether the request succeeded.
    public void TryDamage(Collider other)
    {
        CreatureEnemyStatConfigSO config = ResolveStatConfig();

        if (config == null || Time.time < _nextHitTime)
            return;

        PlayerCharacter playerCharacter = other.GetComponentInParent<PlayerCharacter>();
        if (playerCharacter == null)
            return;

        Damageable damageable = playerCharacter.GetComponent<Damageable>();
        if (damageable == null || damageable.gameObject == gameObject || !damageable.CanReceiveDamage)
            return;

        PlayerShooterInfection playerInfection = playerCharacter.GetComponent<PlayerShooterInfection>();
        bool appliedAny = false;

        if (config.ContactHealthDamage > 0f)
        {
            damageable.ReceiveAnAttack(config.ContactHealthDamage, gameObject);
            appliedAny = true;
        }

        if (playerInfection != null && config.ContactInfectionDamage > 0f)
        {
            playerInfection.AddInfection(config.ContactInfectionDamage);
            appliedAny = true;
        }

        if (appliedAny)
            _nextHitTime = Time.time + config.ContactHitCooldown;
    }

    // Purpose: Resolves the stat config needed by this component.
    private CreatureEnemyStatConfigSO ResolveStatConfig()
    {
        if (_enemyStatConfig != null)
            return _enemyStatConfig;

        if (_movementStatsProvider == null)
            CacheMovementStatsProvider();

        return _movementStatsProvider != null
            ? _movementStatsProvider.EnemyStatConfig
            : null;
    }

    // Purpose: Caches movement stats provider for later reuse.
    private void CacheMovementStatsProvider()
    {
        _movementStatsProvider =
            GetComponent<EnemyMovementStatsProvider>() ??
            GetComponentInParent<EnemyMovementStatsProvider>() ??
            GetComponentInChildren<EnemyMovementStatsProvider>(true);
    }
}
