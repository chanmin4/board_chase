using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MutarusChargeMoveAction",
    menuName = "State Machines/Named Enemy Actions/Mutarus/Charge Move")]
public class MutarusChargeMoveActionSO : StateActionSO<MutarusChargeMoveAction>
{
    [Header("Definition Config")]
    [SerializeField] private MutarusChargeAttackConfigSO _chargeConfig;

    public MutarusChargeAttackConfigSO DefinitionConfig => _chargeConfig;
    public bool HasChargeConfig => _chargeConfig != null;

    public int ChargeCount => _chargeConfig.ChargeCount;
    public float ChargeDistance => _chargeConfig.ChargeDistance;
    public float ChargeSpeed => _chargeConfig.ChargeSpeed;
    public float StartDelay => _chargeConfig.StartDelay;
    public float PauseBetweenCharges => _chargeConfig.PauseBetweenCharges;
    public bool ReAimBeforeEachCharge => _chargeConfig.ReAimBeforeEachCharge;
    public float PredictionFactor => _chargeConfig.PredictionFactor;
    public float MaxPredictionSeconds => _chargeConfig.MaxPredictionSeconds;
    public float RandomAngleDeg => _chargeConfig.RandomAngleDeg;
    public float PostAttackDelaySeconds => _chargeConfig.PostAttackDelaySeconds;
    public bool SnapFacingOnAttackStart => _chargeConfig.SnapFacingOnAttackStart;
    public bool FaceTargetWhileAttacking => _chargeConfig.FaceTargetWhileAttacking;
}

public class MutarusChargeMoveAction : StateAction
{
    private enum ChargePhase
    {
        StartDelay,
        Charging,
        Pause,
        Done
    }

    private MutarusChargeMoveActionSO _origin;
    private NamedEnemyBlackboard _blackboard;
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private Transform _owner;

    private ChargePhase _phase;
    private int _currentChargeIndex;
    private float _timer;
    private float _remainingDistance;
    private Vector3 _chargeDirection;
    private bool _done;
    private bool _hasConfig;
    private bool _waitingPostAttackDelay;
    private bool _finishedNotified;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (MutarusChargeMoveActionSO)OriginSO;

        stateMachine.TryGetComponent(out _blackboard);
        stateMachine.TryGetComponent(out _enemy);
        stateMachine.TryGetComponent(out _agent);

