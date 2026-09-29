/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Handles NamedPatternDamageMultiplierCommand combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (ApplyDamageMultiplierFromPatternActionSO)OriginSO;
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
        _damageable = stateMachine.GetComponentInParent<Damageable>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_origin.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_origin.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_origin.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    // Purpose: Applies the requested apply change.
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

    // Purpose: Applies the requested active multiplier change.
    private void ApplyActiveMultiplier()
    {
        if (!_origin.Config.ApplyActiveDamageMultiplier)
            return;

        _damageable.SetDamageTakenMultiplier(
            _origin.Config.ActiveDamageTakenMultiplier);
    }

    // Purpose: Applies the requested result multiplier change.
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
