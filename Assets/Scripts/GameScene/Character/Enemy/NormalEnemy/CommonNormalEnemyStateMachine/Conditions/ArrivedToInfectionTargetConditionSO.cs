using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ArrivedToInfectionTargetCondition", menuName = "State Machines/Enemy Conditions/Arrived To Infection Target")]
public class ArrivedToInfectionTargetConditionSO : StateConditionSO<ArrivedToInfectionTargetCondition>
{
    [Min(0f)]
    public float arriveDistance = 0.8f;
}

public class ArrivedToInfectionTargetCondition : Condition
{
    private Enemy _enemy;
    private Transform _transform;
    private NavMeshAgent _agent;
    private ArrivedToInfectionTargetConditionSO _originSO => (ArrivedToInfectionTargetConditionSO)OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _transform = stateMachine.transform;
        stateMachine.TryGetComponent(out _agent);
    }

    protected override bool Statement()
    {
        if (_enemy == null || !_enemy.HasInfectionTarget)
            return false;

        float arriveDistance = Mathf.Max(0.05f, _originSO.arriveDistance);

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            if (_agent.pathPending)
                return false;

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return false;

            float allowedDistance = Mathf.Max(arriveDistance, _agent.stoppingDistance + 0.05f);

            if (_agent.remainingDistance <= allowedDistance)
                return true;
        }

        Vector3 offset = _enemy.InfectionTargetPosition - _transform.position;
        offset.y = 0f;

        return offset.sqrMagnitude <= arriveDistance * arriveDistance;
    }
}
