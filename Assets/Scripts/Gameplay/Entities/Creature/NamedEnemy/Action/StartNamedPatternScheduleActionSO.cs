/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Implements the focused NamedPatternScheduleStartMode responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _config = (StartNamedPatternScheduleActionSO)OriginSO;
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_config.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_config.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    // Purpose: Applies the requested apply change.
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
