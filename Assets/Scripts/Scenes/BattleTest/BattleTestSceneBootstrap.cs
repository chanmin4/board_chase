/*
 * Role hierarchy: Scenes > BattleTest
 * Primary responsibility: Implements the focused BattleTestSceneBootstrap responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class BattleTestSceneBootstrap : MonoBehaviour
{
    [Header("Scene Actors")]
    [Tooltip("If true, finds PlayerCharacter and enemy shooter combat controllers already placed in the scene.")]
    [SerializeField] private bool _autoFindActors = true;

    [SerializeField] private PlayerCharacter _player;
    [SerializeField] private EnemyShooterCombatController[] _enemyCombatControllers;

    [Header("Enemy Startup")]
    [Tooltip("If true, every enemy shooter immediately targets the player when the scene starts.")]
    [SerializeField] private bool _alertEnemiesToPlayerOnStart = true;

    [Header("NavMesh Validation")]
    [Tooltip("If true, logs enemy NavMeshAgent state and optionally moves agents onto the nearest NavMesh point.")]
    [SerializeField] private bool _validateEnemyNavMesh = true;

    [Tooltip("If an enemy starts slightly off NavMesh, Warp it to the nearest valid NavMesh position.")]
    [SerializeField] private bool _snapEnemiesToNearestNavMesh = true;

    [SerializeField, Min(0.1f)] private float _navMeshSampleRadius = 4f;

    [Header("Debug")]
    [SerializeField] private bool _logSetup = true;

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        ResolveActors();
        ValidateEnemyNavMeshAgents();
        AlertEnemies();
    }

    [ContextMenu("Resolve Battle Test Actors")]
    // Purpose: Resolves the actors needed by this component.
    private void ResolveActors()
    {
        if (!_autoFindActors)
            return;

        if (_player == null)
            _player = FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Exclude);

        if (_enemyCombatControllers == null || _enemyCombatControllers.Length == 0)
        {
            _enemyCombatControllers =
                FindObjectsByType<EnemyShooterCombatController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
        }

        if (!_logSetup)
            return;

        Debug.Log(
            $"[BattleTestSceneBootstrap] actors resolved. player={(_player != null ? _player.name : "null")}, enemyCombatControllers={(_enemyCombatControllers != null ? _enemyCombatControllers.Length : 0)}",
            this);
    }

    // Purpose: Validates enemy nav mesh agents and reports invalid authored data.
    private void ValidateEnemyNavMeshAgents()
    {
        if (!_validateEnemyNavMesh || _enemyCombatControllers == null)
            return;

        for (int i = 0; i < _enemyCombatControllers.Length; i++)
        {
            EnemyShooterCombatController combat = _enemyCombatControllers[i];
            if (combat == null)
                continue;

            NavMeshAgent agent =
                combat.GetComponent<NavMeshAgent>() ??
                combat.GetComponentInParent<NavMeshAgent>();

            if (agent == null)
            {
                if (_logSetup)
                    Debug.LogWarning(
                        $"[BattleTestSceneBootstrap] Enemy has no NavMeshAgent. enemy={combat.name}",
                        combat);

                continue;
            }

            if (agent.isOnNavMesh)
            {
                if (_logSetup)
                    Debug.Log(
                        $"[BattleTestSceneBootstrap] Enemy NavMeshAgent ready. enemy={agent.name}, pos={agent.transform.position}",
                        agent);

                continue;
            }

            if (!_snapEnemiesToNearestNavMesh)
            {
                Debug.LogWarning(
                    $"[BattleTestSceneBootstrap] Enemy is off NavMesh. enemy={agent.name}, pos={agent.transform.position}",
                    agent);
                continue;
            }

            if (NavMesh.SamplePosition(
                    agent.transform.position,
                    out NavMeshHit hit,
                    _navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                agent.Warp(hit.position);

                if (_logSetup)
                    Debug.Log(
                        $"[BattleTestSceneBootstrap] Enemy warped to NavMesh. enemy={agent.name}, from={combat.transform.position}, to={hit.position}",
                        agent);
            }
            else
            {
                Debug.LogWarning(
                    $"[BattleTestSceneBootstrap] Enemy is off NavMesh and no nearby NavMesh was found. enemy={agent.name}, pos={agent.transform.position}, sampleRadius={_navMeshSampleRadius}",
                    agent);
            }
        }
    }

    // Purpose: Performs the alert enemies workflow for this component.
    private void AlertEnemies()
    {
        if (!_alertEnemiesToPlayerOnStart || _enemyCombatControllers == null)
            return;

        if (_player == null)
        {
            Debug.LogWarning(
                "[BattleTestSceneBootstrap] Cannot alert enemies because PlayerCharacter was not found.",
                this);
            return;
        }

        for (int i = 0; i < _enemyCombatControllers.Length; i++)
        {
            EnemyShooterCombatController combat = _enemyCombatControllers[i];
            if (combat == null)
                continue;

            combat.AlertToPlayer();
        }
    }
}
