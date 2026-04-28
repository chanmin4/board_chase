using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "DashMoveAction", menuName = "State Machines/Player Actions/Dash Move")]
public class DashMoveActionSO : StateActionSO<DashMoveAction>
{
}

public class DashMoveAction : StateAction
{
    private VSplatter_Character _character;
    private VSplatterDashController _dashController;
    private Transform _cachedTransform;

    public override void Awake(StateMachine stateMachine)
    {
        _character = stateMachine.GetComponent<VSplatter_Character>();
        _dashController = stateMachine.GetOrAddComponent<VSplatterDashController>();
        _cachedTransform = stateMachine.transform;
    }

    public override void OnUpdate()
    {
        if (_character == null || _dashController == null || !_dashController.IsDashing)
            return;

        _character.movementVector = _dashController.BuildDashMovementVector(_character.movementVector.y);

        if (_dashController.RotateTowardDashDirection)
        {
            Vector3 direction = _dashController.DashDirection;
            if (direction.sqrMagnitude > 0.0001f)
                _cachedTransform.forward = direction;
        }
    }
}
