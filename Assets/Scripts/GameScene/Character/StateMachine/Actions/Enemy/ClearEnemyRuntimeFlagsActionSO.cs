using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ClearEnemyRuntimeFlagsAction",
    menuName = "State Machines/Enemy Actions/Clear Enemy Runtime Flags")]
public class ClearEnemyRuntimeFlagsActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    [Header("Flags")]
    [SerializeField] private bool _clearInfectionTarget;
    [SerializeField] private bool _clearSearchSucceeded;
    [SerializeField] private bool _clearInfectionTargetValid;
    [SerializeField] private bool _clearKnockbackRequest;

    public StateAction.SpecificMoment Moment => _moment;
    public bool ClearInfectionTarget => _clearInfectionTarget;
    public bool ClearSearchSucceeded => _clearSearchSucceeded;
    public bool ClearInfectionTargetValid => _clearInfectionTargetValid;
    public bool ClearKnockbackRequest => _clearKnockbackRequest;

    protected override StateAction CreateAction() => new ClearEnemyRuntimeFlagsAction();
}

public class ClearEnemyRuntimeFlagsAction : StateAction
{
    private Enemy _enemy;
    private new ClearEnemyRuntimeFlagsActionSO OriginSO => (ClearEnemyRuntimeFlagsActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
    }

    public override void OnUpdate()
    {
        if (OriginSO.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    public override void OnStateEnter()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    public override void OnStateExit()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_enemy == null)
            return;

        if (OriginSO.ClearInfectionTarget)
            _enemy.ClearInfectionTarget();

        if (OriginSO.ClearSearchSucceeded)
            _enemy.SetSearchSucceeded(false);

        if (OriginSO.ClearInfectionTargetValid)
            _enemy.SetInfectionTargetvalid(false);

        if (OriginSO.ClearKnockbackRequest)
            _enemy.ClearKnockbackRequest();
    }
}
