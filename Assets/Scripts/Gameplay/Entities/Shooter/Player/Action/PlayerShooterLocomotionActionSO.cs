/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Action
 * Primary responsibility: Defines PlayerShooterLocomotionActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayerShooterLocomotion",
    menuName = "State Machines/Actions/Shooter/Player Locomotion")]
public class PlayerShooterLocomotionActionSO : StateActionSO<PlayerShooterLocomotionAction>
{
    [Header("Movement")]
    [Tooltip("CharacterController를 지면에 붙여두기 위한 Y축 하강 속도입니다. 이동속도가 아닙니다.")]
    [FormerlySerializedAs("_verticalPull")]
    [SerializeField] private float _downwardVelocity = -10f;

    [Tooltip("true면 플레이어 수평 입력을 강제로 0 처리합니다. Idle state에서 grounded movement만 유지할 때 사용합니다.")]
    [SerializeField] private bool _forceStopHorizontalMovement;

    [Tooltip("true면 이 액션이 CharacterController.Move를 호출합니다.")]
    [SerializeField] private bool _applyMovement = true;

    public float DownwardVelocity => _downwardVelocity;
    public bool ForceStopHorizontalMovement => _forceStopHorizontalMovement;
    public bool ApplyMovement => _applyMovement;
}

public class PlayerShooterLocomotionAction : ShooterLocomotionActionBase
{
    private PlayerCharacter _playerCharacter;
    private CharacterController _characterController;

    private PlayerShooterLocomotionActionSO _originSO =>
        (PlayerShooterLocomotionActionSO)OriginSO;

    // Purpose: Responds to the awake locomotion callback.
    protected override void OnAwakeLocomotion(StateMachine stateMachine)
    {
        _playerCharacter = ResolveComponent<PlayerCharacter>(stateMachine);
        _characterController = ResolveComponent<CharacterController>(stateMachine);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (IsUpdateBlocked())
            return;

        if (TryBuildDashVector(_originSO.DownwardVelocity, out Vector3 dashVector))
        {
            if (_playerCharacter != null)
                _playerCharacter.movementVector = dashVector;

            StopFootTrail();

            if (_originSO.ApplyMovement)
                ApplyCharacterControllerMovement(dashVector);

            return;
        }

        if (_playerCharacter == null)
            return;

        Vector3 input = ResolvePlayerInput();

        if (_originSO.ForceStopHorizontalMovement)
            input = Vector3.zero;

        SetFootTrailByPlanarMotion(input);

        float speed = ResolveMoveSpeed();
        if (_playerCharacter.Stamina != null) speed *= _playerCharacter.Stamina.SpeedMultiplier;

        _playerCharacter.movementVector.x = input.x * speed;
        _playerCharacter.movementVector.y = _originSO.DownwardVelocity;
        _playerCharacter.movementVector.z = input.z * speed;

        if (_originSO.ApplyMovement)
            ApplyCharacterControllerMovement(_playerCharacter.movementVector);
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_playerCharacter != null)
        {
            _playerCharacter.movementVector.x = 0f;
            _playerCharacter.movementVector.z = 0f;
        }

        StopFootTrail();
    }

    // Purpose: Resolves the player input needed by this component.
    private Vector3 ResolvePlayerInput()
    {
        Vector3 input = _playerCharacter.movementInput;
        input.y = 0f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        return input;
    }

    // Purpose: Applies the requested character controller movement change.
    private void ApplyCharacterControllerMovement(Vector3 velocity)
    {
        if (_characterController == null || !_characterController.enabled)
            return;

        ShooterBodyCollisionMotor.MoveWithBodyBlocking(_characterController, velocity * Time.deltaTime);
    }
}
