using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ArrivedToNamedAnchorCondition",
    menuName = "State Machines/Named Enemy Conditions/Arrived To Named Anchor")]
public class ArrivedToNamedAnchorConditionSO : StateConditionSO<ArrivedToNamedAnchorCondition>
{
    [Header("Arrival")]
    [Tooltip("Extra distance allowed from the named spawn/anchor point.")]
    [SerializeField, Min(0f)] private float _extraTolerance = 0.1f;

    [Tooltip("If true, NavMeshAgent.remainingDistance is used when available.")]
    [SerializeField] private bool _useAgentRemainingDistance = true;

    [Tooltip("If true, condition waits until the agent has no pending path.")]
    [SerializeField] private bool _requirePathReady = true;

    public float ExtraTolerance => _extraTolerance;
    public bool UseAgentRemainingDistance => _useAgentRemainingDistance;
    public bool RequirePathReady => _requirePathReady;
}

public class ArrivedToNamedAnchorCondition : Condition
{
    private ArrivedToNamedAnchorConditionSO _config;
    private NamedEnemy _namedEnemy;
    private NavMeshAgent _agent;
    private Transform _owner;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (ArrivedToNamedAnchorConditionSO)OriginSO;
        _namedEnemy = stateMachine.GetComponentInParent<NamedEnemy>();
        _agent = stateMachine.GetComponentInParent<NavMeshAgent>();
        _owner = _namedEnemy != null ? _namedEnemy.transform : stateMachine.transform.root;
    }

    protected override bool Statement()
    {
        if (_namedEnemy == null || !_namedEnemy.HasSpawnContext)
            return false;

        float allowedDistance = ResolveAllowedDistance();

        if (_config.UseAgentRemainingDistance && CanUseAgent())
        {
            if (_config.RequirePathReady && _agent.pathPending)
                return false;

            if (_agent.remainingDistance <= allowedDistance)
                return true;
        }

        Vector3 current = _owner.position;
        Vector3 target = _namedEnemy.SpawnPosition;

        current.y = 0f;
        target.y = 0f;

        return Vector3.Distance(current, target) <= allowedDistance;
    }

    private float ResolveAllowedDistance()
    {
        float allowedDistance = _config.ExtraTolerance;

        if (_agent != null)
            allowedDistance += Mathf.Max(0f, _agent.stoppingDistance);

        return Mathf.Max(0.01f, allowedDistance);
    }

    private bool CanUseAgent()
    {
        return _agent != null &&
               _agent.isActiveAndEnabled &&
               _agent.isOnNavMesh;
    }
}
