/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Implements the focused NamedTargetDistanceCheckMode responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum NamedTargetDistanceCheckMode
{
    LessOrEqual,
    GreaterOrEqual,
    Between
}

[CreateAssetMenu(
    fileName = "NamedTargetDistanceCondition",
    menuName = "State Machines/Named Enemy Conditions/Target Distance")]
public class NamedTargetDistanceConditionSO : StateConditionSO<NamedTargetDistanceCondition>
{
    [SerializeField] private NamedTargetDistanceCheckMode _mode = NamedTargetDistanceCheckMode.LessOrEqual;

    [Header("Distance")]
    [SerializeField, Min(0f)] private float _distance = 10f;
    [SerializeField, Min(0f)] private float _minDistance = 0f;
    [SerializeField, Min(0f)] private float _maxDistance = 10f;

    public NamedTargetDistanceCheckMode Mode => _mode;
    public float Distance => _distance;
    public float MinDistance => _minDistance;
    public float MaxDistance => _maxDistance;
}

public class NamedTargetDistanceCondition : Condition
{
    private Enemy _enemy;
    private NamedTargetDistanceConditionSO _origin;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _origin = (NamedTargetDistanceConditionSO)OriginSO;
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        if (_enemy == null || _enemy.currentTarget == null)
            return false;

        float distance = GetFlatDistance(
            _enemy.transform.position,
            _enemy.currentTarget.transform.position);

        switch (_origin.Mode)
        {
            case NamedTargetDistanceCheckMode.LessOrEqual:
                return distance <= _origin.Distance;

            case NamedTargetDistanceCheckMode.GreaterOrEqual:
                return distance >= _origin.Distance;

            case NamedTargetDistanceCheckMode.Between:
                float min = Mathf.Min(_origin.MinDistance, _origin.MaxDistance);
                float max = Mathf.Max(_origin.MinDistance, _origin.MaxDistance);
                return distance >= min && distance <= max;
        }

        return false;
    }

    // Purpose: Returns the current flat distance.
    private static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
