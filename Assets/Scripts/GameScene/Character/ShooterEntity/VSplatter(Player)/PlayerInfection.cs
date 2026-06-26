using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerInfection : MonoBehaviour
{
    private const float DefaultZoneTickInterval = 0.2f;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    [Header("Rules")]
    [SerializeField] private PlayerInfectionRulesSO _infectionRules;
    [SerializeField] private DifficultyRulesSO _difficultyRules;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;
    [FormerlySerializedAs("_deathEvent")]
    [SerializeField] private VoidEventChannelSO _gameOverEvent;
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;

    [Header("Runtime")]
    [SerializeField] private float _currentInfection;

    private bool _isDead;

    public float ZoneTickInterval =>
        _infectionRules != null ? _infectionRules.ZoneTickInterval : DefaultZoneTickInterval;

    public float CurrentInfection => _currentInfection;
    public bool IsDead => _isDead;

    public float MaxHealth => _damageable != null ? _damageable.MaxHealth : 0f;
    public float CurrentHealth => _damageable != null ? _damageable.CurrentHealth : 0f;

    private float InfectionGainMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionGainMultiplier : 1f;

    private float InfectionRecoverMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionRecoverMultiplier : 1f;

    private DifficultyRulesSO ActiveDifficultyRules =>
        _difficultyRules != null ? _difficultyRules : DifficultyRuntime.CurrentRules;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
    }

    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        ClampInfectionToMaxHealth();
    }

    private void OnEnable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged += HandleDamageableHealthChanged;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.RaiseEvent(this);

        PublishCurrentSnapshot();
    }

    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= HandleDamageableHealthChanged;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.Clear(this);
    }

    public void ApplyVirusZoneTick()
    {
        if (_infectionRules == null)
            return;

        AddInfection(_infectionRules.VirusZoneInfectionGainPerTick * InfectionGainMultiplier);
    }

    public void ApplyVaccineZoneTick()
    {
        if (_infectionRules == null)
            return;

        ReduceInfection(_infectionRules.VaccineZoneRecoverPerTick * InfectionRecoverMultiplier);
    }

    public void RecoverOnSectorCaptured()
    {
        if (_infectionRules == null)
            return;

        ReduceInfection(_infectionRules.RecoverOnSectorCaptured * InfectionRecoverMultiplier);
    }

    public void RecoverOnNamedKilled()
    {
        if (_infectionRules == null)
            return;

        ReduceInfection(_infectionRules.RecoverOnNamedKilled * InfectionRecoverMultiplier);
    }

    public void RecoverOnBossKilled()
    {
        if (_infectionRules == null)
            return;

        ReduceInfection(_infectionRules.RecoverOnBossKilled * InfectionRecoverMultiplier);
    }

    public void AddInfection(float amount)
    {
        if (_isDead || amount <= 0f)
            return;

        _currentInfection += amount;
        ClampInfectionToMaxHealth();

        CheckInfectionDeath();
        PublishCurrentSnapshot();
    }

    public void ReduceInfection(float amount)
    {
        if (_isDead || amount <= 0f)
            return;

        _currentInfection -= amount;
        ClampInfectionToMaxHealth();

        PublishCurrentSnapshot();
    }

    public void ClearInfection()
    {
        _currentInfection = 0f;
        PublishCurrentSnapshot();
    }

    public void PublishCurrentSnapshot()
    {
        if (_playerHealthChanged == null)
            return;

        _playerHealthChanged.RaiseEvent(new PlayerHealthSnapshot(
            MaxHealth,
            CurrentHealth,
            _currentInfection,
            _isDead));
    }

    private void HandleDamageableHealthChanged(Damageable damageable)
    {
        ClampInfectionToMaxHealth();
        CheckInfectionDeath();
        PublishCurrentSnapshot();
    }

    private void ClampInfectionToMaxHealth()
    {
        _currentInfection = Mathf.Clamp(_currentInfection, 0f, MaxHealth);
    }

    private void CheckInfectionDeath()
    {
        if (_isDead)
            return;

        if (CurrentHealth <= 0f)
            return;

        if (_currentInfection < CurrentHealth)
            return;

        _isDead = true;

        if (_damageable != null)
            _damageable.IsDead = true;

        if (_gameOverEvent != null)
            _gameOverEvent.RaiseEvent();

        PublishCurrentSnapshot();
    }
}