using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternAction",
    menuName = "State Machines/Named Enemy Actions/Mutarus/QTE Pattern")]
public class MutarusQTEPatternActionSO : StateActionSO<MutarusQTEPatternAction>
{
    [Header("QTE")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _controllerReadyChannel;

    [Header("Periodic Arc Bomb")]
    [SerializeField] private bool _usePeriodicArcBomb = true;
    [SerializeField] private EnemyArcBombProjectile _arcBombPrefab;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("If true, fires one bomb as soon as QTE stations are active.")]
    [SerializeField] private bool _fireBombOnEnter = false;

    [Tooltip("Delay before the first bomb if Fire Bomb On Enter is false.")]
    [SerializeField, Min(0f)] private float _firstBombDelay = 2f;

    [Tooltip("Bomb interval during PatternActive. Bomb count is controlled only by pattern duration and this interval.")]
    [SerializeField, Min(0.01f)] private float _bombInterval = 5f;

    [Header("Arc Bomb Trajectory")]
    [SerializeField, Min(0.01f)] private float _bombTravelTime = 1.2f;
    [SerializeField, Min(0f)] private float _bombArcHeight = 4f;
    [SerializeField] private float _bombSpawnYOffset = 0.5f;
    [SerializeField] private float _bombTargetYOffset = 0f;

    [Tooltip("Used only if runtime player position and currentTarget are both missing.")]
    [SerializeField, Min(0f)] private float _fallbackTargetDistance = 6f;

    [Tooltip("Random offset around target position. 0 means exact target position.")]
    [SerializeField, Min(0f)] private float _targetRandomRadius = 0f;

    [Header("Arc Bomb Impact Damage")]
    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;

    [FormerlySerializedAs("_healthDamage")]
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;

    [FormerlySerializedAs("_infectionDamage")]
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;

    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Arc Bomb Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.PoisonPuddle;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority = 0;

    [Header("Poison Puddle Damage Config")]
    [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    [Header("Periodic Bomb Animator")]
    [SerializeField] private bool _triggerAnimatorOnBomb = true;
    [SerializeField] private string _bombAnimatorTrigger = "PatternBomb";

    public MutarusQTEPatternControllerEventChannelSO ControllerReadyChannel => _controllerReadyChannel;

    public bool UsePeriodicArcBomb => _usePeriodicArcBomb;
    public EnemyArcBombProjectile ArcBombPrefab => _arcBombPrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;

    public bool FireBombOnEnter => _fireBombOnEnter;
    public float FirstBombDelay => Mathf.Max(0f, _firstBombDelay);
    public float BombInterval => Mathf.Max(0.01f, _bombInterval);

    public float BombTravelTime => Mathf.Max(0.01f, _bombTravelTime);
    public float BombArcHeight => Mathf.Max(0f, _bombArcHeight);
    public float BombSpawnYOffset => _bombSpawnYOffset;
    public float BombTargetYOffset => _bombTargetYOffset;
    public float FallbackTargetDistance => Mathf.Max(0f, _fallbackTargetDistance);
    public float TargetRandomRadius => Mathf.Max(0f, _targetRandomRadius);

    public float DamageRadius => Mathf.Max(0f, _damageRadius);
    public float ImpactHealthDamage => Mathf.Max(0f, _impactHealthDamage);
    public float ImpactInfectionDamage => Mathf.Max(0f, _impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => Mathf.Max(0f, _paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;

    public bool TriggerAnimatorOnBomb => _triggerAnimatorOnBomb;
    public string BombAnimatorTrigger => _bombAnimatorTrigger;
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

        if (!string.IsNullOrWhiteSpace(_config.BombAnimatorTrigger))
            _bombTriggerHash = Animator.StringToHash(_config.BombAnimatorTrigger);
    }

    public override void OnStateEnter()
    {
        _completed = false;
        _runtimeReady = false;

        _bombTimer = 0f;
        _nextBombDelay = _config.FirstBombDelay;

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