        _owner = _enemy != null ? _enemy.transform : stateMachine.transform;
    }

    public override void OnStateEnter()
    {
        _hasConfig = _origin.HasChargeConfig;
        _currentChargeIndex = 0;
        _timer = 0f;
        _remainingDistance = 0f;
        _chargeDirection = Vector3.zero;
        _done = false;
        _waitingPostAttackDelay = false;
        _finishedNotified = false;

        if (!_hasConfig)
        {
            Debug.LogError("[MutarusChargeMoveAction] Charge Config is missing.", _owner);
            NotifyAttackFinished();
            return;
        }

        StopAgent();
        FaceAttackTarget(_origin.SnapFacingOnAttackStart);

        if (_origin.StartDelay > 0f)
            _phase = ChargePhase.StartDelay;
        else
            BeginNextCharge();
    }

    public override void OnUpdate()
    {
        if (_waitingPostAttackDelay)
        {
            TickPostAttackDelay();
            return;
        }

        if (!_hasConfig || _done || _owner == null)
            return;

        switch (_phase)
        {
            case ChargePhase.StartDelay:
                TickStartDelay();
                break;
            case ChargePhase.Charging:
                TickCharging();
                break;
            case ChargePhase.Pause:
                TickPause();
                break;
        }
    }

    public override void OnStateExit()
    {
        StopAgent();
    }

    private void TickStartDelay()
    {
        if (_origin.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        _timer += Time.deltaTime;

        if (_timer >= _origin.StartDelay)
            BeginNextCharge();
    }

    private void TickCharging()
    {
        if (_origin.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        float moveDistance = _origin.ChargeSpeed * Time.deltaTime;
        moveDistance = Mathf.Min(moveDistance, _remainingDistance);

        Vector3 motion = _chargeDirection * moveDistance;

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.Move(motion);
        else
            _owner.position += motion;

        _remainingDistance -= moveDistance;

        if (_remainingDistance <= 0.001f)
            FinishCurrentCharge();
    }

    private void TickPause()
    {
        if (_origin.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        _timer += Time.deltaTime;

        if (_timer >= _origin.PauseBetweenCharges)
            BeginNextCharge();
    }

    private void BeginNextCharge()
    {
        if (_currentChargeIndex >= _origin.ChargeCount)
        {
            FinishAllCharges();
            return;
        }

        _currentChargeIndex++;

        if (_origin.ReAimBeforeEachCharge || _chargeDirection == Vector3.zero)
            _chargeDirection = ResolveChargeDirection();

        _remainingDistance = _origin.ChargeDistance;
        _phase = ChargePhase.Charging;

        if (_origin.SnapFacingOnAttackStart)
            FaceChargeDirection(true);
    }

    private void FinishCurrentCharge()
    {
        if (_currentChargeIndex >= _origin.ChargeCount)
        {
            FinishAllCharges();
            return;
        }

        if (_origin.PauseBetweenCharges > 0f)
        {
            _timer = 0f;
            _phase = ChargePhase.Pause;
        }
        else
        {
            BeginNextCharge();
        }
    }

    private void FinishAllCharges()
    {
        _phase = ChargePhase.Done;
        _done = true;
        StopAgent();
        BeginPostAttackDelay();
    }

    private void BeginPostAttackDelay()
    {
        if (_finishedNotified)
            return;

        if (_origin.PostAttackDelaySeconds <= 0f)
        {
            NotifyAttackFinished();
            return;
        }

        _timer = 0f;
        _waitingPostAttackDelay = true;
    }

    private void TickPostAttackDelay()
    {
        _timer += Time.deltaTime;

        if (_timer >= _origin.PostAttackDelaySeconds)
            NotifyAttackFinished();
    }

    private void NotifyAttackFinished()
    {
        if (_finishedNotified)
            return;

        _finishedNotified = true;
        _waitingPostAttackDelay = false;
        _blackboard?.FinishSelectedAttack();
    }

    private Vector3 ResolveChargeDirection()
    {
        Vector3 origin = _owner.position;
        Vector3 aimPoint = ResolvePredictedAimPoint(origin);

        Vector3 direction = aimPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = _owner.forward;
            direction.y = 0f;
        }

        direction.Normalize();

        if (_origin.RandomAngleDeg > 0f)
        {
            float randomAngle = Random.Range(-_origin.RandomAngleDeg, _origin.RandomAngleDeg);
            direction = Quaternion.Euler(0f, randomAngle, 0f) * direction;
        }

        return direction.normalized;
    }

    private Vector3 ResolvePredictedAimPoint(Vector3 origin)
    {
        if (_enemy == null || _enemy.currentTarget == null)
            return origin + _owner.forward * _origin.ChargeDistance;

        Transform target = _enemy.currentTarget.transform;
        Vector3 targetPosition = target.position;
        Vector3 targetVelocity = ResolveTargetVelocity(target);

        Vector3 flatToTarget = targetPosition - origin;
        flatToTarget.y = 0f;

        float distance = flatToTarget.magnitude;
        float predictionSeconds = 0f;

        if (_origin.PredictionFactor > 0f)
        {
            predictionSeconds = distance / Mathf.Max(0.01f, _origin.ChargeSpeed);
            predictionSeconds *= _origin.PredictionFactor;
            predictionSeconds = Mathf.Min(predictionSeconds, _origin.MaxPredictionSeconds);
        }

        Vector3 predicted = targetPosition + targetVelocity * predictionSeconds;
        predicted.y = targetPosition.y;

        return predicted;
    }

    private static Vector3 ResolveTargetVelocity(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        CharacterController controller =
            target.GetComponent<CharacterController>() ??
            target.GetComponentInParent<CharacterController>();

        if (controller != null)
            return controller.velocity;

        Rigidbody rigidbody =
            target.GetComponent<Rigidbody>() ??
            target.GetComponentInParent<Rigidbody>();

        if (rigidbody != null)
            return rigidbody.linearVelocity;

        return Vector3.zero;
    }

    private void FaceAttackTarget(bool snap)
    {
        MutarusChargeAttackConfigSO config = _origin.DefinitionConfig;

        if (config == null || _owner == null)
            return;

        if (TryResolveCurrentTargetPoint(out Vector3 targetPoint))
        {
            config.TryFaceWorldPoint(_owner, targetPoint, snap, Time.deltaTime);
            return;
        }

        FaceChargeDirection(snap);
    }

    private bool TryResolveCurrentTargetPoint(out Vector3 targetPoint)
    {
        if (_enemy != null && _enemy.currentTarget != null)
        {
            targetPoint = _enemy.currentTarget.transform.position;
            return true;
        }

        targetPoint = Vector3.zero;
        return false;
    }

    private void FaceChargeDirection(bool snap)
    {
        MutarusChargeAttackConfigSO config = _origin.DefinitionConfig;

        if (config == null)
            return;

        if (_chargeDirection.sqrMagnitude < 0.0001f)
            return;

        config.TryFaceDirection(_owner, _chargeDirection, snap, Time.deltaTime);
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }
}
