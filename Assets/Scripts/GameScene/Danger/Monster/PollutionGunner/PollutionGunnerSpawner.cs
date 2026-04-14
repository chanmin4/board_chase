using UnityEngine;

public class PollutionGunnerSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;
    public SurvivalGauge gauge;
    public Transform targetPlayer;

    [Header("Prefab")]
    public PollutionGunner gunnerPrefab;

    [Header("Spawn Rules (Board Grid)")]
    public float edgeMarginWorld = 0.5f;
    public float minDistFromPlayer = 5f;
    public bool  snapToTileCenter = true;
    public int   maxTries = 24;

    [Header("Gunner Settings (Spawner controls)")]
    public PollutionGunner.Settings settings = new PollutionGunner.Settings
    {
        // Movement
        moveSpeed = 3.5f,
        retreatSpeed = 6.0f,
        retreatDuration = 2.0f,
        turnSpeed = 360f,
        groundY = 0.2f,
        postFireDelay = 2f,
        // Missile
        missilePrefab = null,      // 인스펙터에서 SmallHomingMissile 할당
        missileLifetime = 5f,
        missileSpeed = 6f,
        hitRadiusWorld = 2.0f,
        timeoutRadiusWorld = 0.8f,
        gaugePenaltyOnHit = 8f,
        fireHeightOffset = 1.0f,



        // Clamp
        edgeMarginWorld = 0.5f,
        //hit/hp
        killByLayers = ~0,          // 기본: 어떤 레이어와 부딪혀도 히트
        hitsToKill = 2       
    };

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!gauge)    gauge    = FindAnyObjectByType<SurvivalGauge>();
        if (!targetPlayer) targetPlayer = (director && director.player) ? director.player
                               : GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public bool SpawnOne()
    {
        if (!gunnerPrefab || !board) return false;

        Vector3 pos = PickSpawnPos();
        var inst = Instantiate(gunnerPrefab, pos, Quaternion.identity,transform);

        // 주입 + 참조
        settings.edgeMarginWorld = edgeMarginWorld; // 스포너 규칙 반영
        inst.ApplySettings(settings);
        inst.director = director;
        inst.board    = board;
        inst.gauge    = gauge;
        inst.targetPlayer = targetPlayer;

        return inst != null;
    }

    // === 보드 로컬(회전 대응) + 플레이어 최소거리 스폰 ===
    Vector3 PickSpawnPos()
    {
        // 보드 로컬 축 얻기
        Vector3 right = Vector3.ProjectOnPlane(board.transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;

        float U = board.width * board.tileSize; // 보드 로컬 가로 길이
        float V = board.height * board.tileSize; // 보드 로컬 세로 길이

        float m = Mathf.Clamp(edgeMarginWorld, 0f, Mathf.Min(U, V) * 0.49f);
        float minSqr = minDistFromPlayer * minDistFromPlayer;

        Vector3 playerPos = targetPlayer ? targetPlayer.position : board.origin;

        for (int i = 0; i < Mathf.Max(1, maxTries); i++)
        {
            float u = Random.Range(m, U - m);
            float v = Random.Range(m, V - m);

            if (snapToTileCenter && board.tileSize > 0f)
            {
                u = (Mathf.Floor(u / board.tileSize) + 0.5f) * board.tileSize;
                v = (Mathf.Floor(v / board.tileSize) + 0.5f) * board.tileSize;
            }

            // 로컬(u,v) → 월드로 변환
            Vector3 world = board.origin + right * u + forward * v;
            world.y = board.origin.y + settings.groundY;   // ★ Y 고정

            // 플레이어와 최소 거리 유지(XZ)
            Vector3 dz = world - new Vector3(playerPos.x, world.y, playerPos.z);
            dz.y = 0f;
            if (dz.sqrMagnitude < minSqr) continue;

            return world;
        }

        // 실패 시 보드 중앙
        Vector3 center = board.origin + right * (U * 0.5f) + forward * (V * 0.5f);
        center.y = board.origin.y + settings.groundY;
        return center;
    }

}
