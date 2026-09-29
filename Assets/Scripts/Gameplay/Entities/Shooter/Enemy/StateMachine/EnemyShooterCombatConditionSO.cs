/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterCombatConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterCombatCondition",
    menuName = "State Machines/Enemy Shooter/Conditions/Combat Query")]
public class EnemyShooterCombatConditionSO : StateConditionSO<EnemyShooterCombatCondition>
{
    public enum Query
    {
        HasTarget,
        IsAlerted,
        CanSeeTarget,
        ShouldChase,
        ShouldAttack,
        TargetLost,
        HasLastKnownPosition,
        CanAttackTarget,
        ShouldChaseTarget,
        HasInvestigation,
        ShouldInvestigate,
        InvestigationFinished,
        ShouldPatrol,
        PatrolIdleFinished,
        PatrolDestinationReached,
        ShouldUseMelee,
        MeleeActionFinished,
        ShouldUseThrowable,
        ThrowableActionFinished
    }

    [Header("Query")]
    [SerializeField] private Query _query = Query.HasTarget;

    [Header("Update")]
    [Tooltip("If true, awareness is refreshed before evaluating this condition. Keep true for Idle/Move/Attack transitions.")]
    [SerializeField] private bool _refreshAwarenessBeforeQuery = true;

    public Query SelectedQuery => _query;
    public bool RefreshAwarenessBeforeQuery => _refreshAwarenessBeforeQuery;
}

public class EnemyShooterCombatCondition : Condition
{
    private EnemyShooterCombatConditionSO _origin;
    private EnemyShooterCombatController _combat;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterCombatConditionSO)OriginSO;
        _combat = EnemyShooterStateMachineUtility.ResolveCombatController(stateMachine);
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        if (_combat == null)
            return false;

        if (_origin.RefreshAwarenessBeforeQuery)
            _combat.TickAwareness();

        return _origin.SelectedQuery switch
        {
            EnemyShooterCombatConditionSO.Query.HasTarget => _combat.HasTarget,
            EnemyShooterCombatConditionSO.Query.IsAlerted => _combat.IsAlerted,
            EnemyShooterCombatConditionSO.Query.CanSeeTarget => _combat.CanSeeTarget,
            EnemyShooterCombatConditionSO.Query.ShouldChase => _combat.ShouldChase,
            EnemyShooterCombatConditionSO.Query.ShouldAttack => _combat.ShouldAttack,
            EnemyShooterCombatConditionSO.Query.TargetLost => _combat.TargetLost,
            EnemyShooterCombatConditionSO.Query.HasLastKnownPosition => _combat.HasLastKnownPosition,
            EnemyShooterCombatConditionSO.Query.CanAttackTarget => _combat.CanAttackTarget,
            EnemyShooterCombatConditionSO.Query.ShouldChaseTarget => _combat.ShouldChaseTarget,
            EnemyShooterCombatConditionSO.Query.HasInvestigation => _combat.HasInvestigation,
            EnemyShooterCombatConditionSO.Query.ShouldInvestigate => _combat.ShouldInvestigate,
            EnemyShooterCombatConditionSO.Query.InvestigationFinished => _combat.TryCompleteFinishedInvestigation(),
            EnemyShooterCombatConditionSO.Query.ShouldPatrol => _combat.ShouldPatrol,
            EnemyShooterCombatConditionSO.Query.PatrolIdleFinished => _combat.PatrolIdleFinished,
            EnemyShooterCombatConditionSO.Query.PatrolDestinationReached => _combat.PatrolDestinationReached,
            EnemyShooterCombatConditionSO.Query.ShouldUseMelee => _combat.ShouldUseMelee,
            EnemyShooterCombatConditionSO.Query.MeleeActionFinished => _combat.MeleeActionFinished,
            EnemyShooterCombatConditionSO.Query.ShouldUseThrowable => _combat.ShouldUseThrowable,
            EnemyShooterCombatConditionSO.Query.ThrowableActionFinished => _combat.ThrowableActionFinished,
            _ => false
        };
    }
}
