using UnityEngine;

/// 플레이어 잉크를 추적해서 '클린(지우기)' 하고,
/// 누적 섭취량이 임계치에 도달하면 현재 위치를 커다란 오염으로 뒤덮는다.
/// - 이동: 가장 가까운/진한 플레이어 잉크 방향으로 유도(샘플 기반)
/// - '먹기': 반경 eatRadius로 ClearPlayerCircleWorld_Batched 호출 + 양 누적
/// - '폭발(토해내기)': 누적량 ≥ threshold → ContaminateCircleWorld_Batched


public class PollutionInkEater : MonoBehaviour
{
    [System.Serializable]
    public struct Settings
    {
        // Movement
        public float moveSpeed;
        public float turnSpeed;

        // Eating
        public float eatRadius;
        public float eatTick;
        public float eatUnit;
        public float burstThreshold;
        public float burstRadius;
        public float burstCooldown;
        public LayerMask killByLayers; // 이 레이어에 맞으면 히트
        public int hitsToKill;
    }



    [Header("Refs")]
    public BoardMaskRenderer maskRenderer;
    public BoardGrid board;
    public Transform targetPlayer; // 필요하면 이동시 회피 등에 사용

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 360f;
    public int seekSamples = 16;       // 샘플 갯수(원주 분할)

    [Header("Eating")]
    public float eatRadius = 1.2f;       // 실제로 지울 반경
    public float eatTick = 0.15f;        // 지우기 주기(초)
    public float burstThreshold = 100f;  // 임계치(누적 섭취량)
    public float burstRadius = 4.5f;     // 오염 반경
    public float burstCooldown = 1.0f;   // 폭발 후 딜레이



    [Header("InkEater State (debug)")]
    [SerializeField] float eaten = 0f;
    [SerializeField] float eatTimer = 0f;
    [SerializeField] float burstCd = 0f;
    public bool useBoardWideSeek = true;

    int _hp;
    LayerMask killByLayers;
    public void ApplySettings(Settings s)
    {
        moveSpeed = s.moveSpeed;
        turnSpeed = s.turnSpeed;

        eatRadius = Mathf.Max(0.01f, s.eatRadius);
        eatTick = Mathf.Max(0.02f, s.eatTick);
        burstThreshold = Mathf.Max(0f, s.burstThreshold);
        burstRadius = Mathf.Max(0.01f, s.burstRadius);
        burstCooldown = Mathf.Max(0f, s.burstCooldown);
        killByLayers = s.killByLayers;
        _hp = Mathf.Max(1, s.hitsToKill);
    }

    void Awake()
    {
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!board) board = maskRenderer ? maskRenderer.board : FindAnyObjectByType<BoardGrid>();
    }

    void Update()
    {
        if (!maskRenderer || !board) return;

        // 1) 잉크가 '많은 쪽'으로 간단 유도 (원주 샘플링)
        Vector3 dir = SampleInkGradient();
        if (dir.sqrMagnitude > 0.0001f)
        {
            // 회전
            Quaternion tgt = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, tgt, turnSpeed * Time.deltaTime);
        }
        // 전진
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // 2) 먹기 틱
        eatTimer -= Time.deltaTime;
        if (eatTimer <= 0f)
        {
            eatTimer = eatTick;

            // 실제 지워진 면적만큼 섭취
            float clearedArea;
            int clearedPx = maskRenderer.ClearPlayerCircleWorld_Count(transform.position, eatRadius, out clearedArea);

            if (clearedPx > 0)
            {
                eaten += clearedArea; // ← 정확한 월드 m^2 누적
                                      // 필요하면 계수 곱: eaten += clearedArea * eatAreaToUnitMul;
            }
        }


        // 3) 폭발(토해내기)
        if (burstCd > 0f) burstCd -= Time.deltaTime;
        if (eaten >= burstThreshold && burstCd <= 0f)
        {
            maskRenderer.EnemyCircleWorld_Batched(transform.position, burstRadius);
            eaten = 0f;
            burstCd = burstCooldown;
        }
    }

    // 주변 플레이어 잉크 밀도를 샘플링해서 '가장 유리한 방향'을 반환
    Vector3 SampleInkGradient()
    {
        // 탐색 최대 거리 결정: 전역 탐색이면 보드 반대각선/2, 아니면 기존 반경
        float maxR = BoardHalfDiagonal();

        int rays = Mathf.Max(8, seekSamples); // 방향 샘플 수
        int steps = 24;                        // 각 방향으로 몇 번 전진하며 확인할지
        float step = maxR / steps;

        Vector3 pos = transform.position;
        Vector3 bestDir = Vector3.zero;
        float bestScore = 0f;

        for (int i = 0; i < rays; i++)
        {
            float ang = (Mathf.PI * 2f) * (i / (float)rays);
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));

            // 이 방향으로 레이-마치: 가장 가까운 "플레이어 잉크"를 찾으면 점수 = 가까울수록 높게
            float score = 0f;
            for (int s = 1; s <= steps; s++)
            {
                float dist = s * step;
                Vector3 p = pos + dir * dist;

                if (maskRenderer.IsPlayerPaintedWorld(p))
                {
                    // 가까울수록 점수↑ (0~1 범위)
                    score = 1f - (dist / maxR);
                    break; // 첫 발견 지점에서 멈춤
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        if (bestScore <= 0f)
        {
            // 잉크를 못 찾으면 플레이어 쪽으로 느슨히 이동(혹은 랜덤 워크)
            if (targetPlayer)
            {
                Vector3 d = targetPlayer.position - pos;
                d.y = 0f;
                if (d.sqrMagnitude > 0.001f) return d.normalized;
            }
            return Random.insideUnitSphere * 0.5f; // 완전 무정보면 약한 랜덤
        }
        return bestDir.normalized;
    }
    float BoardHalfDiagonal()
    {
        if (!board) return 0;
        float w = board.width * board.tileSize;
        float h = board.height * board.tileSize;
        return 0.5f * Mathf.Sqrt(w * w + h * h);
    }
      void OnCollisionEnter(Collision other)
    {
        int l = other.collider.gameObject.layer;
        if ((killByLayers.value & (1 << l)) == 0) return;

        // 미충족: 기존처럼 HP만 감소, 물리 충돌로 튕김
        TakeHit();
    }


    void TakeHit()
    {
        _hp--;
        if (_hp <= 0)
        {
            Die();
        }
    }


    void Die()
    {
        // 필요하면 이펙트/사운드 추가
        MobSpawnManager.Instance?.ReportMobKilled(MobType.InkEater);
        Destroy(gameObject);
    }



}

static class V3Ext
{
    public static Vector3 WithY0(this Vector3 v) => new Vector3(v.x, 0f, v.z);
}
