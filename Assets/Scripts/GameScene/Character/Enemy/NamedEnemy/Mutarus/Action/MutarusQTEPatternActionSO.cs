using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternAction",
    menuName = "State Machines/Named Enemy Actions/Mutarus/QTE Pattern")]
public class MutarusQTEPatternActionSO : StateActionSO<MutarusQTEPatternAction>
{
    [Header("QTE")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _controllerReadyChannel;

    [Header("Definition Config")]
    [SerializeField] private MutarusQTEPatternBombConfigSO _bombConfig;

    public MutarusQTEPatternControllerEventChannelSO ControllerReadyChannel => _controllerReadyChannel;

    public bool HasBombConfig => _bombConfig != null;
    public bool UsePeriodicArcBomb => _bombConfig.UsePeriodicArcBomb;
    public EnemyArcBombProjectile ArcBombPrefab => _bombConfig.ArcBombPrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _bombConfig.MaskRenderManagerReadyChannel;

    public bool FireBombOnEnter => _bombConfig.FireBombOnEnter;
    public float FirstBombDelay => _bombConfig.FirstBombDelay;
    public float BombInterval => _bombConfig.BombInterval;
    public int BombsPerBurst => _bombConfig.BombsPerBurst;
    public float BombShotInterval => _bombConfig.BombShotInterval;

    public float BombTravelTime => _bombConfig.BombTravelTime;
    public float BombArcHeight => _bombConfig.BombArcHeight;
    public float BombSpawnYOffset => _bombConfig.BombSpawnYOffset;
    public float BombTargetYOffset => _bombConfig.BombTargetYOffset;
    public float FallbackTargetDistance => _bombConfig.FallbackTargetDistance;
    public float TargetRandomRadius => _bombConfig.TargetRandomRadius;

    public bool DisableProjectileCollidersDuringFlight => _bombConfig.DisableProjectileCollidersDuringFlight;
    public bool ShowImpactTelegraph => _bombConfig.ShowImpactTelegraph;
    public float ImpactTelegraphRadius => _bombConfig.ImpactTelegraphRadius;
    public AreaAttackTelegraphStyle ImpactTelegraphStyle => _bombConfig.ImpactTelegraphStyle;

    public float DamageRadius => _bombConfig.DamageRadius;
    public float ImpactHealthDamage => _bombConfig.ImpactHealthDamage;
    public float ImpactInfectionDamage => _bombConfig.ImpactInfectionDamage;
    public LayerMask DamageTargetMask => _bombConfig.DamageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _bombConfig.TriggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _bombConfig.PaintChannel;
    public float PaintRadiusWorld => _bombConfig.PaintRadiusWorld;
    public int PaintPriority => _bombConfig.PaintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _bombConfig.PoisonPuddleDamageConfig;

    public bool FireBombOnPatternFailed => _bombConfig.FireBombOnPatternFailed;
    public EnemyArcBombProjectile FailureArcBombPrefab => _bombConfig.FailureArcBombPrefab;
    public bool FailureBombTargetSectorCenter => _bombConfig.FailureBombTargetSectorCenter;
    public float FailureBombTravelTime => _bombConfig.FailureBombTravelTime;
    public float FailureBombArcHeight => _bombConfig.FailureBombArcHeight;
    public float FailureBombSpawnYOffset => _bombConfig.FailureBombSpawnYOffset;
    public float FailureBombTargetYOffset => _bombConfig.FailureBombTargetYOffset;

    public float FailureDamageRadius => _bombConfig.FailureDamageRadius;
    public float FailureImpactHealthDamage => _bombConfig.FailureImpactHealthDamage;
    public float FailureImpactInfectionDamage => _bombConfig.FailureImpactInfectionDamage;
    public float FailureImpactTelegraphRadius => _bombConfig.FailureImpactTelegraphRadius;

    public MaskRenderManager.PaintChannel FailurePaintChannel => _bombConfig.FailurePaintChannel;
    public float FailurePaintRadiusWorld => _bombConfig.FailurePaintRadiusWorld;
    public int FailurePaintPriority => _bombConfig.FailurePaintPriority;
    public PoisonPuddleDamageConfigSO FailurePoisonPuddleDamageConfig => _bombConfig.FailurePoisonPuddleDamageConfig;

    public bool TriggerAnimatorOnBomb => _bombConfig.TriggerAnimatorOnBomb;
    public string BombAnimatorTrigger => _bombConfig.BombAnimatorTrigger;

    public bool TriggerAnimatorOnFailureBomb => _bombConfig.TriggerAnimatorOnFailureBomb;
    public string FailureBombAnimatorTrigger => _bombConfig.FailureBombAnimatorTrigger;
}

public class MutarusQTEPatternAction : StateAction
{
    private MutarusQTEPatternActionSO _config;
    private NamedPatternController _pattern;
    private MutarusQTEPatternController _qteController;
    private Enemy _enemy;
    private EnemyAttackRig _attackRig;
    private Animator _animator;

