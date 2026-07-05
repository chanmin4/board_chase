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
    [Header("Definition Config")]
    [SerializeField] private ChaserSelfDestructConfigSO _definitionConfig;

    [Header("Virus Paint")]
    [Tooltip("Event channel that provides the active MaskRenderManager. If empty, the action searches the scene.")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Self")]
    [Tooltip("Stops the NavMeshAgent and clears its path before exploding.")]
    [SerializeField] private bool _stopAgentOnEnter = true;

    [Tooltip("Kills this enemy after the explosion. The parent MODE FSM can then transition to Dead.")]
    [SerializeField] private bool _killSelfAfterExplosion = true;

    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public bool HasDefinitionConfig => _definitionConfig != null;
    public ChaserSelfDestructConfigSO DefinitionConfig => _definitionConfig;
    public float VirusPaintRadius => _definitionConfig.VirusPaintRadius;
    public int PaintPriority => _definitionConfig.PaintPriority;
    public LayerMask DamageMask => _definitionConfig.DamageMask;
    public QueryTriggerInteraction TriggerInteraction => _definitionConfig.TriggerInteraction;
    public float PlayerHitRadius => _definitionConfig.PlayerHitRadius;
    public float HealthDamage => _definitionConfig.HealthDamage;
    public float InfectionDamage => _definitionConfig.InfectionDamage;
    public int MaxOverlapHits => _definitionConfig.MaxOverlapHits;
    public bool StopAgentOnEnter => _definitionConfig != null ? _definitionConfig.StopAgentOnEnter : _stopAgentOnEnter;
    public bool KillSelfAfterExplosion => _definitionConfig != null ? _definitionConfig.KillSelfAfterExplosion : _killSelfAfterExplosion;

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
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (ChaserVirusSelfDestructActionSO)OriginSO;
        _owner = stateMachine.gameObject;
        _transform = stateMachine.transform;

        stateMachine.TryGetComponent(out _agent);
        stateMachine.TryGetComponent(out _selfDamageable);

        _hasConfig = _config.HasDefinitionConfig;
        _hits = new Collider[_hasConfig ? _config.MaxOverlapHits : 1];
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasDefinitionConfig;

        if (!_hasConfig)
        {
            Debug.LogError("[ChaserVirusSelfDestructAction] Definition Config is missing.", _owner);
            return;
        }

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
            PaintChannel.Virus,
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

            PlayerShooterInfection infection =
                damageable.GetComponent<PlayerShooterInfection>() ??
                damageable.GetComponentInParent<PlayerShooterInfection>();

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

        MaskRenderManagerEventChannelSO readyChannel =
            _config.DefinitionConfig != null && _config.DefinitionConfig.MaskRenderManagerReadyChannel != null
                ? _config.DefinitionConfig.MaskRenderManagerReadyChannel
                : _config.MaskRenderManagerReadyChannel;

        if (readyChannel != null)
            _maskRenderManager = readyChannel.Current;

        if (_maskRenderManager == null)
            _maskRenderManager = Object.FindAnyObjectByType<MaskRenderManager>();

        return _maskRenderManager;
    }
}
