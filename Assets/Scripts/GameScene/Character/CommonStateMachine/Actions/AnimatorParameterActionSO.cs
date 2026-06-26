// Assets/Scripts/GameScene/Character/CommonStateMachine/Actions/AnimatorParameterActionSO.cs

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
using Moment = VSplatter.StateMachine.StateAction.SpecificMoment;

/// <summary>
/// StateMachine action that sets an Animator parameter.
/// If the owner prefab has no Animator yet, this action safely skips instead of breaking enemy spawn.
/// </summary>
[CreateAssetMenu(
    fileName = "AnimatorParameterAction",
    menuName = "State Machines/Actions/Set Animator Parameter")]
public class AnimatorParameterActionSO : StateActionSO
{
    [Header("Parameter")]
    public ParameterType parameterType = default;
    public string parameterName = default;

    [Header("Constant Value")]
    public bool boolValue = default;
    public int intValue = default;
    public float floatValue = default;

    [Header("Value Source")]
    public ValueSource valueSource = ValueSource.Constant;
    public bool usePlanarSpeed = true;
    public float speedMultiplier = 1f;
    public float isMovingThreshold = 0.05f;

    [Header("Timing")]
    public Moment whenToRun = default;

    protected override StateAction CreateAction()
    {
        int hash = string.IsNullOrWhiteSpace(parameterName)
            ? 0
            : Animator.StringToHash(parameterName);

        return new AnimatorParameterAction(hash);
    }

    public enum ParameterType
    {
        Bool,
        Int,
        Float,
        Trigger,
    }

    public enum ValueSource
    {
        Constant,
        MovementSpeed,
        IsMovingFromSpeed,
    }
}

public class AnimatorParameterAction : StateAction
{
    private Animator _animator;
    private VSplatter_Character _vsplatter;
    private NavMeshAgent _agent;
    private CharacterController _characterController;
    private Rigidbody _rigidbody;
    private Transform _ownerTransform;

    private AnimatorParameterActionSO _originSO => (AnimatorParameterActionSO)OriginSO;

    private readonly int _parameterHash;
    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;
    private bool _canRun;

    public AnimatorParameterAction(int parameterHash)
    {
        _parameterHash = parameterHash;
    }

    public override void Awake(StateMachine stateMachine)
    {
        _canRun = false;

        if (stateMachine == null)
            return;

        _ownerTransform = stateMachine.transform;
        stateMachine.TryGetComponent(out _agent);
        stateMachine.TryGetComponent(out _vsplatter);
        stateMachine.TryGetComponent(out _characterController);
        stateMachine.TryGetComponent(out _rigidbody);

        if (string.IsNullOrWhiteSpace(_originSO.parameterName))
        {
            Debug.LogWarning(
                $"[AnimatorParameterAction] Parameter name is empty. Action skipped. owner={stateMachine.name}",
                stateMachine);
            return;
        }

        _animator = ResolveInOwnerHierarchy<Animator>(stateMachine);

        if (_animator == null)
        {
            Debug.LogWarning(
                $"[AnimatorParameterAction] Animator not found. Action skipped. owner={stateMachine.name}, parameter={_originSO.parameterName}",
                stateMachine);
            return;
        }

        if (!HasAnimatorParameter(_animator, _parameterHash, ResolveExpectedParameterType()))
        {
            return;
        }

        _canRun = true;
    }

    public override void OnStateEnter()
    {
        _hasPreviousPosition = false;

        if (_originSO.whenToRun == SpecificMoment.OnStateEnter)
            SetParameter();
    }

    public override void OnStateExit()
    {
        if (_originSO.whenToRun == SpecificMoment.OnStateExit)
            SetParameter();
    }

    public override void OnUpdate()
    {
        if (_originSO.whenToRun == SpecificMoment.OnUpdate)
            SetParameter();
    }

