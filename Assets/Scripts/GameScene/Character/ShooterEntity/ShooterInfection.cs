using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class ShooterInfection : MonoBehaviour
{
    private enum ExposureKind
    {
        None,
        Virus,
        Vaccine,
        PoisonPuddle
    }

    private const float DefaultZoneTickInterval = 0.2f;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Zone Exposure")]
    [SerializeField] private bool _enableZoneExposure = true;
    [SerializeField] private bool _requireOpenedSector = true;
    private MaskRenderManager _maskRenderManager;

    [Header("Runtime")]
    [SerializeField] private float _currentInfection;
    private Transform _samplePoint;
    private bool _isDead;
    private float _nextNaturalDecayTime;
    private float _zoneTickTimer;
    private ExposureKind _activeExposureKind;
    private PoisonPuddleDamageConfigSO _activePoisonPuddleConfig;

    public event Action<ShooterInfection> OnChanged;

    public float CurrentInfection => _currentInfection;
    public bool IsDead => _isDead;
    public Damageable Damageable => _damageable;
    public float MaxHealth => _damageable != null ? _damageable.MaxHealth : 0f;
    public float CurrentHealth => _damageable != null ? _damageable.CurrentHealth : 0f;

    public float ZoneTickInterval =>
        Rules != null ? Rules.ZoneTickInterval : DefaultZoneTickInterval;

    protected virtual EntityInfectionSettings Rules =>
        _damageable != null && _damageable.StatConfig != null
            ? _damageable.StatConfig.Infection
            : null;

    protected virtual float InfectionGainMultiplier => 1f;
    protected virtual float InfectionRecoverMultiplier => 1f;

    protected virtual void Reset()
    {
        ResolveRefs();
    }

    protected virtual void Awake()
    {
        ResolveRefs();
        ClampInfectionToMaxHealth();
    }

    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_damageable != null)
            _damageable.OnHealthChanged += HandleDamageableHealthChanged;
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;
        PublishCurrentSnapshot();
    }

    protected virtual void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= HandleDamageableHealthChanged;
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    protected virtual void Update()
    {
        TickNaturalDecay();
        TickZoneExposure();
    }
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }
    public void ApplyVirusZoneTick()
    {
        if (Rules == null)
            return;

        AddInfection(Rules.VirusZoneInfectionGainPerTick * InfectionGainMultiplier);
    }

    public void ApplyVaccineZoneTick()
    {
        if (Rules == null)
            return;

        ReduceInfection(Rules.VaccineZoneRecoverPerTick * InfectionRecoverMultiplier);
    }

    public void AddInfection(float amount)
    {
        if (_isDead || amount <= 0f)
            return;

        _currentInfection += amount;
        ClampInfectionToMaxHealth();

        CheckDeathState();
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

    public virtual void RecoverOnSectorCaptured()
    {
    }

    public virtual void RecoverOnNamedKilled()
    {
    }

    public virtual void RecoverOnBossKilled()
    {
    }

    public virtual void PublishCurrentSnapshot()
    {
        OnChanged?.Invoke(this);
    }

    protected virtual void ResolveRefs()
    {
        if (_damageable == null)
        {
            _damageable =
                GetComponent<Damageable>() ??
                GetComponentInParent<Damageable>() ??
                GetComponentInChildren<Damageable>(true);
        }

        if (_samplePoint == null)
            _samplePoint = transform;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    protected virtual void OnBecameDead(bool killedByInfection)
    {
    }

    private void HandleDamageableHealthChanged(Damageable damageable)
    {
        ClampInfectionToMaxHealth();
        CheckDeathState();
        PublishCurrentSnapshot();
    }

    private void TickNaturalDecay()
    {
        EntityInfectionSettings rules = Rules;

        if (_isDead || rules == null)
            return;

        float interval = rules.NaturalDecayInterval;
        float amount = rules.NaturalDecayAmountPerTick;

        if (interval <= 0f || amount <= 0f)
            return;

        if (Time.time < _nextNaturalDecayTime)
            return;

        _nextNaturalDecayTime = Time.time + interval;
        ReduceInfection(amount * InfectionRecoverMultiplier);
    }

    private void TickZoneExposure()
    {
        if (!_enableZoneExposure || _isDead)
            return;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null || _samplePoint == null)
            return;

        ResolveCurrentExposure(out ExposureKind exposureKind, out PoisonPuddleDamageConfigSO poisonConfig);

        if (exposureKind == ExposureKind.None)
        {
            ResetZoneTick();
            return;
        }

        if (exposureKind != _activeExposureKind ||
            poisonConfig != _activePoisonPuddleConfig)
        {
            _activeExposureKind = exposureKind;
            _activePoisonPuddleConfig = poisonConfig;
            _zoneTickTimer = 0f;
        }

        _zoneTickTimer += Time.deltaTime;

        while (_zoneTickTimer >= ZoneTickInterval)
        {
            _zoneTickTimer -= ZoneTickInterval;
            ApplyExposureTick(exposureKind, poisonConfig);
        }
    }

    private void ResolveCurrentExposure(
        out ExposureKind exposureKind,
        out PoisonPuddleDamageConfigSO poisonConfig)
    {
        exposureKind = ExposureKind.None;
        poisonConfig = null;

        if (_maskRenderManager.TryGetPoisonPuddleAtWorld(
                _samplePoint.position,
                out poisonConfig,
                _requireOpenedSector) &&
            poisonConfig != null &&
            poisonConfig.HasDamage)
        {
            exposureKind = ExposureKind.PoisonPuddle;
            return;
        }

        if (!_maskRenderManager.TryGetStateAtWorld(
                _samplePoint.position,
                out PaintSurfaceState state,
                _requireOpenedSector))
        {
            return;
        }

        switch (state)
        {
            case PaintSurfaceState.Virus:
            case PaintSurfaceState.CoatedVirus:
                exposureKind = ExposureKind.Virus;
                break;

            case PaintSurfaceState.Vaccine:
            case PaintSurfaceState.CoatedVaccine:
                exposureKind = ExposureKind.Vaccine;
                break;

            case PaintSurfaceState.PoisonPuddle:
                exposureKind = ExposureKind.PoisonPuddle;
                break;
        }
    }

    private void ApplyExposureTick(
        ExposureKind exposureKind,
        PoisonPuddleDamageConfigSO poisonConfig)
    {
        switch (exposureKind)
        {
            case ExposureKind.Virus:
                ApplyVirusZoneTick();
                break;

            case ExposureKind.Vaccine:
                ApplyVaccineZoneTick();
                break;

            case ExposureKind.PoisonPuddle:
                ApplyPoisonPuddleTick(poisonConfig);
                break;
        }
    }

    private void ApplyPoisonPuddleTick(PoisonPuddleDamageConfigSO config)
    {
        if (config == null)
            return;

        if (_damageable != null && _damageable.CanReceiveDamage)
        {
            float healthDamage = config.HealthDamagePerTick;

            if (healthDamage > 0f)
                _damageable.ReceiveAnAttack(healthDamage);
        }

        float infectionGain = config.InfectionGainPerTick;

        if (infectionGain > 0f)
            AddInfection(infectionGain);
    }

    private void ResetZoneTick()
    {
        _activeExposureKind = ExposureKind.None;
        _activePoisonPuddleConfig = null;
        _zoneTickTimer = 0f;
    }

    private void ClampInfectionToMaxHealth()
    {
        _currentInfection = Mathf.Clamp(_currentInfection, 0f, MaxHealth);
    }

    private void CheckDeathState()
    {
        if (_isDead)
            return;

        if (CurrentHealth <= 0f)
        {
            MarkDead(false);
            return;
        }

        if (_currentInfection < CurrentHealth)
            return;

        MarkDead(true);

        if (_damageable != null)
            _damageable.Kill();
    }

    private void MarkDead(bool killedByInfection)
    {
        if (_isDead)
            return;

        _isDead = true;
        OnBecameDead(killedByInfection);
        PublishCurrentSnapshot();
    }
}