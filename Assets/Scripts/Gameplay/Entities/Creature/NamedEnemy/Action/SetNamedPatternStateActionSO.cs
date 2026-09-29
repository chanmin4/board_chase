/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Implements the focused NamedPatternActionCommand responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum NamedPatternActionCommand
{
    BeginPattern,
    MarkPrepareFinished,
    MarkActiveSucceeded,
    MarkActiveFailed,
    MarkResolved,
    SetPatternReadyTrue,
    SetPatternReadyFalse
}

[CreateAssetMenu(
    fileName = "SetNamedPatternStateAction",
    menuName = "State Machines/Named Enemy Actions/Set Pattern State")]
public class SetNamedPatternStateActionSO : StateActionSO<SetNamedPatternStateAction>
{
    [SerializeField] private NamedPatternActionCommand _command;
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    [Header("Delay")]
    [SerializeField] private bool _useDelay;
    [SerializeField, Min(0f)] private float _delaySeconds;

    public NamedPatternActionCommand Command => _command;
    public StateAction.SpecificMoment Moment => _moment;
    public bool UseDelay => _useDelay;
    public float DelaySeconds => _delaySeconds;
}

public class SetNamedPatternStateAction : StateAction
{
    private SetNamedPatternStateActionSO _config;
    private NamedPatternController _pattern;
    private float _timer;
    private bool _applied;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _config = (SetNamedPatternStateActionSO)OriginSO;
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _timer = 0f;
        _applied = false;

        if (_config.Moment == SpecificMoment.OnStateEnter && !_config.UseDelay)
            Apply();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_config.Moment != SpecificMoment.OnUpdate &&
            _config.Moment != SpecificMoment.OnStateEnter)
            return;

        if (!_config.UseDelay || _applied)
            return;

        _timer += Time.deltaTime;

        if (_timer >= _config.DelaySeconds)
            Apply();
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

        switch (_config.Command)
        {
            case NamedPatternActionCommand.BeginPattern:
                _pattern.BeginPattern();
                break;
            case NamedPatternActionCommand.MarkPrepareFinished:
                _pattern.MarkPrepareFinished();
                break;
            case NamedPatternActionCommand.MarkActiveSucceeded:
                _pattern.MarkActiveFinished(NamedPatternResult.PlayerSucceeded);
                break;
            case NamedPatternActionCommand.MarkActiveFailed:
                _pattern.MarkActiveFinished(NamedPatternResult.PlayerFailed);
                break;
            case NamedPatternActionCommand.MarkResolved:
                _pattern.MarkResolved();
                break;
            case NamedPatternActionCommand.SetPatternReadyTrue:
                _pattern.SetPatternReady(true);
                break;
            case NamedPatternActionCommand.SetPatternReadyFalse:
                _pattern.SetPatternReady(false);
                break;
        }

        _applied = true;
    }
}
