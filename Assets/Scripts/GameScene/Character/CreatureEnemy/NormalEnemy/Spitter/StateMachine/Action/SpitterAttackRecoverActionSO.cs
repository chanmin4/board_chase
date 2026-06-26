using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SpitterAttackRecoverAction",
    menuName = "State Machines/Enemy Actions/Spitter/Attack Recover")]
public class SpitterAttackRecoverActionSO : StateActionSO<SpitterAttackRecoverAction>
{
    [SerializeField] private SpitterAttackConfigSO _config;

    public SpitterAttackConfigSO Config => _config;
}

public class SpitterAttackRecoverAction : StateAction
{

    private Enemy _enemy;
    private NavMeshAgent _agent;
    private SpitterAttackRecoverActionSO _origin;
    private Transform _transform;
    private float _nextRefreshTime;
    private float _side;
    private float _nextDebugLogTime;
    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _origin = (SpitterAttackRecoverActionSO)OriginSO;
        _transform = stateMachine.transform;
    }

    public override void OnStateEnter()
    {
        
        _side = Random.value < 0.5f ? -1f : 1f;
        _nextRefreshTime = 0f;
        _nextDebugLogTime = 0f;
        if (IsAgentReady())
        {
            _agent.isStopped = false;
            _agent.speed = _origin.Config.RecoverMoveSpeed;
        }
    }

    public override void OnUpdate()
    {
        if (_origin.Config == null || _enemy == null || _enemy.currentTarget == null)
            return;

        Vector3 targetPos = _enemy.currentTarget.transform.position;
        Vector3 selfPos = _transform.position;
        float distance = GetFlatDistance(selfPos, targetPos);

        DrawRecoverDebug(targetPos, distance);
        RotateToward(targetPos);

        if (!IsAgentReady())
            return;

        if (Time.time < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.time + _origin.Config.DestinationRefreshInterval;

        Vector3 destination = PickRecoverDestination(targetPos);
        DrawDestinationDebug(destination);
        if (NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                _origin.Config.NavMeshSampleDistance,
                NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
            _agent.isStopped = false;
        }
    }

    public override void OnStateExit()
    {
        if (!IsAgentReady())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    private Vector3 PickRecoverDestination(Vector3 targetPos)
    {
        Vector3 selfPos = _transform.position;

        Vector3 fromTarget = selfPos - targetPos;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = -_transform.forward;

        float distance = fromTarget.magnitude;
        Vector3 away = fromTarget.normalized;
        Vector3 toward = -away;
        Vector3 tangent = new Vector3(-away.z, 0f, away.x) * _side;

        if (distance > _origin.Config.TooFarDistance)
        {
            Vector3 move = (toward + tangent * 0.5f).normalized;
            return selfPos + move * _origin.Config.StrafeDistance;
        }

        if (distance < _origin.Config.TooCloseDistance)
        {
            Vector3 move = (away + tangent * 0.5f).normalized;
            return selfPos + move * _origin.Config.StrafeDistance;
        }

        Vector3 idealPoint =
            targetPos +
            away * _origin.Config.PreferredDistance +
            tangent * _origin.Config.StrafeDistance;

        return idealPoint;
    }

    private void RotateToward(Vector3 targetPos)
    {
        Vector3 direction = targetPos - _transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        _transform.rotation = Quaternion.RotateTowards(
            _transform.rotation,
            targetRotation,
            _origin.Config.TurnSpeedDegPerSecond * Time.deltaTime);
    }

    private bool IsAgentReady()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }
    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void DrawRecoverDebug(Vector3 targetPos, float distance)
    {
        SpitterAttackConfigSO config = _origin.Config;

        if (config == null)
            return;

        if (config.DebugDrawRecoverDistances)
        {
            Vector3 center = targetPos;
            center.y = _transform.position.y + config.DebugDrawHeight;

            DrawCircle(center, config.TooCloseDistance, Color.red);
            DrawCircle(center, config.PreferredDistance, Color.green);
            DrawCircle(center, config.TooFarDistance, Color.yellow);

            Vector3 self = _transform.position;
            self.y = center.y;

            Color distanceColor = Color.green;

            if (distance < config.TooCloseDistance)
                distanceColor = Color.red;
            else if (distance > config.TooFarDistance)
                distanceColor = Color.yellow;

            Debug.DrawLine(center, self, distanceColor);
        }

        if (config.DebugLogRecoverDistance && Time.time >= _nextDebugLogTime)
        {
            _nextDebugLogTime = Time.time + config.DebugLogInterval;

            Debug.Log(
                $"[SpitterRecover] distance={distance:F2}, " +
                $"tooClose={config.TooCloseDistance:F2}, " +
                $"preferred={config.PreferredDistance:F2}, " +
                $"tooFar={config.TooFarDistance:F2}");
        }
    }

    private void DrawDestinationDebug(Vector3 destination)
    {
        SpitterAttackConfigSO config = _origin.Config;

        if (config == null || !config.DebugDrawRecoverDistances)
            return;

        Vector3 from = _transform.position;
        Vector3 to = destination;

        from.y += config.DebugDrawHeight;
        to.y += config.DebugDrawHeight;

        Debug.DrawLine(from, to, Color.cyan);
    }
    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        const int segments = 64;

        Vector3 previous = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            Debug.DrawLine(previous, next, color);
            previous = next;
        }
    }
}

