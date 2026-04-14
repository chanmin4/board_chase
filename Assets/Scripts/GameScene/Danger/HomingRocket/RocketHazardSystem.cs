using UnityEngine;

public class RocketHazardSystem : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Rocket Prefab")]
    public HomingRocket RocketPrefab;

    [Min(0.01f)] public float spawnInterval = 8f;   // N초마다
    [Header("Rocket Lifetime (sec)")]
    [Min(0f)] public float firstSpawnDelay = 0f;
    [Min(0.1f)] public float rocketLifetime = 5f; // 로켓 1발의 총 수명(초)
    public float spawnYOffset = 0f;
    public float homingSpeed = 7f;
    float spawnTimer = 0f;

    HomingRocket active;  

    
    public float lastFireTime { get; private set; } = -1f;

    public event System.Action RocketLaunch;
    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
    }
    void OnEnable()
    {
        spawnTimer = 0f;
        lastFireTime = -1f; // 첫 스폰 전 표식  
    }
    void Update()
    {
        if (!board || !director) return;


        // ★ 첫 스폰 지연
        if (lastFireTime < 0f)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= firstSpawnDelay)
            {
                spawnTimer = 0f;
                if (active) { active.Explode(); active = null; }
                SpawnRocket();
                lastFireTime = Time.time;
            }
            return;
        }

        // 이후부터는 기존 간격 루프
        spawnTimer += Time.deltaTime;
        while (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            if (active) { active.Explode(); active = null; }
            SpawnRocket();
            lastFireTime = Time.time - spawnTimer;
        }
    
    
    
    }
    void SpawnRocket()
    {
        if (!board || !director) return;                  // ★ 프리팹 체크 없음

        Vector3 center = board.origin + new Vector3(
            board.width * board.tileSize * 0.5f,
            0f,
            board.height * board.tileSize * 0.5f
        );
        center.y += spawnYOffset;

        HomingRocket r;
        if (RocketPrefab)
        {
            // ★ 부모 주지 말고 월드에 바로 생성 (UI 트리 영향 방지)
            r = Instantiate(RocketPrefab, center, Quaternion.identity);
        }
        else
        {
            // ★ 프리팹이 없으면 런타임 생성 폴백
            var go = new GameObject("HomingRocket");
            go.transform.position = center;
            r = go.AddComponent<HomingRocket>();

            // (선택) 프리뷰 링은 HomingRocket이 알아서 Cylinder 생성하므로 건드릴 필요 없음
            // r.previewRingPrefab = null;
        }

        active = r;

        var target = director ? director.player : null;
        float life = Mathf.Max(0.1f, rocketLifetime);
        r.Setup(director, life, target, homingSpeed, true);
        RocketLaunch?.Invoke();
    }

}
