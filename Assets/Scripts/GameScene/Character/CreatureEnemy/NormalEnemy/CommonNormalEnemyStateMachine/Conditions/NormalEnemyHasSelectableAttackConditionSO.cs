using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NormalEnemyHasSelectableAttackCondition",
    menuName = "State Machines/Enemy Conditions/Normal Enemy Has Selectable Attack")]
public class NormalEnemyHasSelectableAttackConditionSO : StateConditionSO<NormalEnemyHasSelectableAttackCondition>
{
}

public class NormalEnemyHasSelectableAttackCondition : Condition
{
    private Enemy _enemy;
    private EnemyMovementStatsProvider _movementStatsProvider;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        stateMachine.TryGetComponent(out _movementStatsProvider);
    }

    protected override bool Statement()
    {
        if (_enemy == null ||
            _enemy.currentTarget == null ||
            _enemy.currentTarget.IsDead ||
            _movementStatsProvider == null ||
            _movementStatsProvider.EnemyStatConfig == null)
        {
            return false;
        }

        float distance = ResolveTargetDistance();
        return _movementStatsProvider.EnemyStatConfig.HasSelectableAttack(distance);
    }

    private float ResolveTargetDistance()
    {
        Vector3 from = _enemy.transform.position;
        Vector3 to = _enemy.currentTarget.transform.position;

        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to);
    }
}
