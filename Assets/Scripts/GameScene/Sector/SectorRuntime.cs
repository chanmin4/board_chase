// Assets/Scripts/GameScene/Sector/SectorRuntime.cs
using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    [Header("NeedRef Roots")]
    [SerializeField] private Transform _PatternObjectRoot;
    [SerializeField] private Transform[] _cleanupRoots;

    [Header("Runtime Anchors")]
    [Tooltip("Player spawn/reposition point used when this sector becomes the stage start sector. Falls back to cameraPoint, then this transform.")]
    [SerializeField] private Transform _playerStartPoint;

    [Header("Ref Don't Touch")]
    [ReadOnly] public Vector2Int coord;
    [ReadOnly] public bool isOpened;
    [ReadOnly] public bool isCleared;
    [ReadOnly] public bool is_startsector = false;

    public Transform cameraPoint;

    [Tooltip("Enemy spawn positions in this sector.")]
    public Transform[] enemySpawnPoints;

    [Tooltip("Optional spawn metadata. Can be empty when enemySpawnPoints are enough.")]
    [SerializeField] private EnemySpawnPoint[] _spawnPointMetadata;

    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;

    [Header("Fallback Bounds")]
    [SerializeField] private Vector3 fallbackCenterOffset = Vector3.zero;
    [SerializeField] private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);

    public Vector2Int Coord => coord;
    public bool IsOpened => isOpened;
    public bool IsCleared => isCleared;
    public bool IsStartSector => is_startsector;
    public EnemySpawnPoint[] SpawnPointMetadata => _spawnPointMetadata;
    public Transform PatternObjectRoot => _PatternObjectRoot != null ? _PatternObjectRoot : transform;
    public Transform[] CleanupRoots => _cleanupRoots;

    public Transform PlayerStartPoint
    {
        get
        {
            if (_playerStartPoint != null)
                return _playerStartPoint;

            if (cameraPoint != null)
                return cameraPoint;

            return transform;
        }
    }

    public void SetRuntimeInfo(Vector2Int newCoord, bool opened, bool isStartSector)
    {
        coord = newCoord;
        isOpened = opened;
        isCleared = false;
        is_startsector = isStartSector;
    }

    public void SetOpened(bool opened)
    {
        isOpened = opened;
    }

    public void SetCleared(bool cleared)
    {
        isCleared = cleared;
    }

    public Bounds GetWorldBounds()
    {
        if (HasAllSideBounds())
        {
            float minX = XMin.bound.bounds.center.x;
            float maxX = XMax.bound.bounds.center.x;
            float minZ = ZMin.bound.bounds.center.z;
            float maxZ = ZMax.bound.bounds.center.z;

            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                transform.position.y,
                (minZ + maxZ) * 0.5f);

            Vector3 size = new Vector3(
                Mathf.Abs(maxX - minX),
                5f,
                Mathf.Abs(maxZ - minZ));

            return new Bounds(center, size);
        }

        Vector3 fallbackCenter = transform.TransformPoint(fallbackCenterOffset);
        Vector3 fallbackSize = new Vector3(fallbackSizeXZ.x, 5f, fallbackSizeXZ.y);
        return new Bounds(fallbackCenter, fallbackSize);
    }

    private bool HasAllSideBounds()
    {
        return XMin != null && XMin.bound != null &&
               XMax != null && XMax.bound != null &&
               ZMin != null && ZMin.bound != null &&
               ZMax != null && ZMax.bound != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if ((_spawnPointMetadata == null || _spawnPointMetadata.Length == 0) &&
            enemySpawnPoints != null &&
            enemySpawnPoints.Length > 0)
        {
            EnemySpawnPoint[] found = new EnemySpawnPoint[enemySpawnPoints.Length];

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                Transform point = enemySpawnPoints[i];
                found[i] = point != null ? point.GetComponent<EnemySpawnPoint>() : null;
            }

            _spawnPointMetadata = found;
        }
    }
#endif
}