using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    [Header("Ref Don't Touch")]
    [ReadOnly] public Vector2Int coord;
    [ReadOnly] public bool isOpened;
    [ReadOnly] public bool is_startsector = false;

    public Transform cameraPoint;
    [Tooltip("실제 적 스폰 위치")]
    public Transform[] enemySpawnPoints;
    // 실제 적 생성 위치들. 기존 호환용 spawn point 배열.
    [Tooltip("spawn point medata, additional settings for each spawn point. can be empty for basic spawning using enemySpawnPoints only.")]
    [SerializeField] private EnemySpawnPoint[] _spawnPointMetadata;
    //이포인트 비활성 , 이포인트 보스전용 등등 전용 설정가능
    // 각 spawn point의 추가 설정용 메타데이터 배열.
    // 비어 있어도 enemySpawnPoints만으로 기본 스폰은 가능.
    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;

    [Header("Fallback Bounds")]
    [SerializeField] private Vector3 fallbackCenterOffset = Vector3.zero;
    [SerializeField] private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);

    public Vector2Int Coord => coord;
    public bool IsOpened => isOpened;
    public bool IsStartSector => is_startsector;
    public EnemySpawnPoint[] SpawnPointMetadata => _spawnPointMetadata;

    public void SetRuntimeInfo(Vector2Int newCoord, bool opened, bool isStartSector)
    {
        coord = newCoord;
        isOpened = opened;
        is_startsector = isStartSector;
    }

    public void SetOpened(bool opened)
    {
        isOpened = opened;
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
                (minZ + maxZ) * 0.5f
            );

            Vector3 size = new Vector3(
                Mathf.Abs(maxX - minX),
                5f,
                Mathf.Abs(maxZ - minZ)
            );

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
        if ((_spawnPointMetadata == null || _spawnPointMetadata.Length == 0) && enemySpawnPoints != null && enemySpawnPoints.Length > 0)
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
