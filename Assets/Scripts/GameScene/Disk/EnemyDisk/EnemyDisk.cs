using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// AI 디스크(적) 발사/계획 + 이동 트레일 칠하기(Enemy 채널).
/// - 보드/마스크 렌더러는 기존 API만 사용 (변경 없음)
/// - 난이도 노브: samples, lookaheadBounces, planInterval, reactionDelay, aimJitterDeg, w_deny, trail/burst 크기
/// - 존/하라스/버스트는 훅만 주석으로 남겨둠 (나중에 확장)
[RequireComponent(typeof(Rigidbody))]
public class EnemyDiskLauncher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] BoardPaintSystem paint;          // Enemy 채널로 트레일 찍음
    [SerializeField] BoardMaskRenderer mask;          // 평가(플레이어/오염) 쿼리용
    [SerializeField] LayerMask wallMask;              // 벽 레이어 (반사 예측/실충돌 반사)
    Rigidbody rb;

    [Header("Trail Paint (Enemy)")]
    [Tooltip("AI가 지나가며 칠할 반경(월드)")]
    public float trailRadiusWorld = 0.6f;             // ★ 난이도에 따라 조정
    [Tooltip("트레일 찍기 활성화")]
    public bool enableTrail = true;

    [Header("Planner – Difficulty Knobs")]
    [Tooltip("후보 각도 수(쉬움 12 / 보통 24 / 어려움 48)")]
    [Range(4, 64)] public int samples = 24;
    [Tooltip("반사 예측 횟수(1/2/3)")]
    [Range(0, 4)] public int lookaheadBounces = 2;
    [Tooltip("의사결정 주기(sec): 0.35 / 0.25 / 0.12")]
    [Range(0.05f, 1.0f)] public float planInterval = 0.25f;
    [Tooltip("발사 입력 반응 지연")]
    [Range(0f, 0.6f)] public float reactionDelay = 0.22f;
    [Tooltip("조준 노이즈(도): 12 / 6 / 2")]
    [Range(0f, 30f)] public float aimJitterDeg = 6f;

    [Header("Utility Weights")]
    [Tooltip("내 색 칠하기 가중치")]
    public float w_paint = 1.0f;
    [Tooltip("상대색 지우기(디나이) 가중치 — 쉬움 0.6 / 보통 1.0 / 어려움 1.3")]
    public float w_deny = 1.0f;
    [Tooltip("리스크 패널티(무한핑퐁, 코너 등)")]
    public float w_risk = 1.0f;

    [Header("Eval / Predict")]
    [Tooltip("경로 표본화 간격(월드 m)")]
    public float evalStepMeters = 0.25f;
    [Tooltip("예측 총 길이 상한(m)")]
    public float maxPredictMeters = 12f;
    [Tooltip("반사 감쇠(예측)")]
    [Range(0.7f, 1.0f)] public float bounceDamping = 0.9f;
    [Header("Player-like Drag (max pull launch)")]

    [Tooltip("참조 가능한 경우, 플레이어의 DragAimController에서 maxPull/launchBoost를 가져옴")]
    public DragAimController playerAimRef;        // 없어도 동작
    [Tooltip("참조 가능한 경우, 플레이어 DiskLauncher에서 powerScale을 가져옴")]
    public DiskLauncher playerLauncherRef;        // 없어도 동작

    // 내부 상태
    Vector3 _trailPrev;
    float _nextPlanAt, _nextFireAt;

    // 속도 레벨(빠름/보통) — 후보 속도 2레벨
    readonly float[] speedLevels = { 1.0f, 0.7f };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!paint) paint = FindAnyObjectByType<BoardPaintSystem>();
        if (!mask) mask = FindAnyObjectByType<BoardMaskRenderer>();
        _trailPrev = transform.position;
    }

    void OnEnable()
    {
        _nextPlanAt = Time.time + 0.2f;
        _nextFireAt = Time.time + 0.2f;
        _trailPrev = transform.position;
    }

    void Update()
    {
        // 지나가며 적(오염) 채널로 트레일 찍기(실제 도장은 PaintSystem이 LateUpdate에서 배치 처리)
        if (enableTrail && paint)
        {
            paint.EnqueueTrail(BoardPaintSystem.PaintChannel.Enemy,
                               _trailPrev, transform.position,
                               trailRadiusWorld,
                               -1f, /*spacing auto*/
                               true /*clearOther: 플레이어 마스크 0으로*/);
            _trailPrev = transform.position;
        }

        // 의사결정
        if (Time.time >= _nextPlanAt)
        {
            _nextPlanAt = Time.time + planInterval;
            TryPlanAndFire();
        }
    }

    // === 실충돌 반사(플레이어 디스크와 동일 체감) ===
    void OnCollisionEnter(Collision c)
    {
        if (((1 << c.collider.gameObject.layer) & wallMask) == 0) return; // 벽만

        var contact = c.GetContact(0);
        rb.position += contact.normal * 0.005f;
        Vector3 v = rb.linearVelocity;
        float into = Vector3.Dot(v, -contact.normal);
        if (into > 0f) rb.linearVelocity = v + contact.normal * into;
        // (SFX 등 필요시 여기에)
    }

    // === 플래너 ===
    void TryPlanAndFire()
    {
        if (Time.time < _nextFireAt) return;

        // 후보 생성: 균일 각도 × 속도 2레벨
        float bestScore = float.NegativeInfinity;
        Vector3 bestDir = Vector3.zero;
        float bestSpd = 0f;
        float baseSpeed =GetPlayerLikeMaxSpeed();

        // 월드 평면(XZ) 기준 각도 분해
        int N = Mathf.Max(4, samples);
        for (int i = 0; i < N; i++)
        {
            float deg = (360f / N) * i;
            Vector3 dir = DirFromDeg(deg);

            foreach (var sp in speedLevels)
            {
                float speed =(baseSpeed * sp) ;
                var score = EvaluateCandidate(dir, speed);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                    bestSpd = speed;
                }
            }
        }

        if (bestScore <= float.NegativeInfinity) return;

        // 발사(지연/노이즈)
        StartCoroutine(FireWithDelay(bestDir, bestSpd, reactionDelay, aimJitterDeg));
        _nextFireAt = Time.time + playerLauncherRef.cooldownSeconds; // 간단 쿨다운
    }

    IEnumerator FireWithDelay(Vector3 dir, float speed, float delay, float jitterDeg)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 조준 노이즈(사람스러움)
        if (jitterDeg > 0f)
        {
            float jitter = Random.Range(-jitterDeg, +jitterDeg);
            dir = Quaternion.Euler(0f, jitter, 0f) * dir;
            dir.Normalize();
        }

        // ★ 이전 속도 제거 후 “쾅” 쏘기 (플레이어 발사 체감과 동일)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.linearVelocity = dir * speed;   // ← 드래그 최대치와 동일한 속도
    
    }

    // === 후보 평가 (Economy: 페인트+디나이 – 리스크) ===
    float EvaluateCandidate(Vector3 dir, float speed)
    {
        // 경로 예측(최대 B회 반사)
        var segments = PredictSegments(transform.position, dir * speed,
                                       lookaheadBounces, maxPredictMeters, wallMask);

        if (segments.Count == 0) return float.NegativeInfinity;

        // 샘플링으로 근사 점수
        float paintGain = 0f;
        float denyGain = 0f;
        float risk = 0f;

        // 동일 노멀 반복(무한 핑퐁) 패널티용
        Vector3 lastN = Vector3.zero;
        int repeatNormal = 0;

        foreach (var sg in segments)
        {
            // 리스크: 같은 노멀 반복 반사
            if (lastN != Vector3.zero)
            {
                float parallel = Mathf.Abs(Vector3.Dot(lastN, sg.normal));
                if (parallel > 0.96f) repeatNormal++;
                else repeatNormal = 0;
            }
            lastN = sg.normal;
            if (repeatNormal >= 1) risk += 1f; // 연속 반복에 패널티 누적

            // 경로 따라 도장 샘플(근사)
            float len = (sg.to - sg.from).magnitude;
            int steps = Mathf.Max(1, Mathf.FloorToInt(len / Mathf.Max(0.05f, evalStepMeters)));
            for (int i = 0; i <= steps; i++)
            {
                float t = (steps == 0) ? 0f : (float)i / steps;
                Vector3 p = Vector3.Lerp(sg.from, sg.to, t);

                // Enemy로 칠할 가치(= 아직 오염X) → paintGain
                // Player를 지울 가치(= 플레이어 칠해짐) → denyGain
                // (둘 다 true면 둘 다 +1: 오염 찍으면 자동으로 플레이어는 0이 되므로 디나이도 성립)
                if (mask && !mask.IsContaminatedWorld(p)) paintGain += 1f;
                if (mask && mask.IsPlayerPaintedWorld(p)) denyGain += 1f;
            }
        }

        // 총합
        float score = w_paint * paintGain + w_deny * denyGain - w_risk * risk;

        // (확장 훅) 존/하라스 보정은 여기서 가중치 더하면 됨
        // score += w_zone * zoneGain + w_harass * harassGain;

        return score;
    }

    struct Seg { public Vector3 from, to, normal; }

    List<Seg> PredictSegments(Vector3 p0, Vector3 v0, int maxB, float maxDist, LayerMask walls)
    {
        var list = new List<Seg>(maxB + 1);
        Vector3 p = p0;
        Vector3 v = v0;
        float left = Mathf.Max(1f, maxDist);

        for (int b = 0; b <= maxB && left > 0.1f; b++)
        {
            if (!Physics.Raycast(p + Vector3.up * 0.01f, v.normalized, out var hit, left, walls, QueryTriggerInteraction.Ignore))
            {
                // 벽 없음: 남은 거리만큼 직진
                Vector3 to = p + v.normalized * left;
                list.Add(new Seg { from = p, to = to, normal = Vector3.zero });
                break;
            }

            float d = hit.distance;
            Vector3 mid = p + v.normalized * Mathf.Max(0f, d);
            list.Add(new Seg { from = p, to = mid, normal = hit.normal });
            left -= d;
            if (left <= 0.05f) break;

            // 반사
            Vector3 r = Vector3.Reflect(v, hit.normal) * bounceDamping;
            p = hit.point + hit.normal * 0.01f;
            v = r;
        }

        return list;
    }

    // XZ 평면 방향 벡터
    static Vector3 DirFromDeg(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    }

    float GetPlayerLikeMaxSpeed()
    {
        float maxPull = playerAimRef.maxPull;
        float launchBst = playerAimRef.launchBoost;
        float pscale = playerLauncherRef.powerScale;

        // DragAimController: strength = pull * launchBoost
        // DiskLauncher.Launch: effPower = strength * powerScale * _extSpeedMul(≈1)
        return maxPull * launchBst * pscale;
    }


    // ===== 난이도 프리셋(원클릭) =====
    public enum Preset { Easy, Normal, Hard }
    public void ApplyPreset(Preset p)
    {
        switch (p)
        {
            case Preset.Easy:
                samples = 12; lookaheadBounces = 1; planInterval = 0.35f;
                reactionDelay = 0.35f; aimJitterDeg = 12f;
                w_deny = 0.6f; trailRadiusWorld = 0.55f;
                break;

            case Preset.Normal:
                samples = 24; lookaheadBounces = 2; planInterval = 0.25f;
                reactionDelay = 0.22f; aimJitterDeg = 6f;
                w_deny = 1.0f; trailRadiusWorld = 0.60f;
                break;

            case Preset.Hard:
                samples = 48; lookaheadBounces = 3; planInterval = 0.12f;
                reactionDelay = 0.10f; aimJitterDeg = 2f;
                w_deny = 1.3f; trailRadiusWorld = 0.65f;
                break;
        }
    }
    
}
