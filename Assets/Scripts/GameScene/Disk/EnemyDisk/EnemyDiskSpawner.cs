using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDiskSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;
    public SurvivalGauge gauge;
    public Transform targetPlayer;

    [Header("Prefab")]
    public EnemyDiskLauncher enemyDiskPrefab; // EnemyDisk.cs 안의 EnemyDiskLauncher 사용

    [Header("Spawn Timing")]
    [Tooltip("첫 스폰까지 대기 시간(초)")]
    public float firstSpawnDelay = 1.0f;
    [Tooltip("스폰 간격(초)")]
    public float spawnInterval = 3.0f;
    [Tooltip("한 번에 스폰할 개수")]
    public int spawnCount = 1;
    [Tooltip("동시에 존재 가능한 최대 수(초과 스폰 금지)")]
    public int maxAlive = 3;

    [Header("Spawn Rules (Board Grid)")]
    [Tooltip("보드 가장자리에서 띄우는 여유(월드 m)")]
    public float edgeMarginWorld = 0.5f;
    [Tooltip("플레이어로부터의 최소 거리(월드 m)")]
    public float minDistFromPlayer = 5f;
    [Tooltip("타일 중심에 스냅")]
    public bool  snapToTileCenter = true;
    [Tooltip("스폰 위치 시도 최대 횟수")]
    public int   maxTries = 24;
    [Tooltip("스폰 Y 고정값(보드 원점 Y에 더해짐)")]
    public float groundY = 0.2f;

    [Header("Lifecycle")]
    [Tooltip("OnEnable 때 자동으로 스폰 루프 시작")]
    public bool autoStart = true;

    // 내부
    readonly List<EnemyDiskLauncher> _alive = new List<EnemyDiskLauncher>();
    Coroutine _loop;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!gauge)    gauge    = FindAnyObjectByType<SurvivalGauge>();
        if (!targetPlayer)
        {
            if (director && director.player) targetPlayer = director.player;
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) targetPlayer = p.transform;
            }
        }
    }

    void OnEnable()
    {
        if (autoStart) StartSpawn();
    }

    void OnDisable()
    {
        StopSpawn();
    }

    // ─────────────────────────────────────
    // Public API
    // ─────────────────────────────────────
    public void StartSpawn()
    {
        if (_loop != null) return;
        _loop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawn()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    public bool SpawnOne()
    {
        if (!enemyDiskPrefab || !board) return false;
        CleanupDead();

        if (_alive.Count >= Mathf.Max(0, maxAlive)) return false;

        Vector3 pos = PickSpawnPos();
        var inst = Instantiate(enemyDiskPrefab, pos, Quaternion.identity, transform);

        // 필요 참조 주입(EnemyDiskLauncher 내부에서도 대부분 Find 사용하지만 안전하게)
        // inst.SetExternalSpeedMul / 등은 EnemyDiskLauncher에서 제공됨
        // 여기서는 참조만 채워둠(필요시 인스펙터로 프리팹 기본값 사용)
        // (EnemyDiskLauncher가 Awake에서 paint/mask FindAnyObject... 하므로 필수는 아님)

        _alive.Add(inst);
        return inst != null;
    }

    // ─────────────────────────────────────
    // Loop
    // ─────────────────────────────────────
    IEnumerator SpawnLoop()
    {
        if (firstSpawnDelay > 0f)
            yield return new WaitForSeconds(firstSpawnDelay);

        var wait = new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
        while (true)
        {
            CleanupDead();

            int canSpawn = Mathf.Max(0, maxAlive - _alive.Count);
            int toSpawn = Mathf.Min(Mathf.Max(0, spawnCount), canSpawn);

            for (int i = 0; i < toSpawn; i++)
                SpawnOne();

            yield return wait;
        }
    }

    void CleanupDead()
    {
        for (int i = _alive.Count - 1; i >= 0; --i)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }

    // ─────────────────────────────────────
    // Pick Position (PollutionGunnerSpawner 참고)
    // ─────────────────────────────────────
    Vector3 PickSpawnPos()
    {
        // 보드 로컬 축(회전 대응)
        Vector3 right = Vector3.ProjectOnPlane(board.transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;

        float U = board.width  * board.tileSize;  // 보드 가로 길이
        float V = board.height * board.tileSize;  // 보드 세로 길이

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

            // 로컬(u,v) → 월드
            Vector3 world = board.origin + right * u + forward * v;
            world.y = board.origin.y + groundY;

            // 플레이어와 최소 거리(XZ)
            Vector3 dz = world - new Vector3(playerPos.x, world.y, playerPos.z);
            dz.y = 0f;
            if (dz.sqrMagnitude < minSqr) continue;

            return world;
        }

        // 실패 시 보드 중앙
        Vector3 center = board.origin + right * (U * 0.5f) + forward * (V * 0.5f);
        center.y = board.origin.y + groundY;
        return center;
    }
}
