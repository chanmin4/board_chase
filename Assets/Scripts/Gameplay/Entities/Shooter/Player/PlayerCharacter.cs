/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Implements the focused PlayerCharacter responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _feet;

    public Transform Feet => _feet != null ? _feet : transform;

    private Vector2 _inputVector;
    private float _previousSpeed;

    [NonSerialized] public bool DashInput;


    [NonSerialized] public bool extraActionInput;
    [NonSerialized] public bool shootInput;

    [NonSerialized] public Vector3 movementInput;
    [NonSerialized] public Vector3 movementVector;
    [NonSerialized] public ControllerColliderHit lastHit;
    [NonSerialized] public bool isRunning;

    public const float GRAVITY_MULTIPLIER = 5f;
    public const float MAX_FALL_SPEED = -50f;
    public const float MAX_RISE_SPEED = 100f;
    public const float GRAVITY_COMEBACK_MULTIPLIER = 0.03f;
    public const float GRAVITY_DIVIDER = 0.6f;
    public const float AIR_RESISTANCE = 5f;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ShooterStuckRecovery.EnsureOn(this);
    }

    // Purpose: Responds to the controller collider hit callback.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        lastHit = hit;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_inputReader == null)
            return;

        _inputReader.DashEvent += OnDashInitiated;
        _inputReader.DashCanceledEvent += OnDashCanceled;
        _inputReader.MoveEvent += OnMove;
        _inputReader.ShootEvent += OnShootStarted;
        _inputReader.ShootCanceledEvent += OnShootCanceled;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_inputReader == null)
            return;

        _inputReader.DashEvent -= OnDashInitiated;
        _inputReader.DashCanceledEvent -= OnDashCanceled;
        _inputReader.MoveEvent -= OnMove;
        _inputReader.ShootEvent -= OnShootStarted;
        _inputReader.ShootCanceledEvent -= OnShootCanceled;

        shootInput = false;
        DashInput = false;
        _inputVector = Vector2.zero;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        RecalculateMovement();
    }

    // Purpose: Performs the recalculate movement workflow for this component.
    private void RecalculateMovement()
    {
        float targetSpeed = Mathf.Clamp01(_inputVector.magnitude);

        if (targetSpeed > 0f)
        {
            if (isRunning || shootInput)
                targetSpeed = 1f;
        }

        targetSpeed = Mathf.Lerp(
            _previousSpeed,
            targetSpeed,
            Time.deltaTime * 4f);

        Vector3 adjustedMovement = new Vector3(
            _inputVector.x,
            0f,
            _inputVector.y);

        if (adjustedMovement.sqrMagnitude > 1f)
            adjustedMovement.Normalize();

        movementInput = adjustedMovement * targetSpeed;
        _previousSpeed = targetSpeed;
    }

    // Purpose: Responds to the move callback.
    private void OnMove(Vector2 movement)
    {
        _inputVector = movement;
    }

    // Purpose: Responds to the dash initiated callback.
    private void OnDashInitiated()
    {
        DashInput = true;
    }

    // Purpose: Responds to the dash canceled callback.
    private void OnDashCanceled()
    {
        DashInput = false;
    }

    // Purpose: Responds to the shoot started callback.
    private void OnShootStarted()
    {
        shootInput = true;
    }

    // Purpose: Responds to the shoot canceled callback.
    private void OnShootCanceled()
    {
        shootInput = false;
    }
}
