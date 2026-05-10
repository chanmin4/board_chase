using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SetSpawnReadyAction",
    menuName = "State Machines/Enemy Actions/Set Spawn Ready")]
public class SetSpawnReadyActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;
    [SerializeField] private bool _isSpawnReady = true;

    public StateAction.SpecificMoment Moment => _moment;
    public bool IsSpawnReady => _isSpawnReady;

    protected override StateAction CreateAction() => new SetSpawnReadyAction();
}

public class SetSpawnReadyAction : StateAction
{
    private Enemy _enemy;
    private new SetSpawnReadyActionSO OriginSO => (SetSpawnReadyActionSO)base.OriginSO;

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
        if (_enemy != null)
            _enemy.SetSpawnReady(OriginSO.IsSpawnReady);
    }
}
