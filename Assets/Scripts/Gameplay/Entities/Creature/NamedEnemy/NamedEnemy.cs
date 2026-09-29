/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy
 * Primary responsibility: Implements the focused NamedEnemy responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class NamedEnemy : Enemy
{
    [Header("Named Runtime")]
    [SerializeField] private NamedEnemySpawnInfoEventChannelSO _spawnInfoChannel;

    public SectorRuntime SourceSector { get; private set; }
    public Transform SpawnPoint { get; private set; }

    public bool HasSpawnContext => SpawnPoint != null;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_spawnInfoChannel != null)
            _spawnInfoChannel.OnEventRaised += HandleSpawnContext;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected override void OnDisable()
    {
        if (_spawnInfoChannel != null)
            _spawnInfoChannel.OnEventRaised -= HandleSpawnContext;

        SourceSector = null;
        SpawnPoint = null;

        base.OnDisable();
    }

    // Purpose: Handles the incoming spawn context request or event.
    private void HandleSpawnContext(NamedEnemySpawnInfo context)
    {
        if (context.namedEnemy != this)
            return;

        SourceSector = context.sourceSector;
        SpawnPoint = context.spawnPoint;
    }

    public Vector3 SpawnPosition
    {
        get
        {
            if (SpawnPoint != null)
                return SpawnPoint.position;

            return transform.position;
        }
    }
}