    private int _bombTriggerHash;
    private int _failureBombTriggerHash;

    private bool _completed;
    private bool _runtimeReady;
    private float _runtimeDuration;

    private float _bombTimer;
    private float _shotTimer;
    private float _nextBurstDelay;
    private int _shotsFiredInBurst;
    private bool _burstActive;
    private bool _hasBombConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (MutarusQTEPatternActionSO)OriginSO;

        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _attackRig = stateMachine.GetComponentInParent<EnemyAttackRig>();
        _animator = stateMachine.GetComponentInParent<Animator>();

        if (_config.ControllerReadyChannel != null)
            _qteController = _config.ControllerReadyChannel.Current;

        if (_qteController == null)
            _qteController = Object.FindAnyObjectByType<MutarusQTEPatternController>();

        if (_config.HasBombConfig)
        {
            if (!string.IsNullOrWhiteSpace(_config.BombAnimatorTrigger))
                _bombTriggerHash = Animator.StringToHash(_config.BombAnimatorTrigger);

            if (!string.IsNullOrWhiteSpace(_config.FailureBombAnimatorTrigger))
                _failureBombTriggerHash = Animator.StringToHash(_config.FailureBombAnimatorTrigger);
        }
    }

    public override void OnStateEnter()
    {
        _completed = false;
        _runtimeReady = false;
        _hasBombConfig = _config.HasBombConfig;

        _bombTimer = 0f;
        _shotTimer = 0f;
        _shotsFiredInBurst = 0;
        _burstActive = false;
        _nextBurstDelay = _hasBombConfig ? _config.FirstBombDelay : 0f;

        if (!_hasBombConfig)
        {
            Debug.LogError("[MutarusQTEPatternAction] Bomb Config is missing.");

            if (_pattern != null)
                _pattern.MarkActiveFinished(NamedPatternResult.PlayerFailed);

            _completed = true;
            return;
        }

        RefreshQTEController();

        if (_qteController == null)
        {
            Debug.LogWarning("[MutarusQTEPatternAction] QTE controller is missing. Pattern fails.");

            if (_pattern != null)
                _pattern.MarkActiveFinished(NamedPatternResult.PlayerFailed);

            _completed = true;
            return;
        }

        bool useObjectiveCounter = _pattern != null && _pattern.ShowObjectiveCounter;
        int requiredObjectiveCount = _pattern != null ? _pattern.ObjectiveRequiredCount : 0;

        _runtimeDuration = _pattern != null ? _pattern.PatternActiveDuration : 20f;

        _qteController.BeginPattern(
            _runtimeDuration,
            HandlePatternResult,
            useObjectiveCounter,
            requiredObjectiveCount,
            HandleObjectiveProgress,
            HandleRuntimeReady);
    }

    public override void OnUpdate()
    {
        if (_completed || !_runtimeReady)
            return;

        TickPeriodicArcBomb();
    }

    public override void OnStateExit()
    {
        if (_completed)
            return;

        if (_qteController != null)
            _qteController.CancelPatternWithoutResult();
    }

    private void HandleRuntimeReady()
    {
        if (_completed)
            return;

        _runtimeReady = true;
        _bombTimer = 0f;
        _shotTimer = 0f;
        _burstActive = false;
        _shotsFiredInBurst = 0;

        if (_pattern != null)
            _pattern.StartPatternActiveTimer(_runtimeDuration);

        if (!_config.UsePeriodicArcBomb)
            return;

        if (_config.FireBombOnEnter)
        {
            StartBombBurst();
            return;
        }

        _nextBurstDelay = _config.FirstBombDelay;
    }

