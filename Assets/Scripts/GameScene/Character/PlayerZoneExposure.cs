using UnityEngine;

[DisallowMultipleComponent]
public class PlayerZoneExposure : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInfection _playerInfection;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private Transform _samplePoint;

    [Header("Don't Touch Refs")]
    [SerializeField] private MaskRenderManager _maskRenderManager;

    [Header("Options")]
    [SerializeField] private bool _requireOpenedSector = true;

    private float _poisonPuddleTickTimer;
    private float _poisonPuddleAccumulatedTime;

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

        if (_maskRenderManager.TryGetPoisonPuddleAtWorld(
                _samplePoint.position,
                out PoisonPuddleDamageConfigSO poisonPuddleConfig,
                _requireOpenedSector) &&
            poisonPuddleConfig != null &&
            poisonPuddleConfig.HasDamage)
        {
            ApplyPoisonPuddleExposure(poisonPuddleConfig, Time.deltaTime);
            return;
        }

        ResetPoisonPuddleTick();

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
                _playerInfection.AddVirusZoneExposure(Time.deltaTime);
                break;

            case MaskRenderManager.PaintState.Vaccine:
                _playerInfection.AddVaccineZoneRecovery(Time.deltaTime);
                break;
        }
    }

    private void ApplyPoisonPuddleExposure(
        PoisonPuddleDamageConfigSO config,
        float deltaTime)
    {
        _poisonPuddleTickTimer += deltaTime;
        _poisonPuddleAccumulatedTime += deltaTime;

        if (_poisonPuddleTickTimer < config.TickInterval)
            return;

        float elapsed = _poisonPuddleAccumulatedTime;

        _poisonPuddleTickTimer = 0f;
        _poisonPuddleAccumulatedTime = 0f;

        if (_damageable != null && _damageable.CanReceiveDamage)
        {
            float healthDamage = config.HealthDamagePerSecond * elapsed;

            if (healthDamage > 0f)
                _damageable.ReceiveAnAttack(healthDamage);
        }

        float infectionGain = config.InfectionGainPerSecond * elapsed;

        if (infectionGain > 0f)
            _playerInfection.AddInfection(infectionGain);
    }

    private void ResetPoisonPuddleTick()
    {
        _poisonPuddleTickTimer = 0f;
        _poisonPuddleAccumulatedTime = 0f;
    }
}
