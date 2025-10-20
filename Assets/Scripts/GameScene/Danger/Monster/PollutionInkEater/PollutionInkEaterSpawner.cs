using UnityEngine;

public class PollutionInkEaterSpawner : MonoBehaviour
{
    public PollutionInkEater inkEaterPrefab;
    public Transform targetPlayer;
    public BoardMaskRenderer maskRenderer;
    public BoardGrid board;
    [Header("Spawn Rules (Board Grid)")]
    public float edgeMarginWorld = 5f;      // 보드 테두리에서 이만큼 안쪽에서만 스폰
    public float minDistFromPlayer = 4.0f;    // 플레이어와 최소 거리
    public int maxTries = 24;               // 샘플 시도 횟수

    // ★★★ 여기서 전부 조절하게 묶음 노출 ★★★
    [Header("InkEater Settings")]
    public PollutionInkEater.Settings settings = new PollutionInkEater.Settings
    {
        // 기본치 예시(원하면 편하게 수정)
        moveSpeed = 3.5f,
        turnSpeed = 360f,
        eatRadius = 1.2f,
        eatTick = 0.15f,
        burstThreshold = 100f,
        burstRadius = 4.5f,
        burstCooldown = 1.0f,
        killByLayers = ~0,  // 기본: 모든 레이어와 충돌 시 히트 (원하면 발사체/디스크 레이어만 포함)
        hitsToKill = 1      // 기본 1히트 사망 (원하면 2,3…)
    };

    void Awake()
    {
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!targetPlayer) targetPlayer = FindAnyObjectByType<DiskLauncher>()?.transform;
    }

    public bool SpawnOne()
    {
        if (!inkEaterPrefab) return false;

        Vector3 p = PickSpawnPos();
        // ★ 스폰 높이: 보드 origin.y에 맞춤
        if (board) p.y = board.origin.y;

        var inst = Instantiate(inkEaterPrefab, p, Quaternion.identity,transform);
        if (!inst) return false;

        // 참조 주입
        inst.maskRenderer = maskRenderer;
        inst.board = board;
        inst.targetPlayer = targetPlayer;

        // ★ 설정 주입(핵심)
        inst.ApplySettings(settings);

        return true;
    }

    Vector3 PickSpawnPos()
    {
        if (!board)
            return Vector3.zero;

        // 보드 로컬 축(XZ 평면에 투영)
        Vector3 right = Vector3.ProjectOnPlane(board.transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;

        float totalU = board.width * board.tileSize;
        float totalV = board.height * board.tileSize;

        float margin = Mathf.Clamp(edgeMarginWorld, 0f, Mathf.Min(totalU, totalV) * 0.49f);

        // 플레이어 위치
        Vector3 p = targetPlayer ? targetPlayer.position : Vector3.zero;
        float minDistSqr = minDistFromPlayer * minDistFromPlayer;

        // 여러 번 시도
        int tries = Mathf.Max(1, maxTries);
        for (int i = 0; i < tries; i++)
        {
            // 로컬 보드 좌표(u,v) 선택
            float u = Random.Range(margin, totalU - margin);
            float v = Random.Range(margin, totalV - margin);

            // 월드 변환: origin + right*u + forward*v
            Vector3 pos = board.origin + right * u + forward * v;
            pos.y = board.origin.y; // 보드 Y 정렬

            // 플레이어 최소 거리 체크(XZ)
            Vector3 d = pos - new Vector3(p.x, pos.y, p.z);
            d.y = 0f;
            if (d.sqrMagnitude < minDistSqr)
                continue;

            return pos;
        }

        // 폴백: 보드 중앙(로컬 중점)에서 플레이어 반대쪽 가장자리 근처
        float uFB = (p - board.origin).normalized.Dot(right) > 0 ? margin : totalU - margin;
        float vFB = (p - board.origin).normalized.Dot(forward) > 0 ? margin : totalV - margin;
        Vector3 fallback = board.origin + right * uFB + forward * vFB;
        fallback.y = board.origin.y;
        return fallback;
    }
}
static class VecExt {
    public static float Dot(this Vector3 a, Vector3 b) => Vector3.Dot(a, b);
}