    private void SetParameter()
    {
        if (!_canRun || _animator == null || _parameterHash == 0)
            return;

        float movementSpeed = 0f;

        if (_originSO.valueSource != AnimatorParameterActionSO.ValueSource.Constant)
            movementSpeed = ResolveMovementSpeed();

        switch (_originSO.parameterType)
        {
            case AnimatorParameterActionSO.ParameterType.Bool:
                _animator.SetBool(_parameterHash, ResolveBoolValue(movementSpeed));
                break;

            case AnimatorParameterActionSO.ParameterType.Int:
                _animator.SetInteger(_parameterHash, ResolveIntValue(movementSpeed));
                break;

            case AnimatorParameterActionSO.ParameterType.Float:
                _animator.SetFloat(_parameterHash, ResolveFloatValue(movementSpeed));
                break;

            case AnimatorParameterActionSO.ParameterType.Trigger:
                _animator.SetTrigger(_parameterHash);
                break;
        }
    }

    private bool ResolveBoolValue(float movementSpeed)
    {
        return _originSO.valueSource switch
        {
            AnimatorParameterActionSO.ValueSource.IsMovingFromSpeed =>
                movementSpeed > Mathf.Max(0f, _originSO.isMovingThreshold),
            AnimatorParameterActionSO.ValueSource.MovementSpeed =>
                !Mathf.Approximately(movementSpeed, 0f),
            _ => _originSO.boolValue,
        };
    }

    private int ResolveIntValue(float movementSpeed)
    {
        return _originSO.valueSource switch
        {
            AnimatorParameterActionSO.ValueSource.MovementSpeed =>
                Mathf.RoundToInt(movementSpeed * _originSO.speedMultiplier),
            AnimatorParameterActionSO.ValueSource.IsMovingFromSpeed =>
                movementSpeed > Mathf.Max(0f, _originSO.isMovingThreshold) ? 1 : 0,
            _ => _originSO.intValue,
        };
    }

    private float ResolveFloatValue(float movementSpeed)
    {
        return _originSO.valueSource switch
        {
            AnimatorParameterActionSO.ValueSource.MovementSpeed =>
                movementSpeed * _originSO.speedMultiplier,
            AnimatorParameterActionSO.ValueSource.IsMovingFromSpeed =>
                movementSpeed > Mathf.Max(0f, _originSO.isMovingThreshold) ? 1f : 0f,
            _ => _originSO.floatValue,
        };
    }

    private float ResolveMovementSpeed()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            return GetMagnitude(_agent.velocity);

        if (_characterController != null)
            return GetMagnitude(_characterController.velocity);

        if (_rigidbody != null)
            return GetMagnitude(_rigidbody.linearVelocity);

        if (_vsplatter != null)
            return GetMagnitude(_vsplatter.movementInput);

        return ResolveTransformDeltaSpeed();
    }

    private float ResolveTransformDeltaSpeed()
    {
        if (_ownerTransform == null)
            return 0f;

        Vector3 currentPosition = _ownerTransform.position;

        if (!_hasPreviousPosition)
        {
            _previousPosition = currentPosition;
            _hasPreviousPosition = true;
            return 0f;
        }

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return 0f;

        Vector3 delta = currentPosition - _previousPosition;
        _previousPosition = currentPosition;

        return GetMagnitude(delta / dt);
    }

    private float GetMagnitude(Vector3 velocity)
    {
        if (_originSO.usePlanarSpeed)
            velocity.y = 0f;

        return velocity.magnitude;
    }

    private AnimatorControllerParameterType ResolveExpectedParameterType()
    {
        return _originSO.parameterType switch
        {
            AnimatorParameterActionSO.ParameterType.Bool => AnimatorControllerParameterType.Bool,
            AnimatorParameterActionSO.ParameterType.Int => AnimatorControllerParameterType.Int,
            AnimatorParameterActionSO.ParameterType.Float => AnimatorControllerParameterType.Float,
            AnimatorParameterActionSO.ParameterType.Trigger => AnimatorControllerParameterType.Trigger,
            _ => AnimatorControllerParameterType.Trigger,
        };
    }

    private static bool HasAnimatorParameter(
        Animator animator,
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null || parameterHash == 0)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private static T ResolveInOwnerHierarchy<T>(StateMachine stateMachine) where T : Component
    {
        if (stateMachine.TryGetComponent(out T component))
            return component;

        component = stateMachine.GetComponentInChildren<T>(true);
        if (component != null)
            return component;

        Transform current = stateMachine.transform.parent;

        while (current != null)
        {
            component = current.GetComponentInChildren<T>(true);
            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }
}
