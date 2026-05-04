using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "RotateAction", menuName = "State Machines/Actions/Rotate")]
public class RotateActionSO : StateActionSO<RotateAction>
{
    [Tooltip("Smoothing for rotating the character toward the aim direction")]
    public float turnSmoothTime = 0.01f;
    [Tooltip("Padding in pixels for entering the self-lock zone.")]
    public float selfScreenLockEnterPadding = 50f;

    [Tooltip("Padding in pixels for leaving the self-lock zone.")]
    public float selfScreenLockExitPadding = 60f;
    [Tooltip("Extra yaw offset applied to the body rotation, in degrees.")]
    public float bodyYawOffsetDegrees = -5f;
}

public class RotateAction : StateAction
{
    private VSplatterAimAction _aimAction;
    private VSplatterWeaponHolder _weaponHolder;
    private Transform _transform;
    private VSplatter_Character _character;

    private CharacterController _characterController;
    private Collider[] _selfColliders;
    private bool _isSelfScreenLockActive;

    private float _turnSmoothSpeed;
    private Vector3 _lastValidLookDirection;

    private const float ROTATION_THRESHOLD = 0.0001f;

    private RotateActionSO _originSO => (RotateActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _transform = stateMachine.GetComponent<Transform>();
        stateMachine.TryGetComponent(out _aimAction);
        stateMachine.TryGetComponent(out _weaponHolder);
        stateMachine.TryGetComponent(out _character);
        stateMachine.TryGetComponent(out _characterController);

        if (_aimAction == null)
            _aimAction = stateMachine.GetComponentInChildren<VSplatterAimAction>() ??
                        stateMachine.GetComponentInParent<VSplatterAimAction>();

        if (_weaponHolder == null)
            _weaponHolder = stateMachine.GetComponentInChildren<VSplatterWeaponHolder>() ??
                            stateMachine.GetComponentInParent<VSplatterWeaponHolder>();

        if (_character == null)
            _character = stateMachine.GetComponentInChildren<VSplatter_Character>() ??
                         stateMachine.GetComponentInParent<VSplatter_Character>();

        if (_characterController == null)
            _characterController = stateMachine.GetComponentInChildren<CharacterController>() ??
                                stateMachine.GetComponentInParent<CharacterController>();

        _selfColliders = _transform.GetComponentsInChildren<Collider>(true);

        _lastValidLookDirection = _transform.forward;
        _lastValidLookDirection.y = 0f;

        if (_lastValidLookDirection.sqrMagnitude < ROTATION_THRESHOLD)
            _lastValidLookDirection = Vector3.forward;

        _lastValidLookDirection.Normalize();
    }
    public override void OnUpdate()
    {
        if (_aimAction == null)
            return;

        Camera aimCamera = _aimAction.AimCamera != null ? _aimAction.AimCamera : Camera.main;
        if (aimCamera == null)
            return;

        Vector3 bodyPivotPosition = _character != null && _character.Feet != null
            ? _character.Feet.position
            : _transform.position;

        if (!VSplatterAimUtility.TryGetAimPointOnPlane(aimCamera, bodyPivotPosition.y, out Vector3 bodyAimPoint))
            return;

        bool insideEnter = IsMouseInsideSelfScreenBounds(aimCamera, _originSO.selfScreenLockEnterPadding);
        bool insideExit = IsMouseInsideSelfScreenBounds(aimCamera, _originSO.selfScreenLockExitPadding);

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

        if (_isSelfScreenLockActive)
            return;

        Vector3 lookDirection = bodyAimPoint - bodyPivotPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < ROTATION_THRESHOLD)
            return;

        _lastValidLookDirection = lookDirection.normalized;

       float targetRotation =
        Mathf.Atan2(_lastValidLookDirection.x, _lastValidLookDirection.z) * Mathf.Rad2Deg
        + _originSO.bodyYawOffsetDegrees;

        _transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
            _transform.eulerAngles.y,
            targetRotation,
            ref _turnSmoothSpeed,
            _originSO.turnSmoothTime);

        Vector3 visualAimPoint = _aimAction.HasAimPoint ? _aimAction.AimWorldPoint : bodyAimPoint;
        _weaponHolder?.UpdateVisualAim(visualAimPoint);
    }
    private bool IsMouseInsideSelfScreenBounds(Camera camera, float paddingPixels)
    {
        if (!TryGetSelfBounds(out Bounds bounds))
            return false;

        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(max.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

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
            Collider c = _selfColliders[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            if (!found)
            {
                bounds = c.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return found;
    }

}
