using UnityEngine;

/// 플레이어 잉크를 추적해서 '클린(지우기)' 하고,
/// 누적 섭취량이 임계치에 도달하면 현재 위치를 커다란 오염으로 뒤덮는다.
/// - 이동: 가장 가까운/진한 플레이어 잉크 방향으로 유도(샘플 기반)
/// - '먹기': 반경 eatRadius로 ClearPlayerCircleWorld_Batched 호출 + 양 누적
/// - '폭발(토해내기)': 누적량 ≥ threshold → ContaminateCircleWorld_Batched
public class PollutionInkEater : MonoBehaviour
{
    [Header("Refs")]
    public BoardMaskRenderer maskRenderer;
    public BoardGrid board;
    public Transform targetPlayer; // 필요하면 이동시 회피 등에 사용

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 360f;
    public float seekRadius = 6f;        // 주변 탐색 반경
    public int   seekSamples = 16;       // 샘플 갯수(원주 분할)

    [Header("Eating")]
    public float eatRadius = 1.2f;       // 실제로 지울 반경
    public float eatTick = 0.15f;        // 지우기 주기(초)
    public float eatUnit = 3.14f;        // 1틱 섭취량(대략 면적: πr^2에 계수 곱 등 튜닝)
    public float burstThreshold = 100f;  // 임계치(누적 섭취량)
    public float burstRadius = 4.5f;     // 오염 반경
    public float burstCooldown = 1.0f;   // 폭발 후 딜레이

    [Header("InkEater State (debug)")]
    [SerializeField] float eaten = 0f;
    [SerializeField] float eatTimer = 0f;
    [SerializeField] float burstCd = 0f;

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
            // 현재 위치 주변에 플레이어 잉크가 있으면 지우고 섭취량 누적
            bool hasPlayerInk = maskRenderer.IsPlayerPaintedWorld(transform.position);
            if (hasPlayerInk)
            {
                maskRenderer.ClearPlayerCircleWorld_Batched(transform.position, eatRadius);
                eaten += eatUnit; // 단순 누적(필요하면 면적 기반으로: Mathf.PI*eatRadius*eatRadius*k)
            }
        }

        // 3) 폭발(토해내기)
        if (burstCd > 0f) burstCd -= Time.deltaTime;
        if (eaten >= burstThreshold && burstCd <= 0f)
        {
            maskRenderer.ContaminateCircleWorld_Batched(transform.position, burstRadius);
            eaten = 0f;
            burstCd = burstCooldown;
        }
    }

    // 주변 플레이어 잉크 밀도를 샘플링해서 '가장 유리한 방향'을 반환
    Vector3 SampleInkGradient()
    {
        float bestScore = 0f;
        Vector3 bestDir = Vector3.zero;

        Vector3 pos = transform.position;
        for (int i = 0; i < Mathf.Max(4, seekSamples); i++)
        {
            float ang = (Mathf.PI * 2f) * (i / (float)seekSamples);
            Vector3 d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            Vector3 p = pos + d * seekRadius * 0.8f; // 약간 안쪽을 본다

            // '플레이어 잉크 존재 여부'를 단순 스코어로
            float score = maskRenderer.IsPlayerPaintedWorld(p) ? 1f : 0f;

            // 가까운 쪽에 보너스(선택)
            score += 0.2f * (1f - (p - pos).magnitude / seekRadius);

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = d;
            }
        }

        // 샘플 전부 0이면 랜덤 워크(혹은 플레이어 반대 등)
        if (bestScore <= 0f)
            bestDir = (targetPlayer ? (targetPlayer.position - pos).normalized : Random.insideUnitSphere).WithY0();

        return bestDir.normalized;
    }
}

static class V3Ext
{
    public static Vector3 WithY0(this Vector3 v) => new Vector3(v.x, 0f, v.z);
}
