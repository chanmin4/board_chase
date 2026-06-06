using UnityEngine;

[DisallowMultipleComponent]
public class PlayerZoneExposure : MonoBehaviour
{
    private enum ExposureKind
    {
        None,
        Virus,
        Vaccine,
        PoisonPuddle
    }

    [Header("Refs")]
    [SerializeField] private PlayerInfection _playerInfection;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private Transform _samplePoint;

    [Header("Don't Touch Refs")]
    [SerializeField] private MaskRenderManager _maskRenderManager;

    [Header("Options")]
    [SerializeField] private bool _requireOpenedSector = true;

    private ExposureKind _activeExposureKind;
    private PoisonPuddleDamageConfigSO _activePoisonPuddleConfig;
    private float _zoneTickTimer;

    private void Reset()
    {
        if (_playerInfection == null)
            _playerInfection = GetComponent<PlayerInfection>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_samplePoint == null)
            _samplePoint = transform;
    }

    private void Awake()
    {
        if (_playerInfection == null)
            _playerInfection = GetComponent<PlayerInfection>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_samplePoint == null)
            _samplePoint = transform;
    }

    private void Update()
    {
        if (_playerInfection == null)
            return;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null || _samplePoint == null)
            return;

        ResolveCurrentExposure(
            out ExposureKind exposureKind,
            out PoisonPuddleDamageConfigSO poisonPuddleConfig);

        if (exposureKind == ExposureKind.None)
        {
            ResetZoneTick();
            return;
        }

        if (exposureKind != _activeExposureKind ||
            poisonPuddleConfig != _activePoisonPuddleConfig)
        {
            _activeExposureKind = exposureKind;
            _activePoisonPuddleConfig = poisonPuddleConfig;
            _zoneTickTimer = 0f;
        }

        TickExposure(exposureKind, poisonPuddleConfig);
    }

    private void ResolveCurrentExposure(
        out ExposureKind exposureKind,
        out PoisonPuddleDamageConfigSO poisonPuddleConfig)
    {
        exposureKind = ExposureKind.None;
        poisonPuddleConfig = null;

        if (_maskRenderManager.TryGetPoisonPuddleAtWorld(
                _samplePoint.position,
                out poisonPuddleConfig,
                _requireOpenedSector) &&
            poisonPuddleConfig != null &&
            poisonPuddleConfig.HasDamage)
        {
            exposureKind = ExposureKind.PoisonPuddle;
            return;
        }

        if (!_maskRenderManager.TryGetStateAtWorld(
                _samplePoint.position,
                out MaskRenderManager.PaintState state,
                _requireOpenedSector))
        {
            return;
        }

        switch (state)
        {
            case MaskRenderManager.PaintState.Virus:
                exposureKind = ExposureKind.Virus;
                break;

            case MaskRenderManager.PaintState.Vaccine:
                exposureKind = ExposureKind.Vaccine;
                break;
        }
    }

    private void TickExposure(
        ExposureKind exposureKind,
        PoisonPuddleDamageConfigSO poisonPuddleConfig)
    {
        float tickInterval = _playerInfection.ZoneTickInterval;

        _zoneTickTimer += Time.deltaTime;

        while (_zoneTickTimer >= tickInterval)
        {
            _zoneTickTimer -= tickInterval;
            ApplyExposureTick(exposureKind, poisonPuddleConfig);
        }
    }

    private void ApplyExposureTick(
        ExposureKind exposureKind,
        PoisonPuddleDamageConfigSO poisonPuddleConfig)
    {
        switch (exposureKind)
        {
            case ExposureKind.Virus:
                _playerInfection.ApplyVirusZoneTick();
                break;

            case ExposureKind.Vaccine:
                _playerInfection.ApplyVaccineZoneTick();
                break;

            case ExposureKind.PoisonPuddle:
                ApplyPoisonPuddleTick(poisonPuddleConfig);
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
            _playerInfection.AddInfection(infectionGain);
    }

    private void ResetZoneTick()
    {
        _activeExposureKind = ExposureKind.None;
        _activePoisonPuddleConfig = null;
        _zoneTickTimer = 0f;
    }
}