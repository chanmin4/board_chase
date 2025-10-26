using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PollutionGhostSpawner : MonoBehaviour
{
    [Header("Ghost Settings (Spawner controls)")]
    public PollutionGhost.GhostSettings settings = new PollutionGhost.GhostSettings
    {
        speed = 3.5f,
        randomizeSpeed = false,
        speedRange = new Vector2(2f, 5f),
        radiusWorld = 0.35f,
        groundY = 0.2f,
        lifetime = 8f,
        dropOnTimeout = false,
        contamRadiusWorld = 1.4f,
        enablePathContam = true,
        pathContamStartDelay = 2f,
        pathContamInterval = 1f,
        pathContamRadius = 1.0f,
        maxPathContamDrops = 0,
        killByLayers = ~0,
        hitsToKill = 1
    };

    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;
    public Transform player;
    public PollutionGhost mobPrefab;

    [Header("Timing")]
    [NonSerialized]public float firstDelay = 3f;
    [NonSerialized][Min(0.2f)] public float interval = 6f;
    public int countPerSpawn = 1;
    //public bool autoSpawn = true;

    [Header("Spawn Area")]
    public float edgePaddingTiles = 1f;     // 외벽과의 여유(타일)
    public float avoidPlayerRadius = 3.0f;  // 플레이어 주변 금지 반경(월드)
    //public float spawnY = 0.2f;
    public int   pickMaxTries = 20;

    [Header("Mob Defaults")]
    //public float mobSpeed = 3.5f;
    //public float mobLifetime = 8f;
    //public float contamRadiusWorld = 1.4f;

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
    }
    public bool SpawnOne()
    {
        if (!mobPrefab || !board) return false;

        Vector3 pos = PickSpawnPos();
        var m = Instantiate(mobPrefab, pos, Quaternion.identity, transform);
        m.ApplySettings(settings);
        m.director = director;                        // 참조 주입
        m.board    = board;
        m.player   = player;
        
        return m != null;
    }

    Vector3 PickSpawnPos()
    {
        var r = board.GetWallOuterRectXZ();
        float pad = Mathf.Max(0f, edgePaddingTiles) * board.tileSize;
        float baseY = board ? board.origin.y : 0f;

        float minX = r.xMin + pad, maxX = r.xMax - pad;
        float minZ = r.yMin + pad, maxZ = r.yMax - pad;

        Vector3 p = player ? player.position
                           : new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        for (int i = 0; i < pickMaxTries; i++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            var cand = new Vector3(x, baseY + settings.groundY, z);   // ★ HERE

            if ((new Vector2(cand.x - p.x, cand.z - p.z)).sqrMagnitude
                >= avoidPlayerRadius * avoidPlayerRadius)
                return cand;
        }

        // 실패 시 가장자리 보정
        float xEdge = (p.x < (minX + maxX) * 0.5f) ? maxX - 0.5f : minX + 0.5f;
        float zEdge = (p.z < (minZ + maxZ) * 0.5f) ? maxZ - 0.5f : minZ + 0.5f;
        return new Vector3(xEdge, baseY + settings.groundY, zEdge);   // ★ HERE
    }

}
