// ShooterAnimatorParameterActionSO.cs
using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ShooterAnimatorParameter",
    menuName = "State Machines/Actions/Shooter/Animator Parameter")]
public class ShooterAnimatorParameterActionSO : StateActionSO<ShooterAnimatorParameterAction>
{
    [Header("Animator")]
    [Tooltip("If true, the action first searches child Animator components. Use this when Animator is on VSplatter_visual/Raider_visual and the root object owns CharacterController/StateMachine.")]
    [SerializeField] private bool _preferChildAnimator = true;

    [Tooltip("Case-insensitive name fragment used when choosing a child Animator. Example: 'visual' matches VSplatter_visual and Raider_visual. If empty, the first child Animator is used.")]
    [SerializeField] private string _preferredAnimatorNameContains = "visual";

    [Header("Parameters")]
    [Tooltip("Float Animator parameter receiving 0..1 normalized movement speed.")]
    [SerializeField] private string _moveSpeedParameter = "MoveSpeed";

    [Tooltip("Float Animator parameter receiving local right/left movement direction. Right is +1, left is -1.")]
    [SerializeField] private string _moveDirXParameter = "MoveDirX";

    [Tooltip("Float Animator parameter receiving local forward/back movement direction. Forward is +1, backward is -1.")]
    [SerializeField] private string _moveDirYParameter = "MoveDirY";

    [Header("Movement")]
    [Tooltip("If true, MoveSpeed is normalized to 0..1. If false, raw planar speed is written.")]
    [SerializeField] private bool _normalizeMoveSpeed = true;

    [Tooltip("Movement below this magnitude is treated as idle and MoveDirX/Y become 0.")]
    [SerializeField, Min(0f)] private float _directionDeadZone = 0.001f;

    [Tooltip("If true, MoveSpeed/MoveDirX/MoveDirY are reset to 0 when the state exits.")]
    [SerializeField] private bool _resetOnStateExit = true;

    [Header("Debug")]
    [Tooltip("Logs missing Animator or missing float parameters once during Awake. Use this when blend tree values do not change.")]
    [SerializeField] private bool _logWarnings = true;

    public bool PreferChildAnimator => _preferChildAnimator;
    public string PreferredAnimatorNameContains => _preferredAnimatorNameContains;
    public string MoveSpeedParameter => _moveSpeedParameter;
    public string MoveDirXParameter => _moveDirXParameter;
    public string MoveDirYParameter => _moveDirYParameter;
    public bool NormalizeMoveSpeed => _normalizeMoveSpeed;
    public float DirectionDeadZone => Mathf.Max(0f, _directionDeadZone);
    public bool ResetOnStateExit => _resetOnStateExit;
    public bool LogWarnings => _logWarnings;
}

public class ShooterAnimatorParameterAction : StateAction
{
    private Transform _root;
    private VSplatter_Character _character;
    private ShooterStatsRuntime _statsRuntime;
    private NavMeshAgent _agent;
    private Animator _animator;

    private int _moveSpeedHash;
    private int _moveDirXHash;
    private int _moveDirYHash;

    private bool _hasMoveSpeed;
    private bool _hasMoveDirX;
    private bool _hasMoveDirY;
    private StateMachine _stateMachine;

    private ShooterAnimatorParameterActionSO _originSO =>
        (ShooterAnimatorParameterActionSO)OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        _stateMachine = stateMachine;
        _root = stateMachine.transform;

        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<VSplatter_Character>(true);

        if (!stateMachine.TryGetComponent(out _statsRuntime))
            _statsRuntime = stateMachine.GetComponentInChildren<ShooterStatsRuntime>(true);

        if (!stateMachine.TryGetComponent(out _agent))
            _agent = stateMachine.GetComponentInChildren<NavMeshAgent>(true);

        _animator = ResolveAnimator(stateMachine);
        CacheParameters();

