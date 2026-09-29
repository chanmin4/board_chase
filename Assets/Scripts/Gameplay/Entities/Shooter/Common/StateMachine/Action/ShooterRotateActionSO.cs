/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > StateMachine > Action
 * Primary responsibility: Implements the focused ShooterRotateSource responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// ShooterRotateActionSO.cs
using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum ShooterRotateSource
{
    Auto,
    PlayerAim,
    EnemyGoalAware,
    EnemyCombatTarget,
    MovementInput,
    NavMeshMovement
}

[CreateAssetMenu(
    fileName = "ShooterRotate",
    menuName = "State Machines/Actions/Shooter/Rotate")]
public class ShooterRotateActionSO : StateActionSO<ShooterRotateAction>
{
    [Header("Source")]
    [Tooltip("Auto: PlayerAim if PlayerAimController exists, EnemyGoalAware if EnemyShooterCombatController exists, otherwise MovementInput/NavMeshMovement.")]
    [SerializeField] private ShooterRotateSource _rotateSource = ShooterRotateSource.Auto;

    [Header("Rotation")]
    [Tooltip("SmoothDamp time for body yaw. 0 snaps instantly.")]
    [SerializeField, Min(0f)] private float _turnSmoothTime = 0.01f;

    [Tooltip("If true, the first valid direction snaps instead of smoothing from an invalid previous direction.")]
    [SerializeField] private bool _snapWhenNoPreviousDirection = true;

    [Tooltip("Extra yaw offset in degrees. Use this when the mesh/model forward axis is slightly offset from gameplay forward.")]
    [SerializeField] private float _bodyYawOffsetDegrees = -5f;

    [Header("Player Self Lock")]
    [Tooltip("Player only. Prevents rotation updates when the mouse is inside the player's screen bounds.")]
    [SerializeField] private bool _usePlayerSelfScreenLock = true;

    [Tooltip("Screen-space padding in pixels for entering the self-lock area.")]
    [SerializeField] private float _selfScreenLockEnterPadding = 50f;

    [Tooltip("Screen-space padding in pixels for leaving the self-lock area.")]
    [SerializeField] private float _selfScreenLockExitPadding = 60f;

    [Header("Weapon View")]
    [Tooltip("If true, ShooterEquipmentController.UpdateVisualAim is called with the resolved aim/target point.")]
    [SerializeField] private bool _updateWeaponVisualAim = true;

    public ShooterRotateSource RotateSource => _rotateSource;
    public float TurnSmoothTime => Mathf.Max(0f, _turnSmoothTime);
    public bool SnapWhenNoPreviousDirection => _snapWhenNoPreviousDirection;
    public float BodyYawOffsetDegrees => _bodyYawOffsetDegrees;
    public bool UsePlayerSelfScreenLock => _usePlayerSelfScreenLock;
    public float SelfScreenLockEnterPadding => Mathf.Max(0f, _selfScreenLockEnterPadding);
    public float SelfScreenLockExitPadding => Mathf.Max(0f, _selfScreenLockExitPadding);
    public bool UpdateWeaponVisualAim => _updateWeaponVisualAim;
}

public class ShooterRotateAction : StateAction
{
    private Transform _transform;
    private PlayerCharacter _character;
    private CharacterController _characterController;
    private NavMeshAgent _navMeshAgent;
    private PlayerAimController _playerAim;
    private EnemyShooterCombatController _enemyCombat;
    private ShooterEquipmentController _equipmentController;
    private Collider[] _selfColliders;

    private Vector3 _lastLookDirection;
    private float _turnSmoothSpeed;
    private bool _isSelfScreenLockActive;

    private const float DirectionThreshold = 0.0001f;

    private ShooterRotateActionSO _originSO => (ShooterRotateActionSO)OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        _transform = stateMachine.transform;

        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<PlayerCharacter>(true);

        if (!stateMachine.TryGetComponent(out _characterController))
            _characterController = stateMachine.GetComponentInChildren<CharacterController>(true);

        if (!stateMachine.TryGetComponent(out _navMeshAgent))
            _navMeshAgent = stateMachine.GetComponentInChildren<NavMeshAgent>(true);

        if (_navMeshAgent == null)
            _navMeshAgent = stateMachine.GetComponentInParent<NavMeshAgent>();

        if (!stateMachine.TryGetComponent(out _playerAim))
            _playerAim = stateMachine.GetComponentInChildren<PlayerAimController>(true);

