using UnityEngine;

[DisallowMultipleComponent]
public class EnemyKillRewardSource : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EnemyMovementStatsProvider _movementStatsProvider;

    [Header("Enemy Stat Config")]
    [SerializeField] private EnemyStatConfigSO _enemyStatConfig;

    [Header("XP Event")]
    [SerializeField] private PlayerExperienceGainEventChannelSO _xpGainChannel;

    [Header("Currency Runtime")]
    [SerializeField] private PlayerCurrencyRuntimeReadyEventChannelSO _currencyRuntimeReadyChannel;

    private PlayerCurrencyRuntime _currencyRuntime;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_movementStatsProvider == null)
            _movementStatsProvider = GetComponent<EnemyMovementStatsProvider>();
    }

    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_movementStatsProvider == null)
            _movementStatsProvider = GetComponent<EnemyMovementStatsProvider>();
    }

    private void OnEnable()
    {
        if (_damageable != null)
            _damageable.OnDie += OnDie;

        if (_currencyRuntimeReadyChannel != null)
        {
            _currencyRuntimeReadyChannel.OnEventRaised += OnCurrencyRuntimeReady;

            if (_currencyRuntimeReadyChannel.Current != null)
                OnCurrencyRuntimeReady(_currencyRuntimeReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnDie -= OnDie;

        if (_currencyRuntimeReadyChannel != null)
            _currencyRuntimeReadyChannel.OnEventRaised -= OnCurrencyRuntimeReady;

        _currencyRuntime = null;
    }

    public void SetEnemyStatConfig(EnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    private void OnCurrencyRuntimeReady(PlayerCurrencyRuntime currencyRuntime)
    {
        _currencyRuntime = currencyRuntime;
    }

    private void OnDie()
    {
        EnemyStatConfigSO config = ResolveEnemyStatConfig();

        if (config == null)
            return;

        GrantExperience(config);
        GrantRunCurrency(config);
    }

    private void GrantExperience(EnemyStatConfigSO config)
    {
        if (_xpGainChannel == null)
            return;

        float xp = config.XpOnDeath;

        if (xp <= 0f)
            return;

        _xpGainChannel.RaiseEvent(new PlayerExperienceGain(
            xp,
            PlayerExperienceSource.EnemyKill,
            transform.position,
            gameObject));
    }

    private void GrantRunCurrency(EnemyStatConfigSO config)
    {
        int amount = config.RunCurrencyOnDeath;

        if (amount <= 0)
            return;

        if (_currencyRuntime == null && _currencyRuntimeReadyChannel != null)
            _currencyRuntime = _currencyRuntimeReadyChannel.Current;

        if (_currencyRuntime == null)
            return;

        _currencyRuntime.AddCurrency(PlayerCurrencyType.Run, amount);
    }

    private EnemyStatConfigSO ResolveEnemyStatConfig()
    {
        if (_enemyStatConfig != null)
            return _enemyStatConfig;

        if (_movementStatsProvider != null)
            return _movementStatsProvider.EnemyStatConfig;

        return null;
    }
}