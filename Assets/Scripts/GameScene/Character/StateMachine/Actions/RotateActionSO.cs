using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "RotateAction", menuName = "State Machines/Actions/Rotate")]
public class RotateActionSO : StateActionSO<RotateAction>
{
    [Tooltip("Smoothing for rotating the character toward the aim direction")]
    public float turnSmoothTime = 0.01f;

    [Tooltip("When the aim point gets closer than this radius, rotation freezes and keeps the last valid direction.")]
    public float aimDeadzoneEnterRadius = 0.45f;

    [Tooltip("Rotation resumes only after the aim point moves outside this radius.")]
    public float aimDeadzoneExitRadius = 0.65f;
}

public class RotateAction : StateAction
{
    private VSplatterAimAction _aimAction;
    private VSplatterWeaponHolder _weaponHolder;
    private Transform _transform;

    private float _turnSmoothSpeed;
    private bool _isAimLocked;
    private Vector3 _lastValidLookDirection;

    private const float ROTATION_THRESHOLD = 0.0001f;

    private RotateActionSO _originSO => (RotateActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _transform = stateMachine.GetComponent<Transform>();
        stateMachine.TryGetComponent(out _aimAction);
        stateMachine.TryGetComponent(out _weaponHolder);

        if (_aimAction == null)
            _aimAction = stateMachine.GetComponentInChildren<VSplatterAimAction>();

        if (_aimAction == null)
            _aimAction = stateMachine.GetComponentInParent<VSplatterAimAction>();

        if (_weaponHolder == null)
            _weaponHolder = stateMachine.GetComponentInChildren<VSplatterWeaponHolder>();

        if (_weaponHolder == null)
            _weaponHolder = stateMachine.GetComponentInParent<VSplatterWeaponHolder>();

        _lastValidLookDirection = _transform.forward;
        _lastValidLookDirection.y = 0f;

        if (_lastValidLookDirection.sqrMagnitude < ROTATION_THRESHOLD)
            _lastValidLookDirection = Vector3.forward;

        _lastValidLookDirection.Normalize();
    }

    public override void OnUpdate()
    {
        if (_aimAction == null || !_aimAction.HasAimPoint)
            return;

        Vector3 pivotPosition = _transform.position;
        if (_weaponHolder != null && _weaponHolder.GameplayFireOrigin != null)
            pivotPosition = _weaponHolder.GameplayFireOrigin.position;

        Vector3 lookDirection = _aimAction.AimWorldPoint - pivotPosition;
        lookDirection.y = 0f;

        float sqrDistance = lookDirection.sqrMagnitude;
        float enterRadius = Mathf.Max(0f, _originSO.aimDeadzoneEnterRadius);
        float exitRadius = Mathf.Max(enterRadius, _originSO.aimDeadzoneExitRadius);

        float enterRadiusSqr = enterRadius * enterRadius;
        float exitRadiusSqr = exitRadius * exitRadius;

        if (_isAimLocked)
        {
            if (sqrDistance > exitRadiusSqr)
                _isAimLocked = false;
        }
        else
        {
            if (sqrDistance < enterRadiusSqr)
                _isAimLocked = true;
        }

        if (!_isAimLocked && sqrDistance >= ROTATION_THRESHOLD)
        {
            _lastValidLookDirection = lookDirection.normalized;
        }

        if (_lastValidLookDirection.sqrMagnitude < ROTATION_THRESHOLD)
            return;

        float targetRotation = Mathf.Atan2(_lastValidLookDirection.x, _lastValidLookDirection.z) * Mathf.Rad2Deg;

        _transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
            _transform.eulerAngles.y,
            targetRotation,
            ref _turnSmoothSpeed,
            _originSO.turnSmoothTime);
    }
}
