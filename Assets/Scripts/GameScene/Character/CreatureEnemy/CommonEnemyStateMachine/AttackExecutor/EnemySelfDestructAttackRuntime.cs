using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySelfDestructAttackExecutor : EnemyAttackExecutor
{
    private readonly ChaserSelfDestructConfigSO _config;
    private readonly HashSet<Damageable> _damagedTargets = new HashSet<Damageable>();

    private Collider[] _hits;
    private MaskRenderManager _maskRenderManager;

    public EnemySelfDestructAttackExecutor(
        ChaserSelfDestructConfigSO config,
        EnemyAttackExecutorContext context)
        : base(context)
    {
        _config = config;
    }

    public override void Enter()
    {
        IsFinished = false;

        if (_config == null || Context.OwnerTransform == null)
        {
            Finish();
            return;
        }

        _hits = new Collider[_config.MaxOverlapHits];

        if (_config.StopAgentOnEnter)
            StopAgent();

        Explode();
        Finish();
    }

    public override void Tick(float deltaTime)
    {
    }

    private void Explode()
    {
        Vector3 center = Context.OwnerTransform.position;

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
            Context.OwnerGameObject);
    }

    private void HitPlayers(Vector3 center)
    {
        if (_config.PlayerHitRadius <= 0f || _hits == null)
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
            if (damageable == null || damageable == Context.SelfDamageable)
                continue;

            if (!_damagedTargets.Add(damageable))
                continue;

            if (_config.HealthDamage > 0f && damageable.CanReceiveDamage)
                damageable.ReceiveAnAttack(_config.HealthDamage, Context.OwnerGameObject);

            PlayerInfection infection =
                damageable.GetComponent<PlayerInfection>() ??
                damageable.GetComponentInParent<PlayerInfection>();

            if (infection != null && _config.InfectionDamage > 0f)
                infection.AddInfection(_config.InfectionDamage);
        }
    }

    private void StopAgent()
    {
        NavMeshAgent agent = Context.Agent;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void KillSelf()
    {
        if (Context.SelfDamageable == null)
        {
            if (Context.OwnerGameObject != null)
                Object.Destroy(Context.OwnerGameObject);

            return;
        }

        Context.SelfDamageable.Kill();

        if (!Context.SelfDamageable.IsDead)
            Context.SelfDamageable.IsDead = true;
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
