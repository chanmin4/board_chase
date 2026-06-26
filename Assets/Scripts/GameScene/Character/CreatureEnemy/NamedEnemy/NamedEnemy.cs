using UnityEngine;

public class NamedEnemy : Enemy
{
    [Header("Named Runtime")]
    [SerializeField] private NamedEnemySpawnInfoEventChannelSO _spawnInfoChannel;

    public SectorRuntime SourceSector { get; private set; }
    public Transform SpawnPoint { get; private set; }

    public bool HasSpawnContext => SpawnPoint != null;

    private void OnEnable()
    {
        if (_spawnInfoChannel != null)
            _spawnInfoChannel.OnEventRaised += HandleSpawnContext;
    }

    protected override void OnDisable()
    {
        if (_spawnInfoChannel != null)
            _spawnInfoChannel.OnEventRaised -= HandleSpawnContext;

        SourceSector = null;
        SpawnPoint = null;

        base.OnDisable();
    }

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
