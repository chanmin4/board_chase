/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines EnemySpawnAlarmParticleActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemySpawnAlarmParticleAction",
    menuName = "State Machines/Enemy Actions/Spawn AlarmParticle")]
public class EnemySpawnAlarmParticleActionSO : StateActionSO<EnemySpawnTelegraphAction>
{
    [Header("Warning Effect")]
    [SerializeField] private ParticleSystem _warningParticlePrefab;
    [SerializeField] private Vector3 _warningOffset = Vector3.zero;
    [SerializeField] private bool _parentWarningToEnemy = false;
    [SerializeField] private bool _destroyWarningOnExit = true;

    [Header("During Spawning")]
    [SerializeField] private bool _hideRenderers = true;
    [SerializeField] private bool _disableColliders = true;
    [SerializeField] private bool _stopNavMeshAgent = true;
    [Header("Spawn Effect")]
    [SerializeField] private ParticleSystem _spawnParticlePrefab;
    [SerializeField] private Vector3 _spawnOffset = Vector3.zero;
    [SerializeField] private bool _parentSpawnToEnemy = false;
    [SerializeField] private bool _destroySpawnAfterDelay = true;
    [SerializeField] private float _spawnDestroyDelay = 4f;

    public ParticleSystem WarningParticlePrefab => _warningParticlePrefab;
    public Vector3 WarningOffset => _warningOffset;
    public bool ParentWarningToEnemy => _parentWarningToEnemy;
    public bool DestroyWarningOnExit => _destroyWarningOnExit;

    public bool HideRenderers => _hideRenderers;
    public bool DisableColliders => _disableColliders;
    public bool StopNavMeshAgent => _stopNavMeshAgent;
    public ParticleSystem SpawnParticlePrefab => _spawnParticlePrefab;
    public Vector3 SpawnOffset => _spawnOffset;
    public bool ParentSpawnToEnemy => _parentSpawnToEnemy;
    public bool DestroySpawnAfterDelay => _destroySpawnAfterDelay;
    public float SpawnDestroyDelay => _spawnDestroyDelay;
}

public class EnemySpawnTelegraphAction : StateAction
{
    private EnemySpawnAlarmParticleActionSO _origin;
    private Enemy _enemy;
    private Transform _transform;
    private NavMeshAgent _agent;

    private Renderer[] _renderers;
    private Collider[] _colliders;

    private bool[] _rendererEnabledStates;
    private bool[] _colliderEnabledStates;

    private bool _agentWasStopped;
    private ParticleSystem _warningInstance;
    private SectorRuntime _sectorRuntime;
    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemySpawnAlarmParticleActionSO)OriginSO;
        _transform = stateMachine.transform;

        stateMachine.TryGetComponent(out _agent);
        stateMachine.TryGetComponent(out _enemy);

        _sectorRuntime = stateMachine.GetComponentInParent<SectorRuntime>();

        _renderers = stateMachine.GetComponentsInChildren<Renderer>(true);
        _colliders = stateMachine.GetComponentsInChildren<Collider>(true);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        CacheOriginalStates();

        if (_origin.HideRenderers)
            SetRenderers(false);

        if (_origin.DisableColliders)
            SetColliders(false);

        if (_origin.StopNavMeshAgent)
            StopAgent();

        PlayWarningEffect();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_origin.DestroyWarningOnExit && _warningInstance != null)
            Object.Destroy(_warningInstance.gameObject);

        RestoreOriginalStates();
        RestoreAgent();

        PlaySpawnEffect();
    }

    // Purpose: Caches original states for later reuse.
    private void CacheOriginalStates()
    {
        _rendererEnabledStates = new bool[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _rendererEnabledStates[i] = _renderers[i].enabled;
        }

        _colliderEnabledStates = new bool[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliderEnabledStates[i] = _colliders[i].enabled;
        }

        if (_agent != null)
            _agentWasStopped = _agent.isStopped;
    }

    // Purpose: Updates the renderers state.
    private void SetRenderers(bool enabled)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = enabled;
        }
    }

    // Purpose: Updates the colliders state.
    private void SetColliders(bool enabled)
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = enabled;
        }
    }

    // Purpose: Stops the active agent behavior.
    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    // Purpose: Restores the saved original states state.
    private void RestoreOriginalStates()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && i < _rendererEnabledStates.Length)
                _renderers[i].enabled = _rendererEnabledStates[i];
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null && i < _colliderEnabledStates.Length)
                _colliders[i].enabled = _colliderEnabledStates[i];
        }
    }

    // Purpose: Restores the saved agent state.
    private void RestoreAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = _agentWasStopped;
    }

    // Purpose: Plays the requested warning effect action or effect.
    private void PlayWarningEffect()
    {
        ParticleSystem prefab = _origin.WarningParticlePrefab;
        if (prefab == null)
            return;

        Vector3 position = _transform.position + _origin.WarningOffset;
        Quaternion rotation = Quaternion.identity;
        Transform parent = ResolveEffectParent(_origin.ParentWarningToEnemy);

        _warningInstance = Object.Instantiate(prefab, position, rotation, parent);
        _warningInstance.Play(true);
    }
    // Purpose: Plays the requested spawn effect action or effect.
    private void PlaySpawnEffect()
    {
        ParticleSystem prefab = _origin.SpawnParticlePrefab;
        if (prefab == null)
            return;

        Vector3 position = _transform.position + _origin.SpawnOffset;
        Quaternion rotation = Quaternion.identity;
        Transform parent = ResolveEffectParent(_origin.ParentSpawnToEnemy);

        ParticleSystem instance = Object.Instantiate(prefab, position, rotation, parent);
        instance.Play(true);

        if (_origin.DestroySpawnAfterDelay)
            Object.Destroy(instance.gameObject, _origin.SpawnDestroyDelay);
    }
    // Purpose: Resolves the effect parent needed by this component.
    private Transform ResolveEffectParent(bool parentToEnemy)
    {
        if (parentToEnemy)
            return _transform;

        SectorRuntime sector =
            _enemy != null && _enemy.CurrentSector != null
                ? _enemy.CurrentSector
                : _sectorRuntime;

        if (sector != null && sector.PatternObjectRoot != null)
            return sector.PatternObjectRoot;

        return _transform;
    }
}
