using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ChaserVirusSelfDestructAction",
    menuName = "State Machines/Enemy Actions/Chaser Virus Self Destruct")]
public class ChaserVirusSelfDestructActionSO : StateActionSO
{
    [Header("Virus Paint")]
    [Tooltip("Event channel that provides the active MaskRenderManager. If empty, the action searches the scene.")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("Radius used to paint virus at the explosion position.")]
    [SerializeField] private float _virusPaintRadius = 3.5f;

    [Tooltip("Paint priority passed to MaskRenderManager. Use a higher value to override lower priority paint.")]
    [SerializeField] private int _paintPriority = 10;

    [Header("Player Hit")]
    [Tooltip("Layers that can receive explosion damage. Include the player hurtbox layer.")]
    [SerializeField] private LayerMask _damageMask = Physics.DefaultRaycastLayers;

    [Tooltip("Use Collide when player hurtboxes are trigger colliders.")]
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Tooltip("Radius used for health damage and infection damage.")]
    [SerializeField] private float _playerHitRadius = 3f;

    [Tooltip("Health damage dealt to the player. Set to 0 for infection-only explosions.")]
    [SerializeField] private float _healthDamage = 0f;

    [Tooltip("Infection amount added to the player.")]
    [SerializeField] private float _infectionDamage = 25f;

    [Tooltip("Collider buffer size for overlap queries. Usually 16 to 32 is enough.")]
    [SerializeField] private int _maxOverlapHits = 32;

    [Header("Self")]
    [Tooltip("Stops the NavMeshAgent and clears its path before exploding.")]
    [SerializeField] private bool _stopAgentOnEnter = true;

    [Tooltip("Kills this enemy after the explosion. The parent MODE FSM can then transition to Dead.")]
    [SerializeField] private bool _killSelfAfterExplosion = true;

    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public float VirusPaintRadius => Mathf.Max(0.001f, _virusPaintRadius);
    public int PaintPriority => _paintPriority;
    public LayerMask DamageMask => _damageMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public float PlayerHitRadius => Mathf.Max(0f, _playerHitRadius);
    public float HealthDamage => Mathf.Max(0f, _healthDamage);
    public float InfectionDamage => Mathf.Max(0f, _infectionDamage);
    public int MaxOverlapHits => Mathf.Max(1, _maxOverlapHits);
    public bool StopAgentOnEnter => _stopAgentOnEnter;
    public bool KillSelfAfterExplosion => _killSelfAfterExplosion;

    protected override StateAction CreateAction() => new ChaserVirusSelfDestructAction();
}

public class ChaserVirusSelfDestructAction : StateAction
{
    private ChaserVirusSelfDestructActionSO _config;
    private GameObject _owner;
    private Transform _transform;
    private NavMeshAgent _agent;
    private Damageable _selfDamageable;
    private MaskRenderManager _maskRenderManager;
    private Collider[] _hits;
    private readonly HashSet<Damageable> _damagedTargets = new HashSet<Damageable>();

    public override void Awake(StateMachine stateMachine)
    {
        _config = (ChaserVirusSelfDestructActionSO)OriginSO;
        _owner = stateMachine.gameObject;
        _transform = stateMachine.transform;

        stateMachine.TryGetComponent(out _agent);
        stateMachine.TryGetComponent(out _selfDamageable);

        _hits = new Collider[_config.MaxOverlapHits];
    }

    public override void OnStateEnter()
    {
        if (_config.StopAgentOnEnter)
            StopAgent();

        Explode();
    }

    public override void OnUpdate()
    {
    }

    private void Explode()
    {
        Vector3 center = _transform.position;

        PaintVirus(center);
        HitPlayers(center);

        if (_config.KillSelfAfterExplosion)
            KillSelf();
    }

    private void PaintVirus(Vector3 center)
    {
        MaskRenderManager manager = ResolveMaskRenderManager();
        if (manager == null)
            return;

        manager.RequestCircle(
            MaskRenderManager.PaintChannel.Virus,
            center,
            _config.VirusPaintRadius,
            _config.PaintPriority,
            _owner);
    }

    private void HitPlayers(Vector3 center)
    {
        if (_config.PlayerHitRadius <= 0f)
            return;

        _damagedTargets.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            center,
            _config.PlayerHitRadius,
            _hits,
            _config.DamageMask,
            _config.TriggerInteraction);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];
            if (hit == null)
                continue;

            Damageable damageable = hit.GetComponentInParent<Damageable>();
            if (damageable == null || damageable == _selfDamageable)
                continue;

            if (!_damagedTargets.Add(damageable))
                continue;

            if (_config.HealthDamage > 0f && damageable.CanReceiveDamage)
                damageable.ReceiveAnAttack(_config.HealthDamage, _owner);

            PlayerInfection infection =
                damageable.GetComponent<PlayerInfection>() ??
                damageable.GetComponentInParent<PlayerInfection>();

            if (infection != null && _config.InfectionDamage > 0f)
                infection.AddInfection(_config.InfectionDamage);
        }
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    private void KillSelf()
    {
        if (_selfDamageable == null)
        {
            Object.Destroy(_owner);
            return;
        }

        _selfDamageable.Kill();

        if (!_selfDamageable.IsDead)
            _selfDamageable.IsDead = true;
    }

    private MaskRenderManager ResolveMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return _maskRenderManager;

        if (_config.MaskRenderManagerReadyChannel != null)
            _maskRenderManager = _config.MaskRenderManagerReadyChannel.Current;

        if (_maskRenderManager == null)
            _maskRenderManager = Object.FindAnyObjectByType<MaskRenderManager>();

        return _maskRenderManager;
    }
}
