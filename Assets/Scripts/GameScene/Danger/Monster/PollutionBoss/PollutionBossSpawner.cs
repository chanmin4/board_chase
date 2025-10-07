using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PollutionBossSpawner : MonoBehaviour
{
    [Header("Scene Refs (Spawner holds them)")]
    public BoardGrid board;                 // 필수
    public Transform player;                // 보통 PlayerDisk
    public SurvivalDirector director;       // 선택

    [Header("Boss Prefab")]
    [Tooltip("프리팹 폴더에 보관되는 보스 프리팹. 씬 레퍼런스는 비워둠.")]
    public PollutionBoss bossPrefab;        // 있으면 사용, 없으면 런타임 Sphere 생성
    [Tooltip("루트에 HomingRocket가 붙은 '로켓 자체' 프리팹이어야 함.")]
    public HomingRocket rocketPrefab;       // 보스가 쏠 로켓(필수)

    [Header("Spawn Timing")]
    public bool spawnAtStart = true;
    [Min(0)] public float spawnDelay = 0f;  // 초

    [Header("Options")]
    public bool destroyExistingRocketSpawner = true; // 기존 RocketHazardSystem 제거

    [Header("Fallback Visual (when no prefab)")]
    public Material fallbackURPLitMaterial;

    // ───────── 겹침 회피 설정 ─────────
    [Header("Spawn Placement (Avoid Overlap)")]
    [Tooltip("겹치면 보드 내부에서 랜덤 재배정")]
    public bool avoidOverlap = true;
    [Tooltip("보스 반지름을 알 수 없을 때 사용할 기본값")]
    public float defaultBossRadius = 0.6f;
    [Tooltip("겹침 판정에 더할 여유 거리")]
    public float spawnSearchPadding = 0.2f;
    [Tooltip("플레이어와 최소 거리 보장(센터~센터)")]
    public float minDistanceFromPlayer = 1.0f;
    [Tooltip("랜덤 재시도 횟수")]
    [Min(1)] public int maxPlacementTries = 30;
    [Tooltip("이 레이어들과 겹치면 재배정 (벽/지형/적/오브젝트 등)")]
    public LayerMask blockLayers = ~0; // 기본: 전부 고려

    void Reset()
    {
        board    = FindAnyObjectByType<BoardGrid>();
        director = FindAnyObjectByType<SurvivalDirector>();
        if (!player && director) player = director.player;
    }

    void Start()
    {
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!player && director) player = director.player;

        if (destroyExistingRocketSpawner)
        {
            var old = FindAnyObjectByType<RocketHazardSystem>();
            if (old) Destroy(old.gameObject);
        }

        if (spawnAtStart) Invoke(nameof(SpawnNow), Mathf.Max(0f, spawnDelay));
    }

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        if (!board)        { Debug.LogError("[BossSpawner] BoardGrid 없음"); return; }
        if (!rocketPrefab) { Debug.LogError("[BossSpawner] HomingRocket '로켓 프리팹' 필요"); return; }

        // 보스 반지름 추정(프리팹 있으면 그 값 사용)
        float bossRadius =
            bossPrefab ? Mathf.Max(0.05f, bossPrefab.bossRadius) : Mathf.Max(0.05f, defaultBossRadius);

        // 스폰 위치 결정(중앙 우선, 겹치면 재배정)
        Vector3 spawnPos = GetBoardCenter();
        if (avoidOverlap && !IsSpawnPositionFree(spawnPos, bossRadius))
        {
            if (!TryFindFreeSpawn(out spawnPos, bossRadius))
            {
                Debug.LogWarning("[BossSpawner] 빈 공간을 찾지 못했습니다. 중앙에 강제 스폰합니다.");
                spawnPos = GetBoardCenter();
            }
        }

        PollutionBoss bossInstance;

        if (bossPrefab)
        {
            bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 프리팹이 없을 경우: 런타임 생성 + 기본 비주얼
            var go = new GameObject("PollutionBoss(Auto)");
            go.transform.position = spawnPos;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = PrimitiveSphere();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = fallbackURPLitMaterial ? fallbackURPLitMaterial : DefaultLit(new Color(0.9f, 0.2f, 0.2f, 1f));

            var col = go.AddComponent<SphereCollider>(); // PollutionBoss가 Require 하므로 있어도 됨
            col.isTrigger = true;

            bossInstance = go.AddComponent<PollutionBoss>();
        }

        // 스포너가 **씬 레퍼런스 주입**
        bossInstance.Setup(
            board: board,
            player: player ? player : (director ? director.player : null),
            director: director,
            rocketPrefab: rocketPrefab
        );
    }

    // ───────── 위치 유틸 ─────────
    Vector3 GetBoardCenter()
    {
        Vector3 center = board.origin + new Vector3(
            board.width * board.tileSize * 0.5f,
           board ? board.origin.y : 0f,
            board.height * board.tileSize * 0.5f
        );
        return center;
    }

    bool TryFindFreeSpawn(out Vector3 pos, float bossRadius /*, float spawnYOffset = 0f*/)
    {
        // 보드 외곽 사각(XZ)
        Rect r = new Rect(
            board.origin.x,
            board.origin.z,
            board.width * board.tileSize,
            board.height * board.tileSize
        );

        float pad = Mathf.Max(0f, bossRadius + spawnSearchPadding);
        float minX = r.xMin + pad;
        float maxX = r.xMax - pad;
        float minZ = r.yMin + pad;
        float maxZ = r.yMax - pad;

        // 유효 공간이 없으면 중앙 반환
        if (minX >= maxX || minZ >= maxZ)
        {
            pos = GetBoardCenter();
            return false;
        }

        float yBase =  board ? board.origin.y : 0f; // + spawnYOffset;   // 필요하면 오프셋 더해 사용

        // 첫 번째 시도=중앙, 이후 랜덤
        for (int i = 0; i < maxPlacementTries; i++)
        {
            Vector3 p = (i == 0)
                ? GetBoardCenter()
                : new Vector3(
                    UnityEngine.Random.Range(minX, maxX),
                    yBase,
                    UnityEngine.Random.Range(minZ, maxZ)
                  );

            if (IsSpawnPositionFree(p, bossRadius))
            {
                pos = p;
                return true;
            }
        }

        // 실패 시 중앙
        pos = GetBoardCenter();
        return false;
    }


    bool IsSpawnPositionFree(Vector3 p, float bossRadius)
    {
        // 플레이어와 최소 거리
        if (player)
        {
            float minDist = bossRadius + Mathf.Max(0f, minDistanceFromPlayer);
            Vector2 a = new Vector2(p.x, p.z);
            Vector2 b = new Vector2(player.position.x, player.position.z);
            if ((a - b).sqrMagnitude < (minDist * minDist)) return false;
        }

        // 물리 겹침 체크 (트리거 제외 / IgnoreTriggers)
        float radius = bossRadius + spawnSearchPadding;
        // y는 씬 환경에 따라 다를 수 있으니, 높이 영향 최소화를 위해 OverlapSphere 사용
        var hits = Physics.OverlapSphere(new Vector3(p.x, p.y + 0.1f, p.z), radius, blockLayers, QueryTriggerInteraction.Ignore);

        // 자신이 아닌(스폰 전이므로 전부 타물체) 콜라이더가 하나라도 있으면 실패
        if (hits != null && hits.Length > 0) return false;

        return true;
    }

    // ── 내부 유틸: 프리미티브/머티리얼 ─────────────────────────
    static Mesh _sphere;
    static Mesh PrimitiveSphere()
    {
        if (_sphere) return _sphere;
        var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return _sphere;
    }

    static Material _defaultMat;
    static Material DefaultLit(Color c)
    {
        if (_defaultMat) return _defaultMat;
        _defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _defaultMat.color = c;
        return _defaultMat;
    }
}
