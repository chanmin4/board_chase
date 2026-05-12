using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "HasValidNamedNormalAttackRangeCondition",
    menuName = "State Machines/Named Enemy Conditions/Has Valid Normal Attack Range")]
public class HasValidNamedNormalAttackRangeConditionSO
    : StateConditionSO<HasValidNamedNormalAttackRangeCondition>
{
    [SerializeField] private NamedNormalAttackConfigSO _config;

    public NamedNormalAttackConfigSO Config => _config;
}

public class HasValidNamedNormalAttackRangeCondition : Condition
{
    private Enemy _enemy;
    private HasValidNamedNormalAttackRangeConditionSO _origin;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _origin = (HasValidNamedNormalAttackRangeConditionSO)OriginSO;
    }

    protected override bool Statement()
    {
        if (_enemy == null || _enemy.currentTarget == null)
            return false;

        if (_origin.Config == null)
            return false;

        float distance = GetFlatDistance(
            _enemy.transform.position,
            _enemy.currentTarget.transform.position);

        return _origin.Config.HasAnyAttackInRange(distance);
    }

    private static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
