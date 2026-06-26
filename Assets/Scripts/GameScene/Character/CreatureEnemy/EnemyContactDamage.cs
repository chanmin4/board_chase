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

    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    private void Reset()
    {
        if (_hitboxes == null || _hitboxes.Length == 0)
            _hitboxes = GetComponentsInChildren<EnemyContactAttack>(true);
    }

    private void Awake()
    {
        CacheMovementStatsProvider();
        RegisterHitboxes();
    }

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

    public void TryDamage(Collider other)
    {
        CreatureEnemyStatConfigSO config = ResolveStatConfig();

        if (config == null || Time.time < _nextHitTime)
            return;

        VSplatter_Character playerCharacter = other.GetComponentInParent<VSplatter_Character>();
        if (playerCharacter == null)
            return;

        Damageable damageable = playerCharacter.GetComponent<Damageable>();
        if (damageable == null || damageable.gameObject == gameObject || !damageable.CanReceiveDamage)
            return;

        PlayerInfection playerInfection = playerCharacter.GetComponent<PlayerInfection>();
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

    private void CacheMovementStatsProvider()
    {
        _movementStatsProvider =
            GetComponent<EnemyMovementStatsProvider>() ??
            GetComponentInParent<EnemyMovementStatsProvider>() ??
            GetComponentInChildren<EnemyMovementStatsProvider>(true);
    }
}
