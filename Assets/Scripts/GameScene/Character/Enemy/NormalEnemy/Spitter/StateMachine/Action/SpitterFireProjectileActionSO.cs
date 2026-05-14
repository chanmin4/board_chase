using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SpitterFireProjectileAction",
    menuName = "State Machines/Enemy Actions/Spitter/Fire Projectile")]
public class SpitterFireProjectileActionSO : StateActionSO<SpitterFireProjectileAction>
{
    [SerializeField] private SpitterAttackConfigSO _config;

    public SpitterAttackConfigSO Config => _config;
}

public class SpitterFireProjectileAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private EnemyAttackRig _rig;
    private SpitterFireProjectileActionSO _origin;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _rig = stateMachine.GetComponent<EnemyAttackRig>();
        _origin = (SpitterFireProjectileActionSO)OriginSO;
    }

    public override void OnStateEnter()
    {
        if (_origin.Config == null)
        {
            Debug.LogError("[SpitterFire] Config is null");
            return;
        }

        if (_enemy == null)
        {
            Debug.LogError("[SpitterFire] Enemy is null");
            return;
        }

        if (_enemy.currentTarget == null)
        {
            Debug.LogWarning("[SpitterFire] currentTarget is null");
            return;
        }

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }

        FireOnce();
    }

    public override void OnUpdate() { }

    private void FireOnce()
    {
        EnemyProjectile prefab = _origin.Config.ProjectilePrefab;

        if (prefab == null)
        {
            Debug.LogError("[SpitterFire] Projectile prefab is null");
            return;
        }

        Transform fireOrigin = _rig != null ? _rig.FireOrigin : _enemy.transform;
        Transform projectileRoot = _rig != null && _rig.ProjectileRoot != null
        ? _rig.ProjectileRoot
        : ProjectileRootRegistry.Root;

        Vector3 spawnPosition = fireOrigin.position;
        spawnPosition.y += _origin.Config.ProjectileSpawnYOffset;

        Vector3 aimPoint = ResolveAimPoint(spawnPosition);
        Vector3 direction = aimPoint - spawnPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"[SpitterFire] Direction too small. fireOrigin={fireOrigin.position}, spawnPosition={spawnPosition}, aimPoint={aimPoint}");
            return;
        }

        direction.Normalize();
        direction = ApplyRandomSpread(direction);

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        _enemy.transform.rotation = rotation;

        EnemyProjectile projectile = projectileRoot != null
            ? Object.Instantiate(prefab, spawnPosition, rotation, projectileRoot)
            : Object.Instantiate(prefab, spawnPosition, rotation);

        projectile.Init(
            direction,
            _origin.Config.ProjectileSpeed,
            _origin.Config.HealthDamage,
            _origin.Config.InfectionDamage,
            _origin.Config.ProjectileCastRadius,
            _origin.Config.ProjectileLifetime,
            _origin.Config.DamageTargetMask,
            _origin.Config.ImpactMask,
            _origin.Config.TriggerInteraction,
            null,
            _origin.Config.PaintChannel,
            _origin.Config.PaintRadiusWorld,
            _origin.Config.PaintPriority,
            _enemy.gameObject
        );
    }


    private Vector3 ResolveAimPoint(Vector3 fireOriginPosition)
    {
        Transform target = _enemy.currentTarget.transform;
        Vector3 targetPosition = target.position;

        if (!_origin.Config.UsePredictiveAim)
            return targetPosition;

        Vector3 targetVelocity = ResolveTargetVelocity(target);
        targetVelocity.y = 0f;

        float leadTime = Mathf.Max(0f, _origin.Config.AimLeadTime);
        Vector3 leadOffset = targetVelocity * leadTime;

        float maxLeadDistance = Mathf.Max(0f, _origin.Config.MaxAimLeadDistance);
        if (maxLeadDistance > 0f && leadOffset.magnitude > maxLeadDistance)
            leadOffset = leadOffset.normalized * maxLeadDistance;

        return targetPosition + leadOffset;
    }

    private Vector3 ResolveTargetVelocity(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        if (target.TryGetComponent(out CharacterController characterController))
            return characterController.velocity;

        if (target.TryGetComponent(out Rigidbody rigidbody))
            return rigidbody.linearVelocity;

        if (target.TryGetComponent(out NavMeshAgent navMeshAgent))
            return navMeshAgent.velocity;

        return Vector3.zero;
    }

    private Vector3 ApplyRandomSpread(Vector3 direction)
    {
        float spreadAngle = Mathf.Max(0f, _origin.Config.RandomSpreadAngle);

        if (spreadAngle <= 0f)
            return direction;

        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRotation = Quaternion.AngleAxis(randomAngle, Vector3.up);

        return (spreadRotation * direction).normalized;
    }
}
