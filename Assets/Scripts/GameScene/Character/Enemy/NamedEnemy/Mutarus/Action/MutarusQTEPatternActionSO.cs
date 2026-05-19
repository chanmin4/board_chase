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

    public float BombTravelTime => _bombConfig.BombTravelTime;
    public float BombArcHeight => _bombConfig.BombArcHeight;
    public float BombSpawnYOffset => _bombConfig.BombSpawnYOffset;
    public float BombTargetYOffset => _bombConfig.BombTargetYOffset;
    public float FallbackTargetDistance => _bombConfig.FallbackTargetDistance;
    public float TargetRandomRadius => _bombConfig.TargetRandomRadius;

    public float DamageRadius => _bombConfig.DamageRadius;
    public float ImpactHealthDamage => _bombConfig.ImpactHealthDamage;
    public float ImpactInfectionDamage => _bombConfig.ImpactInfectionDamage;
    public LayerMask DamageTargetMask => _bombConfig.DamageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _bombConfig.TriggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _bombConfig.PaintChannel;
    public float PaintRadiusWorld => _bombConfig.PaintRadiusWorld;
    public int PaintPriority => _bombConfig.PaintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _bombConfig.PoisonPuddleDamageConfig;

    public bool TriggerAnimatorOnBomb => _bombConfig.TriggerAnimatorOnBomb;
    public string BombAnimatorTrigger => _bombConfig.BombAnimatorTrigger;
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

    private bool _completed;
    private bool _runtimeReady;
    private float _runtimeDuration;

    private float _bombTimer;
    private float _nextBombDelay;
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

        if (_config.HasBombConfig && !string.IsNullOrWhiteSpace(_config.BombAnimatorTrigger))
            _bombTriggerHash = Animator.StringToHash(_config.BombAnimatorTrigger);
    }

    public override void OnStateEnter()
    {
        _completed = false;
        _runtimeReady = false;
        _hasBombConfig = _config.HasBombConfig;

        _bombTimer = 0f;
        _nextBombDelay = _hasBombConfig ? _config.FirstBombDelay : 0f;

        if (!_hasBombConfig)
        {
            Debug.LogError("[MutarusQTEPatternAction] Bomb Config is missing.");

            if (_pattern != null)
                _pattern.MarkActiveFinished(NamedPatternResult.PlayerFailed);

            _completed = true;
            return;
        }

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

        if (_pattern != null)
            _pattern.StartPatternActiveTimer(_runtimeDuration);

        if (!_config.UsePeriodicArcBomb)
            return;

        if (_config.FireBombOnEnter)
        {
            FireArcBomb();
            _nextBombDelay = _config.BombInterval;
            return;
        }

        _nextBombDelay = _config.FirstBombDelay;
    }

    private void HandlePatternResult(NamedPatternResult result)
    {
        if (_completed)
            return;

        _completed = true;
        _runtimeReady = false;

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

        float delay = Mathf.Max(0.01f, _nextBombDelay);

        _bombTimer += Time.deltaTime;

        if (_bombTimer < delay)
            return;

        _bombTimer = 0f;
        _nextBombDelay = _config.BombInterval;

        FireArcBomb();
    }

    private void FireArcBomb()
    {
        if (_config.ArcBombPrefab == null || _enemy == null)
            return;

        Transform fireOrigin = _attackRig != null ? _attackRig.FireOrigin : _enemy.transform;

        Transform projectileRoot = _attackRig != null && _attackRig.ProjectileRoot != null
            ? _attackRig.ProjectileRoot
            : ProjectileRootRegistry.Root;

        Vector3 start = fireOrigin.position + Vector3.up * _config.BombSpawnYOffset;
        Vector3 target = ResolveBombTarget();

        EnemyArcBombProjectile projectile = projectileRoot != null
            ? Object.Instantiate(_config.ArcBombPrefab, start, Quaternion.identity, projectileRoot)
            : Object.Instantiate(_config.ArcBombPrefab, start, Quaternion.identity);

        projectile.Init(
            start,
            target,
            _config.BombTravelTime,
            _config.BombArcHeight,
            _config.DamageRadius,
            _config.ImpactHealthDamage,
            _config.ImpactInfectionDamage,
            _config.DamageTargetMask,
            _config.TriggerInteraction,
            _config.MaskRenderManagerReadyChannel,
            _config.PaintChannel,
            _config.PaintRadiusWorld,
            _config.PaintPriority,
            _config.PoisonPuddleDamageConfig,
            _enemy.gameObject);

        if (_config.TriggerAnimatorOnBomb &&
            _animator != null &&
            _bombTriggerHash != 0)
        {
            _animator.SetTrigger(_bombTriggerHash);
        }
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
}
