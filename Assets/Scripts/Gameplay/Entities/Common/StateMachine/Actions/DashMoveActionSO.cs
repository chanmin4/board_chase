/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines DashMoveActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "DashMoveAction", menuName = "State Machines/Player Actions/Dash Move")]
public class DashMoveActionSO : StateActionSO<DashMoveAction>
{
}

public class DashMoveAction : StateAction
{
    private PlayerCharacter _character;
    private ShooterDashController _dashController;
    private Transform _cachedTransform;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<PlayerCharacter>(true);

        if (!stateMachine.TryGetComponent(out _dashController))
            _dashController = stateMachine.GetComponentInChildren<ShooterDashController>(true);

        _cachedTransform = stateMachine.transform;
    }

    // Purpose: Responds to the update callback.
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
