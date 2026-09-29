/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Defines MoveNamedEnemyToSpawnPointActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MoveNamedEnemyToSpawnPointAction",
    menuName = "State Machines/Named Enemy Actions/Move To Spawn Point")]
public class MoveNamedEnemyToSpawnPointActionSO : StateActionSO<MoveNamedEnemyToSpawnPointAction>
{
    [SerializeField, Min(0.01f)] private float _moveSpeed = 3f;
    [SerializeField, Min(0f)] private float _stoppingDistance = 0.15f;
    [SerializeField] private bool _resetPathOnExit = true;

    public float MoveSpeed => _moveSpeed;
    public float StoppingDistance => _stoppingDistance;
    public bool ResetPathOnExit => _resetPathOnExit;
}

public class MoveNamedEnemyToSpawnPointAction : StateAction
{
    private MoveNamedEnemyToSpawnPointActionSO _config;
    private NamedEnemy _namedEnemy;
    private NavMeshAgent _agent;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _config = (MoveNamedEnemyToSpawnPointActionSO)OriginSO;
        _namedEnemy = stateMachine.GetComponentInParent<NamedEnemy>();
        _agent = stateMachine.GetComponentInParent<NavMeshAgent>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (!CanUseAgent() || _namedEnemy == null || !_namedEnemy.HasSpawnContext)
            return;

        _agent.speed = _config.MoveSpeed;
        _agent.stoppingDistance = _config.StoppingDistance;
        _agent.isStopped = false;
        _agent.SetDestination(_namedEnemy.SpawnPosition);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (!CanUseAgent() || _namedEnemy == null || !_namedEnemy.HasSpawnContext)
            return;

        _agent.SetDestination(_namedEnemy.SpawnPosition);
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (!CanUseAgent() || !_config.ResetPathOnExit)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    // Purpose: Checks whether use agent is currently permitted.
    private bool CanUseAgent()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }
}
