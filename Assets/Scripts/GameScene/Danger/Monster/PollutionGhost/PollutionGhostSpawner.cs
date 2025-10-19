using UnityEngine;

[DisallowMultipleComponent]
public class PollutionGhostSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;
    public Transform player;
    public PollutionGhost mobPrefab;

    [Header("Timing")]
    public float firstDelay = 3f;
    [Min(0.2f)] public float interval = 6f;
    public int countPerSpawn = 1;
    //public bool autoSpawn = true;

    [Header("Spawn Area")]
    public float edgePaddingTiles = 1f;     // 외벽과의 여유(타일)
    public float avoidPlayerRadius = 3.0f;  // 플레이어 주변 금지 반경(월드)
    public float spawnY = 0.2f;
    public int   pickMaxTries = 20;

    [Header("Mob Defaults")]
    public float mobSpeed = 3.5f;
    public float mobLifetime = 8f;
    public float contamRadiusWorld = 1.4f;

    float timer;
    bool firstDone;
    public event System.Action GhostSpawn;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!player)   player   = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void OnEnable()
    {
        timer = 0f; firstDone = false;
    }

    void Update()
    {
        if ( !mobPrefab || !board) return;

        if (!firstDone)
        {
            timer += Time.deltaTime;
            if (timer >= firstDelay) { timer = 0f; firstDone = true; SpawnBurst(); }
            return;
        }

        timer += Time.deltaTime;
        while (timer >= interval)
        {
            timer -= interval;
            SpawnBurst();
        }
    }

    [ContextMenu("Spawn Burst")]
    public void SpawnBurst()
    {
        int n = Mathf.Max(1, countPerSpawn);
        for (int i = 0; i < n; i++) SpawnOne();
        GhostSpawn?.Invoke();
    }

    public bool SpawnOne()
    {
        if (!mobPrefab || !board) return false;

        Vector3 pos = PickSpawnPos();
        var m = Instantiate(mobPrefab, pos, Quaternion.identity, transform);
        m.Setup(director, board, player, mobSpeed, mobLifetime, contamRadiusWorld);
        return m != null;
    }

    Vector3 PickSpawnPos()
    {
        var r = board.GetWallOuterRectXZ();
        float pad = Mathf.Max(0f, edgePaddingTiles) * board.tileSize;
float baseY = board ? board.origin.y : 0f;
        float minX = r.xMin + pad, maxX = r.xMax - pad;
        float minZ = r.yMin + pad, maxZ = r.yMax - pad;

        Vector3 p = player ? player.position : new Vector3((minX+maxX)*0.5f, 0f, (minZ+maxZ)*0.5f);

        for (int i = 0; i < pickMaxTries; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            var cand = new Vector3(x, baseY+spawnY, z);
            if ((new Vector2(cand.x - p.x, cand.z - p.z)).sqrMagnitude >= avoidPlayerRadius * avoidPlayerRadius)
                return cand;
        }

        // 실패 시: 플레이어와 반대쪽 가장자리 쪽으로 보정
        float xEdge = (p.x < (minX+maxX)*0.5f) ? maxX - 0.5f : minX + 0.5f;
        float zEdge = (p.z < (minZ+maxZ)*0.5f) ? maxZ - 0.5f : minZ + 0.5f;
        return new Vector3(xEdge, baseY+spawnY, zEdge);
    }
}
