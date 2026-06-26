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
        HasLastKnownPosition
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

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterCombatConditionSO)OriginSO;
        _combat = stateMachine.GetOrAddComponent<EnemyShooterCombatController>();
    }

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
            _ => false
        };
    }
}
