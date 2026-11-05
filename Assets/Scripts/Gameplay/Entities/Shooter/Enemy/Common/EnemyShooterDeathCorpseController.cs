/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Coordinates the EnemyShooterDeathCorpseController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class EnemyShooterDeathCorpseController : ShooterDeathCorpseController
{
    [Header("Enemy Refs")]
    [SerializeField] private EnemyLootInventoryRuntime _lootInventory;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    [Header("Enemy Death")]
    [Tooltip("Copies both weapon slots, remaining throwable amounts, and armor durability into corpse loot at death.")]
    [FormerlySerializedAs("_syncLootArmorDurability")]
    [SerializeField] private bool _syncLootEquipment = true;
    [SerializeField] private bool _stopAndDisableNavMeshAgent = true;

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        Damageable?.PreserveGameObjectOnDeath();
    }

    // Purpose: Performs the before convert to corpse workflow for this component.
    protected override void BeforeConvertToCorpse()
    {
        if (_syncLootEquipment)
            _lootInventory?.SyncEquipmentFromRuntime();
        _lootInventory?.PrepareCorpseArmorDrop();

        StopNavMeshAgent();
    }

    // Purpose: Performs the after convert to corpse workflow for this component.
    protected override void AfterConvertToCorpse()
    {
        if (!DebugLogsEnabled)
            return;

        Debug.Log(
            $"[EnemyShooterDeathCorpseController] Corpse loot state. " +
            $"lootNull={_lootInventory == null}, " +
            $"{(_lootInventory != null ? _lootInventory.BuildInteractionDebugInfo() : "loot=missing")}",
            this);
    }

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_lootInventory == null)
            _lootInventory = EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(this);

        if (_navMeshAgent == null)
            _navMeshAgent = EntityComponentResolver.Resolve<NavMeshAgent>(this);
    }

    // Purpose: Stops the active nav mesh agent behavior.
    private void StopNavMeshAgent()
    {
        if (_navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled)
            return;

        if (_navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.ResetPath();
            _navMeshAgent.isStopped = true;
        }

        if (_stopAndDisableNavMeshAgent)
            _navMeshAgent.enabled = false;
    }
}
