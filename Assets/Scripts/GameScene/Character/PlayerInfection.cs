using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInfection : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HealthSO _healthSO;
    [SerializeField] private Damageable _damageable;

    [Header("Rules")]
    [SerializeField] private PlayerInfectionRulesSO _infectionRules;
    [SerializeField] private DifficultyRulesSO _difficultyRules;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;
    [SerializeField] private VoidEventChannelSO _deathEvent;
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;

    [Header("Listening To")]
    [SerializeField] private VoidEventChannelSO _updateHealthUI;

    [Header("Runtime")]
    [SerializeField] private float _currentInfection;

    private bool _isDead;
    public float PoisonPuddleHealthDamagePerSecond =>
        _infectionRules != null ? _infectionRules.PoisonPuddleHealthDamagePerSecond : 0f;
    public float CurrentInfection => _currentInfection;
    public bool IsDead => _isDead;

    private float MaxHealth
    {
        get
        {
            if (_damageable != null)
                return _damageable.MaxHealth;

            return _healthSO != null ? _healthSO.MaxHealth : 0f;
        }
    }

    private float CurrentHealth
    {
        get
        {
            if (_damageable != null)
                return _damageable.CurrentHealth;

            return _healthSO != null ? _healthSO.CurrentHealth : 0f;
        }
    }

    private float InfectionGainMultiplier =>
        _difficultyRules != null ? _difficultyRules.PlayerInfectionGainMultiplier : 1f;

    private float InfectionRecoverMultiplier =>
        _difficultyRules != null ? _difficultyRules.PlayerInfectionRecoverMultiplier : 1f;

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
        PublishSnapshot();
    }

    private void OnEnable()
    {
        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised += OnHealthChanged;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised -= OnHealthChanged;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.Clear(this);
    }

    public void AddVirusZoneExposure(float deltaTime)
    {
        if (_infectionRules == null)
            return;

        AddInfection(_infectionRules.VirusZoneGainPerSecond * InfectionGainMultiplier * deltaTime);
    }
    public void AddPoisonPuddleZoneExposure(float deltaTime)
    {
        if (_infectionRules == null)
            return;

        AddInfection(_infectionRules.PoisonPuddleInfectionGainPerSecond * InfectionGainMultiplier * deltaTime);
    }

    public void AddVaccineZoneRecovery(float deltaTime)
    {
        if (_infectionRules == null)
            return;

        ReduceInfection(_infectionRules.VaccineZoneRecoverPerSecond * InfectionRecoverMultiplier * deltaTime);
    }

    public void AddHitInfection()
    {
        if (_infectionRules == null)
            return;

        AddInfection(_infectionRules.InfectionOnHit * InfectionGainMultiplier);
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
        PublishSnapshot();
    }

    public void ReduceInfection(float amount)
    {
        if (_isDead || amount <= 0f)
            return;

        _currentInfection -= amount;
        ClampInfectionToMaxHealth();

        PublishSnapshot();
    }

    public void ClearInfection()
    {
        _currentInfection = 0f;
        PublishSnapshot();
    }

    private void OnHealthChanged()
    {
        ClampInfectionToMaxHealth();
        CheckInfectionDeath();
        PublishSnapshot();
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

        if (_damageable != null && !_damageable.IsDead)
            _damageable.Kill();
        else if (_deathEvent != null)
            _deathEvent.RaiseEvent();
    }

    private void PublishSnapshot()
    {
        if (_playerHealthChanged == null)
            return;

        _playerHealthChanged.RaiseEvent(new PlayerHealthSnapshot(
            MaxHealth,
            CurrentHealth,
            _currentInfection,
            _isDead));
    }
}
