using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NormalEnemyAttackAction",
    menuName = "State Machines/Enemy Actions/Normal Enemy/Attack")]
public class NormalEnemyAttackActionSO : StateActionSO<NormalEnemyAttackAction>
{
    [Header("Selection")]
    [Tooltip("If true, the action picks an attack from EnemyStatConfigSO on state enter.")]
    [SerializeField] private bool _selectAttackOnEnter = true;

    [Tooltip("If true, missing or unsupported attack config finishes the attack state immediately instead of leaving it running.")]
    [SerializeField] private bool _finishImmediatelyWhenInvalid = true;

    [Header("Exit")]
    [Tooltip("If true, clears the selected attack when the state exits. Usually true for common NormalEnemy FSM.")]
    [SerializeField] private bool _clearSelectedAttackOnExit = true;

    public bool SelectAttackOnEnter => _selectAttackOnEnter;
    public bool FinishImmediatelyWhenInvalid => _finishImmediatelyWhenInvalid;
    public bool ClearSelectedAttackOnExit => _clearSelectedAttackOnExit;
}

public class NormalEnemyAttackAction : StateAction
{
    private NormalEnemyAttackActionSO _origin;
    private StateMachine _stateMachine;
    private Enemy _enemy;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private EnemyAttackExecutorController _runtimeController;
    private EnemyAttackExecutor _runtime;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (NormalEnemyAttackActionSO)OriginSO;
        _stateMachine = stateMachine;
        _enemy = stateMachine.GetComponent<Enemy>();
        stateMachine.TryGetComponent(out _movementStatsProvider);
        _runtimeController = stateMachine.GetOrAddComponent<EnemyAttackExecutorController>();
    }

    public override void OnStateEnter()
    {
        _runtime = null;

        if (_runtimeController == null)
            return;

        if (!_origin.SelectAttackOnEnter)
        {
            _runtimeController.BeginAttack(_runtimeController.SelectedAttack);
            TryStartRuntime(_runtimeController.SelectedAttack);
            return;
        }

        if (!TryPickAttack(out EnemyAttackConfigSO attackConfig))
        {
            FinishInvalidAttack();
            return;
        }

        _runtimeController.BeginAttack(attackConfig);
        TryStartRuntime(attackConfig);
    }

    public override void OnUpdate()
    {
        if (_runtime == null || _runtime.IsFinished)
            return;

        _runtime.Tick(Time.deltaTime);

        if (_runtime.IsFinished)
            _runtimeController?.FinishAttack(false);
    }

    public override void OnStateExit()
    {
        _runtime?.Exit();
        _runtime = null;

        if (_origin.ClearSelectedAttackOnExit)
            _runtimeController?.ClearAttack();
    }

    private bool TryPickAttack(out EnemyAttackConfigSO attackConfig)
    {
        attackConfig = null;

        CreatureEnemyStatConfigSO statConfig = _movementStatsProvider != null
            ? _movementStatsProvider.EnemyStatConfig
            : null;

        if (statConfig == null || _enemy == null || _enemy.currentTarget == null)
            return false;

        float distance = ResolveTargetDistance();
        return statConfig.TryPickAttack(distance, out attackConfig);
    }

    private void TryStartRuntime(EnemyAttackConfigSO attackConfig)
    {
        if (attackConfig == null)
        {
            FinishInvalidAttack();
            return;
        }

        EnemyAttackExecutorContext context =
            new EnemyAttackExecutorContext(_stateMachine, _enemy, _runtimeController);

        if (!EnemyAttackExecutorFactory.TryCreate(attackConfig, context, out _runtime) ||
            _runtime == null)
        {
            FinishInvalidAttack();
            return;
        }

        _runtime.Enter();

        if (_runtime.IsFinished)
            _runtimeController?.FinishAttack(false);
    }

    private void FinishInvalidAttack()
    {
        if (_origin.FinishImmediatelyWhenInvalid)
            _runtimeController?.FinishAttack();
    }

    private float ResolveTargetDistance()
    {
        Vector3 from = _enemy.transform.position;
        Vector3 to = _enemy.currentTarget.transform.position;

        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to);
    }
}
