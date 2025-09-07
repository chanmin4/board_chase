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

    [Header("Spawn Rule")]
    public int  spawncycle = 2;              // 0이면 미사용, N주기마다 스폰

    [Header("Spawn Geometry (fallback when no anchors)")]
    [Tooltip("보드 중앙으로부터의 스폰 반경(월드 단위). 0 이면 자동(보드의 1/4)")]
    public float spawnRadiusWorld = 0f;
    public float spawnY = 1.0f;            // ★ 회전벽 피하려고 기본 1

    [Header("Missile Tuning")]
    public float missileSpeed = 6f;
    public float hitRadiusWorld = 2.0f;
    public float timeoutRadiusWorld = 0.8f;
    public float gaugePenaltyOnHit = 1.2f;
    public int missileCount = 1;
    public bool uniqueAnchorsPerBurst = true;       // true면 한 번의 발사에서 서로 다른 앵커를 우선 사용

    int seq = 0;
    // 내부
    Vector3 boardCenter;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!gauge)    gauge    = FindAnyObjectByType<SurvivalGauge>();
        RecalcCenter();
    }

    void OnEnable()
    {
        if (director)
        {
            director.OnZonesResetSeq += HandleResetSeq;
            director.OnZonesReset    += RecalcCenter;
        }
    }
    void OnDisable()
    {
        if (director)
        {
            director.OnZonesResetSeq -= HandleResetSeq;
            director.OnZonesReset    -= RecalcCenter;
        }
    }

    void RecalcCenter()
    {
        if (board)
        {
            boardCenter = board.origin + new Vector3(board.width * board.tileSize * 0.5f, 0f,
                                                     board.height * board.tileSize * 0.5f);
        }
        else boardCenter = Vector3.zero;
    }

    void HandleResetSeq(int seq)
    {
        if (spawncycle <= 0) return;
        if ((seq % spawncycle) != 0) return;
        if (!missilePrefab || !director) return;
        int count = Mathf.Max(1, missileCount);
        // 1) 앵커가 있으면 거기서 랜덤 선택
        if (anchors != null && anchors.Length > 0)
        {
            int anchorL = anchors.Length;
            int idx = randomPickAnchor ? Random.Range(0, anchorL) : (seq % anchorL);
            if (uniqueAnchorsPerBurst)
            {
                //일단 최대한 서로다른앵커 발사
                int use = Mathf.Min(count, anchorL);
                for (int k = 0; k < use; k++)
                {
                    var a = anchors[(idx + k) % anchorL];
                    Vector3 p = a ? a.position : transform.position;
                    p.y = spawnY;
                    SpawnOne(p);
                }
                // 남는 분량은 랜덤 앵커에서 추가 발사
                for (int k = anchorL; k < count; k++)
                {
                    var a = anchors[Random.Range(0, anchorL)];
                    Vector3 p = a ? a.position : transform.position;
                    p.y = spawnY;
                    SpawnOne(p);
                }
            }
            else
            {
                // 같은 앵커에서 count개 발사
                var a = anchors[idx];
                Vector3 p = a ? a.position : transform.position;
                p.y = spawnY;
                for (int k = 0; k < count; k++) SpawnOne(p);
            }
        }
    }

    void SpawnOne(Vector3 pos)
    {
        var m = Instantiate(missilePrefab, pos, Quaternion.identity, transform);


        float life = director.SetDuration; // 이 사이클 동안만 유효

        Transform player = director.player
            ? director.player
            : GameObject.FindGameObjectWithTag("Player")?.transform;

        m.Setup(director, player, life, missileSpeed, hitRadiusWorld, timeoutRadiusWorld, gauge, spawnY);
        m.gaugePenaltyOnHit = gaugePenaltyOnHit;
    }
}