    private void HandlePatternResult(NamedPatternResult result)
    {
        if (_completed)
            return;

        _completed = true;
        _runtimeReady = false;
        _burstActive = false;

        if (result == NamedPatternResult.PlayerFailed)
            FireFailureArcBomb();

        if (_pattern != null)
            _pattern.MarkActiveFinished(result);
    }

    private void HandleObjectiveProgress(int completedCount, int requiredCount)
    {
        if (_pattern != null)
            _pattern.SetObjectiveProgress(completedCount, requiredCount);
    }

    private void TickPeriodicArcBomb()
    {
        if (!_config.UsePeriodicArcBomb)
            return;

        if (_config.ArcBombPrefab == null || _enemy == null)
            return;

        if (_burstActive)
        {
            TickBombBurst();
            return;
        }

        _bombTimer += Time.deltaTime;

        if (_bombTimer < Mathf.Max(0.01f, _nextBurstDelay))
            return;

        StartBombBurst();
    }

    private void StartBombBurst()
    {
        _burstActive = true;
        _shotsFiredInBurst = 0;
        _shotTimer = 0f;
        _bombTimer = 0f;

        FirePeriodicArcBomb();
        _shotsFiredInBurst++;

        if (_shotsFiredInBurst >= _config.BombsPerBurst)
            FinishBombBurst();
    }

    private void TickBombBurst()
    {
        if (_shotsFiredInBurst >= _config.BombsPerBurst)
        {
            FinishBombBurst();
            return;
        }

        _shotTimer += Time.deltaTime;

        if (_shotTimer < _config.BombShotInterval)
            return;

        _shotTimer = 0f;

        FirePeriodicArcBomb();
        _shotsFiredInBurst++;

        if (_shotsFiredInBurst >= _config.BombsPerBurst)
            FinishBombBurst();
    }

    private void FinishBombBurst()
    {
        _burstActive = false;
        _shotsFiredInBurst = 0;
        _shotTimer = 0f;
        _bombTimer = 0f;
        _nextBurstDelay = _config.BombInterval;
    }

    private void FirePeriodicArcBomb()
    {
        if (_config.ArcBombPrefab == null || _enemy == null)
            return;

        Transform fireOrigin = _attackRig != null ? _attackRig.FireOrigin : _enemy.transform;

        Vector3 start = fireOrigin.position + Vector3.up * _config.BombSpawnYOffset;
        Vector3 target = ResolveBombTarget();

        SpawnArcBomb(
            _config.ArcBombPrefab,
            start,
            target,
            _config.BombTravelTime,
            _config.BombArcHeight,
            _config.DamageRadius,
            _config.ImpactHealthDamage,
            _config.ImpactInfectionDamage,
            _config.PaintChannel,
            _config.PaintRadiusWorld,
            _config.PaintPriority,
            _config.PoisonPuddleDamageConfig,
            _config.DisableProjectileCollidersDuringFlight,
            _config.ShowImpactTelegraph,
            _config.ImpactTelegraphRadius,
            _config.ImpactTelegraphStyle);

        TriggerAnimator(_config.TriggerAnimatorOnBomb, _bombTriggerHash);
    }

    private void FireFailureArcBomb()
    {
        if (!_config.FireBombOnPatternFailed)
            return;

        if (_config.FailureArcBombPrefab == null || _enemy == null)
            return;

        Transform fireOrigin = _attackRig != null ? _attackRig.FireOrigin : _enemy.transform;

        Vector3 start = fireOrigin.position + Vector3.up * _config.FailureBombSpawnYOffset;
        Vector3 target = ResolveFailureBombTarget();

        SpawnArcBomb(
            _config.FailureArcBombPrefab,
            start,
            target,
            _config.FailureBombTravelTime,
            _config.FailureBombArcHeight,
            _config.FailureDamageRadius,
            _config.FailureImpactHealthDamage,
            _config.FailureImpactInfectionDamage,
            _config.FailurePaintChannel,
            _config.FailurePaintRadiusWorld,
            _config.FailurePaintPriority,
            _config.FailurePoisonPuddleDamageConfig,
            _config.DisableProjectileCollidersDuringFlight,
            _config.ShowImpactTelegraph,
            _config.FailureImpactTelegraphRadius,
            _config.ImpactTelegraphStyle);

        TriggerAnimator(_config.TriggerAnimatorOnFailureBomb, _failureBombTriggerHash);
    }

