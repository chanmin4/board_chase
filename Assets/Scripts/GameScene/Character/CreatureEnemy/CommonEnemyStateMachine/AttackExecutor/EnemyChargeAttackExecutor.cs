using UnityEngine;
using UnityEngine.AI;

public class EnemyChargeAttackExecutor : EnemyAttackExecutor
{
    private enum ChargePhase
    {
        StartDelay,
        Charging,
        Pause,
        Done
    }

    private readonly MutarusChargeAttackConfigSO _config;

    private ChargePhase _phase;
    private int _currentChargeIndex;
    private float _timer;
    private float _remainingDistance;
    private Vector3 _chargeDirection;
    private bool _done;
    private bool _waitingPostAttackDelay;

    public EnemyChargeAttackExecutor(
        MutarusChargeAttackConfigSO config,
        EnemyAttackExecutorContext context)
        : base(context)
    {
        _config = config;
    }

    public override void Enter()
    {
        _currentChargeIndex = 0;
        _timer = 0f;
        _remainingDistance = 0f;
        _chargeDirection = Vector3.zero;
        _done = false;
        _waitingPostAttackDelay = false;
        IsFinished = false;

        if (_config == null || Context.OwnerTransform == null)
        {
            Finish();
            return;
        }

        StopAgent();
        FaceAttackTarget(_config.SnapFacingOnAttackStart);

        if (_config.StartDelay > 0f)
            _phase = ChargePhase.StartDelay;
        else
            BeginNextCharge();
    }

    public override void Tick(float deltaTime)
    {
        if (IsFinished)
            return;

        if (_waitingPostAttackDelay)
        {
            TickPostAttackDelay(deltaTime);
            return;
        }

        if (_config == null || _done || Context.OwnerTransform == null)
            return;

        switch (_phase)
        {
            case ChargePhase.StartDelay:
                TickStartDelay(deltaTime);
                break;
            case ChargePhase.Charging:
                TickCharging(deltaTime);
                break;
            case ChargePhase.Pause:
                TickPause(deltaTime);
                break;
        }
    }

    public override void Exit()
    {
        StopAgent();
    }

    private void TickStartDelay(float deltaTime)
    {
        if (_config.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        _timer += deltaTime;

        if (_timer >= _config.StartDelay)
            BeginNextCharge();
    }

    private void TickCharging(float deltaTime)
    {
        if (_config.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        float moveDistance = _config.ChargeSpeed * deltaTime;
        moveDistance = Mathf.Min(moveDistance, _remainingDistance);

        Vector3 motion = _chargeDirection * moveDistance;
        NavMeshAgent agent = Context.Agent;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.Move(motion);
        else
            Context.OwnerTransform.position += motion;

        _remainingDistance -= moveDistance;

        if (_remainingDistance <= 0.001f)
            FinishCurrentCharge();
    }

    private void TickPause(float deltaTime)
    {
        if (_config.FaceTargetWhileAttacking)
            FaceAttackTarget(false);

        _timer += deltaTime;

        if (_timer >= _config.PauseBetweenCharges)
            BeginNextCharge();
    }

    private void BeginNextCharge()
    {
        if (_currentChargeIndex >= _config.ChargeCount)
        {
            FinishAllCharges();
            return;
        }

        _currentChargeIndex++;

        if (_config.ReAimBeforeEachCharge || _chargeDirection == Vector3.zero)
            _chargeDirection = ResolveChargeDirection();

        _remainingDistance = _config.ChargeDistance;
        _phase = ChargePhase.Charging;

        if (_config.SnapFacingOnAttackStart)
            FaceChargeDirection(true);
    }

    private void FinishCurrentCharge()
    {
        if (_currentChargeIndex >= _config.ChargeCount)
        {
            FinishAllCharges();
            return;
        }

        if (_config.PauseBetweenCharges > 0f)
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
        if (IsFinished)
            return;

        if (_config.PostAttackDelaySeconds <= 0f)
        {
            Finish();
            return;
        }

        _timer = 0f;
        _waitingPostAttackDelay = true;
    }

    private void TickPostAttackDelay(float deltaTime)
    {
        _timer += deltaTime;

        if (_timer >= _config.PostAttackDelaySeconds)
            Finish();
    }

    private Vector3 ResolveChargeDirection()
    {
        Transform owner = Context.OwnerTransform;
        Vector3 origin = owner.position;
        Vector3 aimPoint = ResolvePredictedAimPoint(origin);

        Vector3 direction = aimPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = owner.forward;
            direction.y = 0f;
        }

        direction.Normalize();

        if (_config.RandomAngleDeg > 0f)
        {
            float randomAngle = Random.Range(-_config.RandomAngleDeg, _config.RandomAngleDeg);
            direction = Quaternion.Euler(0f, randomAngle, 0f) * direction;
        }

        return direction.normalized;
    }

    private Vector3 ResolvePredictedAimPoint(Vector3 origin)
    {
        Enemy enemy = Context.Enemy;
        Transform owner = Context.OwnerTransform;

        if (enemy == null || enemy.currentTarget == null)
            return origin + owner.forward * _config.ChargeDistance;

        Transform target = enemy.currentTarget.transform;
        Vector3 targetPosition = target.position;
        Vector3 targetVelocity = ResolveTargetVelocity(target);

        Vector3 flatToTarget = targetPosition - origin;
        flatToTarget.y = 0f;

        float distance = flatToTarget.magnitude;
        float predictionSeconds = 0f;

        if (_config.PredictionFactor > 0f)
        {
            predictionSeconds = distance / Mathf.Max(0.01f, _config.ChargeSpeed);
            predictionSeconds *= _config.PredictionFactor;
            predictionSeconds = Mathf.Min(predictionSeconds, _config.MaxPredictionSeconds);
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
        Transform owner = Context.OwnerTransform;

        if (_config == null || owner == null)
            return;

        if (TryResolveCurrentTargetPoint(out Vector3 targetPoint))
        {
            _config.TryFaceWorldPoint(owner, targetPoint, snap, Time.deltaTime);
            return;
        }

        FaceChargeDirection(snap);
    }

    private bool TryResolveCurrentTargetPoint(out Vector3 targetPoint)
    {
        Enemy enemy = Context.Enemy;

        if (enemy != null && enemy.currentTarget != null)
        {
            targetPoint = enemy.currentTarget.transform.position;
            return true;
        }

        targetPoint = Vector3.zero;
        return false;
    }

    private void FaceChargeDirection(bool snap)
    {
        if (_config == null || Context.OwnerTransform == null)
            return;

        if (_chargeDirection.sqrMagnitude < 0.0001f)
            return;

        _config.TryFaceDirection(
            Context.OwnerTransform,
            _chargeDirection,
            snap,
            Time.deltaTime);
    }

    private void StopAgent()
    {
        NavMeshAgent agent = Context.Agent;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        agent.ResetPath();
        agent.isStopped = true;
    }
}