        if (!stateMachine.TryGetComponent(out _enemyCombat))
            _enemyCombat = stateMachine.GetComponentInChildren<EnemyShooterCombatController>(true);

        if (!stateMachine.TryGetComponent(out _equipmentController))
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(stateMachine);

        _selfColliders = _transform.GetComponentsInChildren<Collider>(true);

        _lastLookDirection = _transform.forward;
        _lastLookDirection.y = 0f;

        if (_lastLookDirection.sqrMagnitude < DirectionThreshold)
            _lastLookDirection = Vector3.forward;

        _lastLookDirection.Normalize();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            _turnSmoothSpeed = 0f;
            return;
        }

        if (_transform == null)
            return;

        if (!TryResolveLookPoint(out Vector3 lookPoint))
            return;

        Vector3 pivot = ResolveBodyPivotPosition();
        Vector3 lookDirection = lookPoint - pivot;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < DirectionThreshold)
            return;

        _lastLookDirection = lookDirection.normalized;

        ApplyRotation(_lastLookDirection);

        if (_originSO.UpdateWeaponVisualAim)
            _equipmentController?.UpdateVisualAim(lookPoint);
    }

    // Purpose: Attempts to resolve look point and reports whether the request succeeded.
    private bool TryResolveLookPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        ShooterRotateSource source = ResolveSource();

        switch (source)
        {
            case ShooterRotateSource.PlayerAim:
                return TryResolvePlayerAimPoint(out lookPoint);

            case ShooterRotateSource.EnemyGoalAware:
                return TryResolveEnemyGoalPoint(out lookPoint);

            case ShooterRotateSource.EnemyCombatTarget:
                return TryResolveEnemyCombatPoint(out lookPoint);

            case ShooterRotateSource.MovementInput:
                return TryResolveMovementInputPoint(out lookPoint);

            case ShooterRotateSource.NavMeshMovement:
                return TryResolveNavMeshMovementPoint(out lookPoint);

            default:
                return TryResolvePlayerAimPoint(out lookPoint) ||
                       TryResolveEnemyGoalPoint(out lookPoint) ||
                       TryResolveEnemyCombatPoint(out lookPoint) ||
                       TryResolveMovementInputPoint(out lookPoint) ||
                       TryResolveNavMeshMovementPoint(out lookPoint);
        }
    }

    // Purpose: Resolves the source needed by this component.
    private ShooterRotateSource ResolveSource()
    {
        if (_originSO.RotateSource != ShooterRotateSource.Auto)
            return _originSO.RotateSource;

        if (_playerAim != null)
            return ShooterRotateSource.PlayerAim;

        if (_enemyCombat != null)
            return ShooterRotateSource.EnemyGoalAware;

        return ShooterRotateSource.MovementInput;
    }

    // Purpose: Attempts to resolve enemy goal point and reports whether the request succeeded.
    private bool TryResolveEnemyGoalPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        return _enemyCombat != null &&
               _enemyCombat.TryGetCurrentLookPoint(out lookPoint);
    }

    // Purpose: Attempts to resolve player aim point and reports whether the request succeeded.
    private bool TryResolvePlayerAimPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_playerAim == null)
            return false;

        Camera aimCamera = _playerAim.AimCamera != null
            ? _playerAim.AimCamera
            : Camera.main;

        if (aimCamera == null)
            return false;

        if (_originSO.UsePlayerSelfScreenLock && IsMouseSelfLocked(aimCamera))
            return false;

        if (_playerAim.HasAimPoint)
        {
            lookPoint = _playerAim.AimWorldPoint;
            return true;
        }

        Vector3 pivot = ResolveBodyPivotPosition();

        if (!VSplatterAimUtility.TryGetAimPointOnPlane(
                aimCamera,
                pivot.y,
                out lookPoint))
        {
            return false;
        }

        return true;
    }

    // Purpose: Attempts to resolve enemy combat point and reports whether the request succeeded.
    private bool TryResolveEnemyCombatPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_enemyCombat == null)
            return false;

        if (_enemyCombat.TryGetCurrentLookPoint(out lookPoint))
            return true;

        if (_enemyCombat.HasTarget && _enemyCombat.Target != null)
        {
            lookPoint = _enemyCombat.Target.position;
            return true;
        }

        if (_enemyCombat.HasLastKnownPosition)
        {
            lookPoint = _enemyCombat.LastKnownPosition;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to resolve nav mesh movement point and reports whether the request succeeded.
    private bool TryResolveNavMeshMovementPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled)
            return false;

        if (TryNormalizePlanar(_navMeshAgent.desiredVelocity, out Vector3 direction) ||
            TryNormalizePlanar(_navMeshAgent.velocity, out direction) ||
            (_navMeshAgent.hasPath &&
             TryNormalizePlanar(_navMeshAgent.steeringTarget - ResolveBodyPivotPosition(), out direction)))
        {
            lookPoint = ResolveBodyPivotPosition() + direction;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to resolve movement input point and reports whether the request succeeded.
    private bool TryResolveMovementInputPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_character == null)
            return false;

        Vector3 input = _character.movementInput;
        input.y = 0f;

        if (input.sqrMagnitude < DirectionThreshold)
            return false;

        lookPoint = ResolveBodyPivotPosition() + input.normalized;
        return true;
    }

    // Purpose: Resolves the body pivot position needed by this component.
    private Vector3 ResolveBodyPivotPosition()
    {
        if (_character != null && _character.Feet != null)
            return _character.Feet.position;

        return _transform.position;
    }

    // Purpose: Attempts to normalize planar and reports whether the request succeeded.
    private static bool TryNormalizePlanar(Vector3 value, out Vector3 direction)
    {
        value.y = 0f;

        if (value.sqrMagnitude <= DirectionThreshold)
        {
            direction = default;
            return false;
        }

        direction = value.normalized;
        return true;
    }

    // Purpose: Applies the requested rotation change.
    private void ApplyRotation(Vector3 direction)
    {
        float targetYaw =
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg +
            _originSO.BodyYawOffsetDegrees;

        if (_originSO.TurnSmoothTime <= 0f ||
            (_originSO.SnapWhenNoPreviousDirection && _lastLookDirection.sqrMagnitude < DirectionThreshold))
        {
            _transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            return;
        }

        float yaw = Mathf.SmoothDampAngle(
            _transform.eulerAngles.y,
            targetYaw,
            ref _turnSmoothSpeed,
            _originSO.TurnSmoothTime);

        _transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // Purpose: Checks whether mouse self locked is currently true.
    private bool IsMouseSelfLocked(Camera camera)
    {
        if (camera == null)
            return false;

        bool insideEnter = IsMouseInsideSelfScreenBounds(
            camera,
            _originSO.SelfScreenLockEnterPadding);

        bool insideExit = IsMouseInsideSelfScreenBounds(
            camera,
            _originSO.SelfScreenLockExitPadding);

        if (_isSelfScreenLockActive)
        {
            if (!insideExit)
                _isSelfScreenLockActive = false;
        }
        else
        {
            if (insideEnter)
                _isSelfScreenLockActive = true;
        }

        return _isSelfScreenLockActive;
    }

    // Purpose: Checks whether mouse inside self screen bounds is currently true.
    private bool IsMouseInsideSelfScreenBounds(Camera camera, float paddingPixels)
    {
        if (!TryGetSelfBounds(out Bounds bounds))
            return false;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners =
        {
            new(min.x, min.y, min.z),
            new(max.x, min.y, min.z),
            new(min.x, max.y, min.z),
            new(max.x, max.y, min.z),
            new(min.x, min.y, max.z),
            new(max.x, min.y, max.z),
            new(min.x, max.y, max.z),
            new(max.x, max.y, max.z)
        };

        bool anyVisible = false;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);

            if (screen.z <= 0f)
                continue;

            anyVisible = true;
            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxY = Mathf.Max(maxY, screen.y);
        }

        if (!anyVisible)
            return false;

        Rect rect = Rect.MinMaxRect(
            minX - paddingPixels,
            minY - paddingPixels,
            maxX + paddingPixels,
            maxY + paddingPixels);

        return rect.Contains(Input.mousePosition);
    }

    // Purpose: Attempts to get self bounds and reports whether the request succeeded.
    private bool TryGetSelfBounds(out Bounds bounds)
    {
        if (_characterController != null)
        {
            bounds = _characterController.bounds;
            return true;
        }

        bounds = default;
        bool found = false;

        if (_selfColliders == null)
            return false;

        for (int i = 0; i < _selfColliders.Length; i++)
        {
            Collider collider = _selfColliders[i];

            if (collider == null || !collider.enabled || collider.isTrigger)
                continue;

            if (!found)
            {
                bounds = collider.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return found;
    }
}
