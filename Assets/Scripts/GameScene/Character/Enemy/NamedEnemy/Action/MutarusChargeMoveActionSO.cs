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
    [SerializeField] private MutarusChargeConfigSO _definitionConfig;

    public bool HasDefinitionConfig => _definitionConfig != null;

    public int ChargeCount => _definitionConfig.ChargeCount;
    public float ChargeDistance => _definitionConfig.ChargeDistance;
    public float ChargeSpeed => _definitionConfig.ChargeSpeed;
    public float StartDelay => _definitionConfig.StartDelay;
    public float PauseBetweenCharges => _definitionConfig.PauseBetweenCharges;
    public bool ReAimBeforeEachCharge => _definitionConfig.ReAimBeforeEachCharge;
    public float PredictionFactor => _definitionConfig.PredictionFactor;
    public float MaxPredictionSeconds => _definitionConfig.MaxPredictionSeconds;
    public float RandomAngleDeg => _definitionConfig.RandomAngleDeg;
    public bool SnapRotationOnChargeStart => _definitionConfig.SnapRotationOnChargeStart;
    public bool RotateWhileCharging => _definitionConfig.RotateWhileCharging;
    public float RotationSpeedDegPerSec => _definitionConfig.RotationSpeedDegPerSec;
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

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (MutarusChargeMoveActionSO)OriginSO;

        stateMachine.TryGetComponent(out _enemy);
        stateMachine.TryGetComponent(out _agent);

        _owner = _enemy != null ? _enemy.transform : stateMachine.transform;
    }

    public override void OnStateEnter()
    {
        _hasConfig = _origin.HasDefinitionConfig;
        _currentChargeIndex = 0;
        _timer = 0f;
        _remainingDistance = 0f;
        _chargeDirection = Vector3.zero;
        _done = false;

        if (!_hasConfig)
        {
            Debug.LogError("[MutarusChargeMoveAction] Definition Config is missing.", _owner);
            return;
        }

        StopAgent();

        if (_origin.StartDelay > 0f)
            _phase = ChargePhase.StartDelay;
        else
            BeginNextCharge();
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        if (_done || _owner == null)
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
        _timer += Time.deltaTime;

        if (_timer >= _origin.StartDelay)
            BeginNextCharge();
    }

    private void TickCharging()
    {
        if (_origin.RotateWhileCharging)
            RotateTowardChargeDirection();

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

        if (_origin.SnapRotationOnChargeStart)
            SnapRotationToChargeDirection();
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

    private void SnapRotationToChargeDirection()
    {
        if (_chargeDirection.sqrMagnitude < 0.0001f)
            return;

        _owner.rotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);
    }

    private void RotateTowardChargeDirection()
    {
        if (_chargeDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);

        _owner.rotation = Quaternion.RotateTowards(
            _owner.rotation,
            targetRotation,
            _origin.RotationSpeedDegPerSec * Time.deltaTime);
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }
}