        if (_originSO.LogWarnings)
            LogBindingWarnings();
    }

    public override void OnUpdate()
    {
        if (_animator == null)
            return;

        Vector3 worldMove = ResolveWorldMoveVector();
        float planarSpeed = new Vector3(worldMove.x, 0f, worldMove.z).magnitude;

        float speedValue = ResolveAnimatorSpeed(planarSpeed);
        Vector2 direction = ResolveLocalDirection(worldMove);

        Apply(speedValue, direction.x, direction.y);
    }

    public override void OnStateExit()
    {
        if (!_originSO.ResetOnStateExit)
            return;

        Apply(0f, 0f, 0f);
    }

    private Vector3 ResolveWorldMoveVector()
    {
        if (_agent != null && _agent.enabled && _agent.gameObject.activeInHierarchy)
            return ResolveAgentMoveVector();

        if (_character != null)
            return ResolveCharacterMoveVector();

        return Vector3.zero;
    }

    private Vector3 ResolveCharacterMoveVector()
    {
        Vector3 input = _character.movementInput;
        input.y = 0f;

        if (input.sqrMagnitude > _originSO.DirectionDeadZone * _originSO.DirectionDeadZone)
            return input.normalized;

        Vector3 movement = _character.movementVector;
        movement.y = 0f;
        return movement;
    }

    private Vector3 ResolveAgentMoveVector()
    {
        Vector3 velocity = _agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > _originSO.DirectionDeadZone * _originSO.DirectionDeadZone)
            return velocity;

        Vector3 desired = _agent.desiredVelocity;
        desired.y = 0f;
        return desired;
    }

    private float ResolveAnimatorSpeed(float planarSpeed)
    {
        if (!_originSO.NormalizeMoveSpeed)
            return planarSpeed;

        if (_character != null)
        {
            Vector3 input = _character.movementInput;
            input.y = 0f;
            return Mathf.Clamp01(input.magnitude);
        }

        if (_agent != null && _agent.speed > 0f)
            return Mathf.Clamp01(planarSpeed / _agent.speed);

        if (_statsRuntime != null && _statsRuntime.MoveSpeed > 0f)
            return Mathf.Clamp01(planarSpeed / _statsRuntime.MoveSpeed);

        return planarSpeed > _originSO.DirectionDeadZone ? 1f : 0f;
    }

    private Vector2 ResolveLocalDirection(Vector3 worldMove)
    {
        worldMove.y = 0f;

        if (worldMove.sqrMagnitude <= _originSO.DirectionDeadZone * _originSO.DirectionDeadZone)
            return Vector2.zero;

        Transform reference = _root != null ? _root : _animator.transform;
        Vector3 local = reference.InverseTransformDirection(worldMove.normalized);

        return new Vector2(
            Mathf.Clamp(local.x, -1f, 1f),
            Mathf.Clamp(local.z, -1f, 1f));
    }

    private void Apply(float moveSpeed, float moveDirX, float moveDirY)
    {
        if (_animator == null)
            return;

        if (_hasMoveSpeed)
            _animator.SetFloat(_moveSpeedHash, moveSpeed);

        if (_hasMoveDirX)
            _animator.SetFloat(_moveDirXHash, moveDirX);

        if (_hasMoveDirY)
            _animator.SetFloat(_moveDirYHash, moveDirY);
    }

    private Animator ResolveAnimator(StateMachine stateMachine)
    {
        if (_originSO.PreferChildAnimator)
        {
            Animator childAnimator = ResolvePreferredChildAnimator(stateMachine.transform);
            if (childAnimator != null)
                return childAnimator;
        }

        if (stateMachine.TryGetComponent(out Animator rootAnimator))
            return rootAnimator;

        return stateMachine.GetComponentInChildren<Animator>(true);
    }

    private Animator ResolvePreferredChildAnimator(Transform root)
    {
        if (root == null)
            return null;

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        string namePart = _originSO.PreferredAnimatorNameContains;

        if (!string.IsNullOrWhiteSpace(namePart))
        {
            string lowered = namePart.ToLowerInvariant();

            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];

                if (animator == null || animator.transform == root)
                    continue;

                if (animator.name.ToLowerInvariant().Contains(lowered))
                    return animator;
            }
        }

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator != null && animator.transform != root)
                return animator;
        }

        return null;
    }

    private void CacheParameters()
    {
        if (_animator == null)
            return;

        _moveSpeedHash = CacheFloatParameter(_originSO.MoveSpeedParameter, out _hasMoveSpeed);
        _moveDirXHash = CacheFloatParameter(_originSO.MoveDirXParameter, out _hasMoveDirX);
        _moveDirYHash = CacheFloatParameter(_originSO.MoveDirYParameter, out _hasMoveDirY);
    }

    private void LogBindingWarnings()
    {
        if (_animator == null)
        {
            Debug.LogWarning(
                $"[ShooterAnimatorParameterAction] Animator not found. owner={_stateMachine?.name}",
                _stateMachine);
            return;
        }

        if (!_hasMoveSpeed)
            LogMissingParameter(_originSO.MoveSpeedParameter);

        if (!_hasMoveDirX)
            LogMissingParameter(_originSO.MoveDirXParameter);

        if (!_hasMoveDirY)
            LogMissingParameter(_originSO.MoveDirYParameter);
    }

    private void LogMissingParameter(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return;

        Debug.LogWarning(
            $"[ShooterAnimatorParameterAction] Missing float parameter '{parameterName}' on animator={_animator.name}, owner={_stateMachine?.name}",
            _animator);
    }

    private int CacheFloatParameter(string parameterName, out bool found)
    {
        found = false;

        if (string.IsNullOrWhiteSpace(parameterName) || _animator == null)
            return 0;

        int hash = Animator.StringToHash(parameterName);
        AnimatorControllerParameter[] parameters = _animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.nameHash == hash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                found = true;
                return hash;
            }
        }

        return hash;
    }
}
