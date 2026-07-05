// ShooterRotateActionSO.cs
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum ShooterRotateSource
{
    Auto,
    PlayerAim,
    EnemyCombatTarget,
    MovementInput
}

[CreateAssetMenu(
    fileName = "ShooterRotate",
    menuName = "State Machines/Actions/Shooter/Rotate")]
public class ShooterRotateActionSO : StateActionSO<ShooterRotateAction>
{
    [Header("Source")]
    [Tooltip("Auto: PlayerAim if PlayerAimAction exists, EnemyCombatTarget if EnemyShooterCombatController exists, otherwise MovementInput.")]
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
    [Tooltip("If true, EntityWeaponHolder.UpdateVisualAim is called with the resolved aim/target point.")]
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
    private VSplatter_Character _character;
    private CharacterController _characterController;
    private PlayerAimAction _playerAim;
    private EnemyShooterCombatController _enemyCombat;
    private EntityWeaponHolder _weaponHolder;
    private Collider[] _selfColliders;

    private Vector3 _lastLookDirection;
    private float _turnSmoothSpeed;
    private bool _isSelfScreenLockActive;

    private const float DirectionThreshold = 0.0001f;

    private ShooterRotateActionSO _originSO => (ShooterRotateActionSO)OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        _transform = stateMachine.transform;

        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<VSplatter_Character>(true);

        if (!stateMachine.TryGetComponent(out _characterController))
            _characterController = stateMachine.GetComponentInChildren<CharacterController>(true);

        if (!stateMachine.TryGetComponent(out _playerAim))
            _playerAim = stateMachine.GetComponentInChildren<PlayerAimAction>(true);

        if (!stateMachine.TryGetComponent(out _enemyCombat))
            _enemyCombat = stateMachine.GetComponentInChildren<EnemyShooterCombatController>(true);

        if (!stateMachine.TryGetComponent(out _weaponHolder))
            _weaponHolder = stateMachine.GetComponentInChildren<EntityWeaponHolder>(true);

        _selfColliders = _transform.GetComponentsInChildren<Collider>(true);

        _lastLookDirection = _transform.forward;
        _lastLookDirection.y = 0f;

        if (_lastLookDirection.sqrMagnitude < DirectionThreshold)
            _lastLookDirection = Vector3.forward;

        _lastLookDirection.Normalize();
    }

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
            _weaponHolder?.UpdateVisualAim(lookPoint);
    }

    private bool TryResolveLookPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        ShooterRotateSource source = ResolveSource();

        switch (source)
        {
            case ShooterRotateSource.PlayerAim:
                return TryResolvePlayerAimPoint(out lookPoint);

            case ShooterRotateSource.EnemyCombatTarget:
                return TryResolveEnemyCombatPoint(out lookPoint);

            case ShooterRotateSource.MovementInput:
                return TryResolveMovementInputPoint(out lookPoint);

            default:
                return TryResolvePlayerAimPoint(out lookPoint) ||
                       TryResolveEnemyCombatPoint(out lookPoint) ||
                       TryResolveMovementInputPoint(out lookPoint);
        }
    }

    private ShooterRotateSource ResolveSource()
    {
        if (_originSO.RotateSource != ShooterRotateSource.Auto)
            return _originSO.RotateSource;

        if (_playerAim != null)
            return ShooterRotateSource.PlayerAim;

        if (_enemyCombat != null)
            return ShooterRotateSource.EnemyCombatTarget;

        return ShooterRotateSource.MovementInput;
    }

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

    private bool TryResolveEnemyCombatPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_enemyCombat == null)
            return false;

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

    private Vector3 ResolveBodyPivotPosition()
    {
        if (_character != null && _character.Feet != null)
            return _character.Feet.position;

        return _transform.position;
    }

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
