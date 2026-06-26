using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum NamedPatternDamageMultiplierCommand
{
    ApplyPatternActive,
    ClearPatternActive,
    ApplyPatternResult
}

[CreateAssetMenu(
    fileName = "ApplyDamageMultiplierFromPatternAction",
    menuName = "State Machines/Named Enemy Actions/Apply Damage Multiplier From Pattern")]
public class ApplyDamageMultiplierFromPatternActionSO
    : StateActionSO<ApplyDamageMultiplierFromPatternAction>
{
    [SerializeField] private NamedPatternConfigSO _config;
    [SerializeField] private NamedPatternDamageMultiplierCommand _command;
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    public NamedPatternConfigSO Config => _config;
    public NamedPatternDamageMultiplierCommand Command => _command;
    public StateAction.SpecificMoment Moment => _moment;
}

public class ApplyDamageMultiplierFromPatternAction : StateAction
{
    private ApplyDamageMultiplierFromPatternActionSO _origin;
    private NamedPatternController _pattern;
    private Damageable _damageable;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (ApplyDamageMultiplierFromPatternActionSO)OriginSO;
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
        _damageable = stateMachine.GetComponentInParent<Damageable>();
    }

    public override void OnStateEnter()
    {
        if (_origin.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    public override void OnUpdate()
    {
        if (_origin.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    public override void OnStateExit()
    {
        if (_origin.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_origin.Config == null || _damageable == null)
            return;

        switch (_origin.Command)
        {
            case NamedPatternDamageMultiplierCommand.ApplyPatternActive:
                ApplyActiveMultiplier();
                break;

            case NamedPatternDamageMultiplierCommand.ClearPatternActive:
                _damageable.ResetDamageTakenMultiplier();
                break;

            case NamedPatternDamageMultiplierCommand.ApplyPatternResult:
                ApplyResultMultiplier();
                break;
        }
    }

    private void ApplyActiveMultiplier()
    {
        if (!_origin.Config.ApplyActiveDamageMultiplier)
            return;

        _damageable.SetDamageTakenMultiplier(
            _origin.Config.ActiveDamageTakenMultiplier);
    }

    private void ApplyResultMultiplier()
    {
        if (_pattern == null)
            return;

        if (_pattern.result == NamedPatternResult.PlayerSucceeded)
        {
            if (!_origin.Config.ApplySuccessDamageMultiplier)
                return;

            _damageable.SetDamageTakenMultiplierForSeconds(
                _origin.Config.SuccessDamageTakenMultiplier,
                _origin.Config.SuccessMultiplierDuration);

            return;
        }

        if (_pattern.result == NamedPatternResult.PlayerFailed)
        {
            if (!_origin.Config.ApplyFailureDamageMultiplier)
                return;

            _damageable.SetDamageTakenMultiplierForSeconds(
                _origin.Config.FailureDamageTakenMultiplier,
                _origin.Config.FailureMultiplierDuration);
        }
    }
}
