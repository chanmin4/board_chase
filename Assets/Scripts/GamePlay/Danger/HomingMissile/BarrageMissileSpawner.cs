using UnityEngine;

[DisallowMultipleComponent]
public class BarrageMissileSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;                // 보드 크기/중앙 계산용(앵커가 없을 때만 사용)
    public SurvivalGauge gauge;            // 맞았을 때 패널티 적용(선택)

    [Header("Missile Prefab")]
    public SmallHomingMissile missilePrefab;

    [Header("Spawn Anchors")]
    [Tooltip("상/하/좌/우 센터에 배치한 앵커들. 비어 있으면 자동 좌표를 사용합니다.")]
    public Transform[] anchors;
    public bool randomPickAnchor = true;

    [Header("Time-Based Spawn")]
    public bool useTimeRate = true;          // ★ true면 초당 스폰 모드 사용
    [Min(0.01f)] public float spawnInterval = 5f; // ★ N초 간격

    [Header("Spawn Geometry (fallback when no anchors)")]
    [Tooltip("보드 중앙으로부터의 스폰 반경(월드 단위). 0 이면 자동(보드의 1/4)")]
    public float spawnRadiusWorld = 0f;
    public float spawnY = 1.0f;            // ★ 회전벽 피하려고 기본 1

    [Header("Missile Tuning")]
    [Min(0f)] public float firstSpawnDelay = 0f;

    public float missileSpeed = 6f;
    public float hitRadiusWorld = 2.0f;
    public float timeoutRadiusWorld = 0.8f;
    public float gaugePenaltyOnHit = 10f;
    public int missileCount = 1;
    public bool uniqueAnchorsPerBurst = true;       // true면 한 번의 발사에서 서로 다른 앵커를 우선 사용
                                                    // 내부
    public float missileLifetime = 5f;
    float spawnTimer = 0f; //내부 타이머
    public float lastFireTime { get; private set; } = -1f;
    public event System.Action MissileLaunch;
    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!gauge)    gauge    = FindAnyObjectByType<SurvivalGauge>();
    }
    void OnEnable()
    {
        spawnTimer = 0f;
        lastFireTime = -1f; // 첫 스폰 전임을 나타내는 센티넬
    }

    void Update()
    {
        if (!useTimeRate) return;                 // ★ 사이클 모드면 여기선 안 함
        if (spawnInterval <= 0f) return;              // ★ 방어

        // ★ 첫 스폰 지연 처리: lastFireTime < 0 이면 아직 한 번도 안 쏜 상태
        if (lastFireTime < 0f)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= firstSpawnDelay)
            {
                spawnTimer = 0f;
                FireMissile();
                lastFireTime = Time.time;
            }
            return; // 첫 발사 전에는 아래 루프를 돌지 않음
        }

        // 이후부터는 기존 간격 루프 그대로
        spawnTimer += Time.deltaTime;
        while (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            FireMissile();
            lastFireTime = Time.time - spawnTimer;
        }
    
        
    }


    void FireMissile()
    {
        if (!missilePrefab || !director) return;
        float baseY = board ? board.origin.y : 0f;
        int count = Mathf.Max(1, missileCount);
        // 1) 앵커가 있으면 거기서 랜덤 선택
        if (anchors != null && anchors.Length > 0)
        {
            int anchorL = anchors.Length;
            int idx = Random.Range(0, anchorL);
            if (uniqueAnchorsPerBurst)
            {
                //일단 최대한 서로다른앵커 발사
                int use = Mathf.Min(count, anchorL);
                for (int k = 0; k < use; k++)
                {
                    var a = anchors[(idx + k) % anchorL];
                    Vector3 p = a ? a.position : transform.position;
                     p = new Vector3(p.x, baseY + spawnY, p.z);   // [FIX] XZ=앵커, Y=보드origin+오프셋 
                    SpawnOne(p);
                }
                // 남는 분량은 랜덤 앵커에서 추가 발사
                for (int k = anchorL; k < count; k++)
                {
                    var a = anchors[Random.Range(0, anchorL)];
                    Vector3 p = a ? a.position : transform.position;
                     p = new Vector3(p.x, baseY + spawnY, p.z);   // [FIX]
                    SpawnOne(p);
                }
            }
            else
            {
                // 같은 앵커에서 count개 발사
                var a = anchors[idx];
                Vector3 p = a ? a.position : transform.position;
                 p = new Vector3(p.x, baseY + spawnY, p.z);  
                for (int k = 0; k < count; k++) SpawnOne(p);
            }
        }
        MissileLaunch.Invoke();
    }

    void SpawnOne(Vector3 pos)
    {
        var m = Instantiate(missilePrefab, pos, Quaternion.identity, transform);
        var hm = m.GetComponent<SmallHomingMissile>();
        Transform player = director.player
            ? director.player
            : GameObject.FindGameObjectWithTag("Player")?.transform;

        var g = gauge ? gauge : director ? director.gauge : null;

        if (hm) hm.Setup(director, player, missileLifetime, missileSpeed,
                     hitRadiusWorld, timeoutRadiusWorld, g, pos.y);

        
        hm.gaugePenaltyOnHit = gaugePenaltyOnHit;
    }
}
