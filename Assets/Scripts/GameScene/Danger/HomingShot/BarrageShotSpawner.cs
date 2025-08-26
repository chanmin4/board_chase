using UnityEngine;

[DisallowMultipleComponent]
public class BarrageMissileSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;                // 보드 크기/중앙 계산용(앵커가 없을 때만 사용)
    public SurvivalGauge gauge;            // 맞았을 때 패널티 적용(선택)

    [Header("Missile Prefab")]
    public SmallHomingShot missilePrefab;

    [Header("Spawn Anchors")]
    [Tooltip("상/하/좌/우 센터에 배치한 앵커들. 비어 있으면 자동 좌표를 사용합니다.")]
    public Transform[] anchors;
    public bool randomPickAnchor = true;

    [Header("Spawn Rule")]
    [Tooltip("true면 (2,4,6,…) 짝수 사이클마다 1발 발사")]
    public bool fireOnEvenCycles = true;   // “두 번째 lifetime마다”
    [Tooltip("true면 (1,3,5,…) 홀수 사이클마다 1발 발사")]
    public bool fireOnOddCycles  = false;
    public int  everyN = 0;              // 0이면 미사용, N주기마다 스폰

    [Header("Spawn Geometry (fallback when no anchors)")]
    [Tooltip("보드 중앙으로부터의 스폰 반경(월드 단위). 0 이면 자동(보드의 1/4)")]
    public float spawnRadiusWorld = 0f;
    public float spawnY = 1.0f;            // ★ 회전벽 피하려고 기본 1

    [Header("Missile Tuning")]
    public float missileSpeed = 6f;
    public float hitRadiusWorld = 2.0f;
    public float timeoutRadiusWorld = 0.8f;
    public float gaugePenaltyOnHit = 1.2f;

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
        bool should = (fireOnEvenCycles && (seq % 2 == 0)) || (fireOnOddCycles && (seq % 2 == 1));
        if (!should || !missilePrefab || !director) return;

        Vector3 pos;
        // 1) 앵커가 있으면 거기서 랜덤 선택
        if (anchors != null && anchors.Length > 0)
        {
            int idx = randomPickAnchor ? Random.Range(0, anchors.Length) : (seq % anchors.Length);
            var a = anchors[idx];
            pos = a ? a.position : transform.position;
        }
        else
        {
            // 2) 앵커가 없으면 기존 자동 좌표
            float radius = spawnRadiusWorld > 0f
                         ? spawnRadiusWorld
                         : (board ? Mathf.Min(board.width, board.height) * board.tileSize * 0.25f : 5f);

            Vector3[] auto =
            {
                boardCenter + new Vector3( 0f, 0f,  radius),
                boardCenter + new Vector3( 0f, 0f, -radius),
                boardCenter + new Vector3( radius, 0f,  0f),
                boardCenter + new Vector3(-radius, 0f,  0f)
            };
            int idx = Random.Range(0, auto.Length);
            pos = auto[idx];
        }

        pos.y = spawnY; // ★ 항상 Y를 1로 올려 회전벽 무시
        SpawnOne(pos);
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
