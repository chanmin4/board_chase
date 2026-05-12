using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SelectNamedAttackAction",
    menuName = "State Machines/Named Enemy Actions/Select Attack")]
public class SelectNamedAttackActionSO : StateActionSO<SelectNamedAttackAction>
{
    [SerializeField] private NamedNormalAttackConfigSO _config;

    public NamedNormalAttackConfigSO Config => _config;
}

public class SelectNamedAttackAction : StateAction
{
    private SelectNamedAttackActionSO _origin;
    private NamedEnemyBlackboard _blackboard;
    private Enemy _enemy;
    public override void OnUpdate()
    {
    }
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (SelectNamedAttackActionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
        _enemy = stateMachine.GetComponentInParent<Enemy>();
    }

    public override void OnStateEnter()
    {
        TrySelect();
    }

    private void TrySelect()
    {
        if (_blackboard == null || _enemy == null)
            return;

        if (_blackboard.HasSelectedAttack)
            return;

        if (_enemy.currentTarget == null)
            return;

        if (_origin.Config == null)
            return;

        float distance = GetFlatDistance(
            _enemy.transform.position,
            _enemy.currentTarget.transform.position);

        if (!_origin.Config.TryPickAttack(distance, out NamedEnemyAttackType selected))
            return;

        _blackboard.SelectNormalAttack(selected);
    }

    private static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
