// ShooterLocomotionActionSO.cs
using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ShooterLocomotion",
    menuName = "State Machines/Actions/Shooter/Locomotion")]
public class ShooterLocomotionActionSO : StateActionSO<ShooterLocomotionAction>
{
    [Header("Movement")]
    [Tooltip("Downward velocity written to VSplatter_Character.movementVector.y so CharacterController stays grounded. This is not movement speed.")]
    [FormerlySerializedAs("_verticalPull")]
    [SerializeField] private float _downwardVelocity = -10f;

    [Tooltip("If true, horizontal input is ignored. Use this for idle states that still need grounded CharacterController movement.")]
    [SerializeField] private bool _forceStopHorizontalMovement;

    [Tooltip("If true, this action calls CharacterController.Move. Disable only when another action applies movement.")]
    [SerializeField] private bool _applyMovement = true;

    [Tooltip("Player only. If PlayerAimAction exists, aim movement speed multiplier is applied while aiming.")]
    [SerializeField] private bool _applyAimMoveSpeedMultiplier = true;

    public float DownwardVelocity => _downwardVelocity;
    public bool ForceStopHorizontalMovement => _forceStopHorizontalMovement;
    public bool ApplyMovement => _applyMovement;
    public bool ApplyAimMoveSpeedMultiplier => _applyAimMoveSpeedMultiplier;
}

public class ShooterLocomotionAction : StateAction
{
    private VSplatter_Character _character;
    private CharacterController _characterController;
    private ShooterStatsRuntime _statsRuntime;
    private PlayerAimAction _aimAction;
    private ShooterEffectController _effects;
    private VSplatterDashController _playerDashController;

    private ShooterLocomotionActionSO _originSO => (ShooterLocomotionActionSO)OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<VSplatter_Character>(true);

        if (!stateMachine.TryGetComponent(out _characterController))
            _characterController = stateMachine.GetComponentInChildren<CharacterController>(true);

        if (!stateMachine.TryGetComponent(out _statsRuntime))
            _statsRuntime = stateMachine.GetComponentInChildren<ShooterStatsRuntime>(true);

        if (!stateMachine.TryGetComponent(out _aimAction))
            _aimAction = stateMachine.GetComponentInChildren<PlayerAimAction>(true);

        if (!stateMachine.TryGetComponent(out _effects))
            _effects = stateMachine.GetComponentInChildren<ShooterEffectController>(true);

        if (!stateMachine.TryGetComponent(out _playerDashController))
            _playerDashController = stateMachine.GetComponentInChildren<VSplatterDashController>(true);
    }

    public override void OnUpdate()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
            return;

        if (_character == null)
            return;

        Vector3 input = ResolveMovementInput();

        if (_originSO.ForceStopHorizontalMovement)
            input = Vector3.zero;

        bool isDashing = _playerDashController != null && _playerDashController.IsDashing;
        _effects?.SetFootTrailActive(input.sqrMagnitude > 0.0001f && !isDashing);

        float speed = ResolveMoveSpeed();

        _character.movementVector.x = input.x * speed;
        _character.movementVector.y = _originSO.DownwardVelocity;
        _character.movementVector.z = input.z * speed;

        if (!_originSO.ApplyMovement)
            return;

        if (_characterController == null || !_characterController.enabled)
            return;

        _characterController.Move(_character.movementVector * Time.deltaTime);
    }

    public override void OnStateExit()
    {
        if (_character == null)
            return;

        _character.movementVector.x = 0f;
        _character.movementVector.z = 0f;
        _effects?.SetFootTrailActive(false);
    }

    private Vector3 ResolveMovementInput()
    {
        Vector3 input = _character.movementInput;
        input.y = 0f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        return input;
    }

    private float ResolveMoveSpeed()
    {
        if (_statsRuntime == null)
            return 0f;

        float speed = _statsRuntime.MoveSpeed;

        if (_originSO.ApplyAimMoveSpeedMultiplier && _aimAction != null)
            speed *= _aimAction.MoveSpeedMultiplier;

        return Mathf.Max(0f, speed);
    }
}
