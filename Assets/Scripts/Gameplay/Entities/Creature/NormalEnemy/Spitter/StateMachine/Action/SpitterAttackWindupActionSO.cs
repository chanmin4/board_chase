/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Spitter > StateMachine > Action
 * Primary responsibility: Defines SpitterAttackWindupActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SpitterAttackWindupAction",
    menuName = "State Machines/Enemy Actions/Spitter/Attack Windup")]
public class SpitterAttackWindupActionSO : StateActionSO<SpitterAttackWindupAction>
{
    [SerializeField] private SpitterAttackConfigSO _config;

    public SpitterAttackConfigSO Config => _config;
}

public class SpitterAttackWindupAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private SpitterAttackWindupActionSO _origin;
    private float _side;
    private Transform _transform;
    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _origin = (SpitterAttackWindupActionSO)OriginSO;
        _transform = stateMachine.transform;
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _side = Random.value < 0.5f ? -1f : 1f;

        if (IsAgentReady())
        {
            _agent.isStopped = false;
            _agent.speed = _origin.Config.WindupMoveSpeed;
        }
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_origin.Config == null || _enemy == null || _enemy.currentTarget == null)
            return;

        Vector3 targetPos = _enemy.currentTarget.transform.position;

        RotateToward(targetPos);

        if (!IsAgentReady())
            return;

        Vector3 destination = PickStrafePoint(targetPos);
        if (NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                _origin.Config.NavMeshSampleDistance,
                NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (!IsAgentReady())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    // Purpose: Performs the pick strafe point workflow for this component.
    private Vector3 PickStrafePoint(Vector3 targetPos)
    {
        Vector3 fromTarget = _transform.position - targetPos;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = -_transform.forward;

        Vector3 radial = fromTarget.normalized;
        Vector3 tangent = new Vector3(-radial.z, 0f, radial.x) * _side;

        return _transform.position + tangent * _origin.Config.StrafeDistance;
    }

    // Purpose: Performs the rotate toward workflow for this component.
    private void RotateToward(Vector3 targetPos)
    {
        Vector3 direction = targetPos - _transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        _transform.rotation = Quaternion.RotateTowards(
            _transform.rotation,
            targetRotation,
            _origin.Config.TurnSpeedDegPerSecond * Time.deltaTime);
    }

    // Purpose: Checks whether agent ready is currently true.
    private bool IsAgentReady()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }
}