    private void SpawnArcBomb(
        EnemyArcBombProjectile prefab,
        Vector3 start,
        Vector3 target,
        float travelTime,
        float arcHeight,
        float damageRadius,
        float impactHealthDamage,
        float impactInfectionDamage,
        MaskRenderManager.PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        PoisonPuddleDamageConfigSO poisonPuddleDamageConfig,
        bool disableCollidersDuringFlight,
        bool showImpactTelegraph,
        float impactTelegraphRadius,
        AreaAttackTelegraphStyle impactTelegraphStyle)
    {
        Transform projectileRoot = _attackRig != null && _attackRig.ProjectileRoot != null
            ? _attackRig.ProjectileRoot
            : ProjectileRootRegistry.Root;

        EnemyArcBombProjectile projectile = projectileRoot != null
            ? Object.Instantiate(prefab, start, Quaternion.identity, projectileRoot)
            : Object.Instantiate(prefab, start, Quaternion.identity);

        projectile.Init(
            start,
            target,
            travelTime,
            arcHeight,
            damageRadius,
            impactHealthDamage,
            impactInfectionDamage,
            _config.DamageTargetMask,
            _config.TriggerInteraction,
            _config.MaskRenderManagerReadyChannel,
            paintChannel,
            paintRadiusWorld,
            paintPriority,
            poisonPuddleDamageConfig,
            _enemy.gameObject,
            disableCollidersDuringFlight,
            showImpactTelegraph,
            impactTelegraphRadius,
            impactTelegraphStyle);
    }

    private Vector3 ResolveBombTarget()
    {
        Vector3 target;

        if (_qteController != null &&
            _qteController.TryGetPlayerPosition(out Vector3 playerPosition))
        {
            target = playerPosition;
        }
        else if (_enemy.currentTarget != null)
        {
            target = _enemy.currentTarget.transform.position;
        }
        else
        {
            target = _enemy.transform.position +
                     _enemy.transform.forward * _config.FallbackTargetDistance;
        }

        target.y += _config.BombTargetYOffset;

        if (_config.TargetRandomRadius > 0f)
        {
            Vector2 random = Random.insideUnitCircle * _config.TargetRandomRadius;
            target.x += random.x;
            target.z += random.y;
        }

        return target;
    }

    private Vector3 ResolveFailureBombTarget()
    {
        if (_config.FailureBombTargetSectorCenter &&
            _enemy != null &&
            _enemy.CurrentSector != null)
        {
            Bounds bounds = _enemy.CurrentSector.GetWorldBounds();
            Vector3 center = bounds.center;
            center.y += _config.FailureBombTargetYOffset;
            return center;
        }

        if (_qteController != null &&
            _qteController.TryGetPlayerPosition(out Vector3 playerPosition))
        {
            playerPosition.y += _config.FailureBombTargetYOffset;
            return playerPosition;
        }

        if (_enemy.currentTarget != null)
        {
            Vector3 target = _enemy.currentTarget.transform.position;
            target.y += _config.FailureBombTargetYOffset;
            return target;
        }

        Vector3 fallback = _enemy.transform.position;
        fallback.y += _config.FailureBombTargetYOffset;
        return fallback;
    }

    private void TriggerAnimator(bool shouldTrigger, int triggerHash)
    {
        if (!shouldTrigger || _animator == null || triggerHash == 0)
            return;

        _animator.SetTrigger(triggerHash);
    }

    private void RefreshQTEController()
    {
        if (_qteController != null)
            return;

        if (_config.ControllerReadyChannel != null)
            _qteController = _config.ControllerReadyChannel.Current;

        if (_qteController == null)
            _qteController = Object.FindAnyObjectByType<MutarusQTEPatternController>();
    }
}
