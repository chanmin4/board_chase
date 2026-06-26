using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum NamedPatternScheduleStartMode
{
    First,
    Repeat
}

[CreateAssetMenu(
    fileName = "StartNamedPatternScheduleAction",
    menuName = "State Machines/Named Enemy Actions/Start Pattern Schedule")]
public class StartNamedPatternScheduleActionSO : StateActionSO<StartNamedPatternScheduleAction>
{
    [SerializeField] private NamedPatternScheduleStartMode _startMode = NamedPatternScheduleStartMode.First;
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    public NamedPatternScheduleStartMode StartMode => _startMode;
    public StateAction.SpecificMoment Moment => _moment;
}

public class StartNamedPatternScheduleAction : StateAction
{
    private StartNamedPatternScheduleActionSO _config;
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (StartNamedPatternScheduleActionSO)OriginSO;
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
    }

    public override void OnStateEnter()
    {
        if (_config.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    public override void OnUpdate()
    {
    }

    public override void OnStateExit()
    {
        if (_config.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_pattern == null)
            return;

        if (_config.StartMode == NamedPatternScheduleStartMode.First)
            _pattern.StartFirstPatternSchedule();
        else
            _pattern.StartRepeatPatternSchedule();
    }
}
