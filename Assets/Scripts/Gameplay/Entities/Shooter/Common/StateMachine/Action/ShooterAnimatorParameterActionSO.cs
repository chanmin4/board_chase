/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > StateMachine > Action
 * Primary responsibility: Defines ShooterAnimatorParameterActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    [Tooltip("Searches child visuals before checking the state-machine object. Shooter Animators should normally live on the visual child.")]
    [SerializeField] private bool _preferChildAnimator = true;

    [Tooltip("When several child Animators exist, prefer the one whose GameObject name contains this text.")]
    [SerializeField] private string _preferredAnimatorNameContains = "visual";

    [Header("Parameters")]
    [Tooltip("Animator float updated from the shooter's normalized movement speed.")]
    [SerializeField] private string _moveSpeedParameter = "MoveSpeed";

    [Tooltip("Animator float updated from local left/right movement direction.")]
    [SerializeField] private string _moveDirXParameter = "MoveDirX";

    [Tooltip("Animator float updated from local backward/forward movement direction.")]
    [SerializeField] private string _moveDirYParameter = "MoveDirY";

    [Tooltip("Animator trigger raised once per accepted ShooterMeleeAttack. Character and weapon-socket animation stays under the shared Shooter Animator Controller.")]
    [SerializeField] private string _meleeAttackTriggerParameter = "MeleeAttack";

    [Header("Movement")]
    [SerializeField] private bool _normalizeMoveSpeed = true;
    [SerializeField, Min(0f)] private float _directionDeadZone = 0.001f;
    [SerializeField] private bool _resetOnStateExit = true;

    [Header("Debug")]
    [Tooltip("Logs every MeleeAttack trigger sent to the resolved visual Animator.")]
    [SerializeField] private bool _debugMeleeTrigger;

    public bool PreferChildAnimator => _preferChildAnimator;
    public string PreferredAnimatorNameContains => _preferredAnimatorNameContains;
    public string MoveSpeedParameter => _moveSpeedParameter;
    public string MoveDirXParameter => _moveDirXParameter;
    public string MoveDirYParameter => _moveDirYParameter;
    public string MeleeAttackTriggerParameter => _meleeAttackTriggerParameter;
    public bool NormalizeMoveSpeed => _normalizeMoveSpeed;
    public float DirectionDeadZone => Mathf.Max(0f, _directionDeadZone);
    public bool ResetOnStateExit => _resetOnStateExit;
    public bool DebugMeleeTrigger => _debugMeleeTrigger;
}

public class ShooterAnimatorParameterAction : StateAction
{
    private Transform _root;
    private PlayerCharacter _character;
    private ShooterStatsRuntime _statsRuntime;
    private NavMeshAgent _agent;
    private Animator _animator;
    private ShooterMeleeAttack _meleeAttack;

    private int _moveSpeedHash;
    private int _moveDirXHash;
    private int _moveDirYHash;
    private int _meleeAttackTriggerHash;

    private bool _hasMoveSpeed;
    private bool _hasMoveDirX;
    private bool _hasMoveDirY;
    private bool _hasMeleeAttackTrigger;
    private int _observedMeleeAttackSequence;

    private ShooterAnimatorParameterActionSO _originSO =>
        (ShooterAnimatorParameterActionSO)OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        _root = stateMachine.transform;

        if (!stateMachine.TryGetComponent(out _character))
            _character = stateMachine.GetComponentInChildren<PlayerCharacter>(true);

        if (!stateMachine.TryGetComponent(out _statsRuntime))
            _statsRuntime = stateMachine.GetComponentInChildren<ShooterStatsRuntime>(true);

        if (!stateMachine.TryGetComponent(out _agent))
            _agent = stateMachine.GetComponentInChildren<NavMeshAgent>(true);

        _meleeAttack = EntityComponentResolver.Resolve<ShooterMeleeAttack>(stateMachine);
        _observedMeleeAttackSequence = _meleeAttack != null
            ? _meleeAttack.AttackSequence
            : 0;

        _animator = ResolveAnimator(stateMachine);
        CacheParameters();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_animator == null)
            return;

        Vector3 worldMove = ResolveWorldMoveVector();
        float planarSpeed = new Vector3(worldMove.x, 0f, worldMove.z).magnitude;

        float speedValue = ResolveAnimatorSpeed(planarSpeed);
        Vector2 direction = ResolveLocalDirection(worldMove);

        Apply(speedValue, direction.x, direction.y);
        ApplyPendingMeleeAttackTrigger();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (!_originSO.ResetOnStateExit)
            return;

        Apply(0f, 0f, 0f);
    }

    // Purpose: Resolves the world move vector needed by this component.
    private Vector3 ResolveWorldMoveVector()
    {
        if (_character != null)
        {
            Vector3 input = _character.movementInput;
            input.y = 0f;

            if (input.sqrMagnitude > _originSO.DirectionDeadZone * _originSO.DirectionDeadZone)
                return input.normalized;

            Vector3 movement = _character.movementVector;
            movement.y = 0f;
            return movement;
        }

        if (_agent != null)
        {
            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > _originSO.DirectionDeadZone * _originSO.DirectionDeadZone)
                return velocity;

            Vector3 desired = _agent.desiredVelocity;
            desired.y = 0f;
            return desired;
        }

        return Vector3.zero;
    }

    // Purpose: Resolves the animator speed needed by this component.
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

    // Purpose: Resolves the local direction needed by this component.
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

    // Purpose: Applies the requested apply change.
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

    // Purpose: Resolves the animator needed by this component.
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

    // Purpose: Resolves the preferred child animator needed by this component.
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

    // Purpose: Caches parameters for later reuse.
    private void CacheParameters()
    {
        if (_animator == null)
            return;

        _moveSpeedHash = CacheFloatParameter(_originSO.MoveSpeedParameter, out _hasMoveSpeed);
        _moveDirXHash = CacheFloatParameter(_originSO.MoveDirXParameter, out _hasMoveDirX);
        _moveDirYHash = CacheFloatParameter(_originSO.MoveDirYParameter, out _hasMoveDirY);
        _meleeAttackTriggerHash = CacheTriggerParameter(
            _originSO.MeleeAttackTriggerParameter,
            out _hasMeleeAttackTrigger);
    }

    // Purpose: Applies the requested pending melee attack trigger change.
    private void ApplyPendingMeleeAttackTrigger()
    {
        if (_meleeAttack == null)
            return;

        int sequence = _meleeAttack.AttackSequence;

        if (sequence == _observedMeleeAttackSequence)
            return;

        _observedMeleeAttackSequence = sequence;

        if (_animator == null || !_hasMeleeAttackTrigger)
            return;

        _animator.ResetTrigger(_meleeAttackTriggerHash);
        _animator.SetTrigger(_meleeAttackTriggerHash);

        if (_originSO.DebugMeleeTrigger)
        {
            Debug.Log(
                $"[ShooterAnimatorParameterAction] MeleeAttack trigger raised. " +
                $"animator={_animator.name}, owner={_root.name}, sequence={sequence}",
                _animator);
        }
    }

    // Purpose: Caches float parameter for later reuse.
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

    // Purpose: Caches trigger parameter for later reuse.
    private int CacheTriggerParameter(string parameterName, out bool found)
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
                parameter.type == AnimatorControllerParameterType.Trigger)
            {
                found = true;
                return hash;
            }
        }

        Debug.LogWarning(
            $"[ShooterAnimatorParameterAction] Trigger parameter '{parameterName}' is missing on animator '{_animator.name}'.",
            _animator);

        return hash;
    }
}
