using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// AI 디스크(적) 런처: 플레이어 DiskLauncher와 동일한 발사/쿨타임/외부 모디파이어 구조로 통합.
/// - 이동/반사/트레일/경로평가 로직은 유지
/// - Launch(dir, pull) 사용 (Drag 최대치로 쏘는 효과)
/// - EnemyInkGauge의 스턴 감속/쿨다운 가산 이벤트를 그대로 수신
[RequireComponent(typeof(Rigidbody))]
public class EnemyDiskLauncher : MonoBehaviour
{
    [Header("Refs")]

    [SerializeField] BoardPaintSystem paint;          // Enemy 채널 트레일
    [SerializeField] BoardMaskRenderer mask;          // 평가(플레이어/오염) 쿼리
    [SerializeField] LayerMask wallMask;              // 벽 레이어 (실충돌/예측)
    Rigidbody rb;
    //[SerializeField]EnemyInkGauge enemyinkgauge;

    // ─────────────────────────────────────
    // DiskLauncher와 동일한 런처 코어
    // ─────────────────────────────────────
    [Header("Launch / Stop")]
    public float powerScale = 1f;

    [Header("Cooldown Mode")]
    public bool useCooldown = true;
    public float cooldownSeconds = 2f;
    public float CooldownRemain { get; private set; } = 0f;
    public bool CanLaunchNow => useCooldown ? (CooldownRemain <= 0f) : true;
    public event System.Action<float, float> OnCooldownChanged; // (remain, duration)

    [Header("Cooldown Bonus On Wall Bounce (optional)")]
    public bool cooldownBonusOnBounce = false;
    [Min(0f)] public float cooldownReducePerBounce = 0.5f;

    [Header("External Modifiers (Events from Gauge etc.)")]
    public UnityEvent<float> externalSpeedMul;     // 1=기본, 0.6=감속 등
    public UnityEvent<float> externalCooldownAdd;  // 초 가산
    float _extSpeedMul = 1f;
    float _extCooldownAdd = 0f;

    // ─────────────────────────────────────
    // Player-like Drag (최대 드래그로 쏘는 체감)
    // ─────────────────────────────────────
    [Header("Player-like Drag (max pull launch)")]
    public DragAimController playerAimRef;   // maxPull / launchBoost 참조
    public DiskLauncher playerLauncherRef; // powerScale, cooldownSeconds 참조(선택)
    [Tooltip("플레이어 레퍼런스가 없을 때 기본값")]
    public float fallbackPlayerMaxPull = 6f;
    public float fallbackPlayerLaunchBoost = 6f;

    // ─────────────────────────────────────
    // Trail Paint (Enemy)
    // ─────────────────────────────────────
    [Header("Trail Paint (Enemy)")]
    public bool enableTrail = true;
    public float trailRadiusWorld = 0.6f;

    // ─────────────────────────────────────
    // Planner – Difficulty Knobs (평가/의사결정)
    // ─────────────────────────────────────
    [Header("Planner – Difficulty Knobs")]
    [Range(4, 64)] public int samples = 24;   // 각도 수
    [Range(0, 4)] public int lookaheadBounces = 2;    // 반사 예측
    [Range(0.05f, 1f)] public float planInterval = 0.25f;// 결정 주기
    [Range(0f, 0.6f)] public float reactionDelay = 0.22f;// 반응 지연
    [Range(0f, 30f)] public float aimJitterDeg = 6f;   // 조준 노이즈

    [Header("Utility Weights")]
    public float w_paint = 1.0f;
    public float w_deny = 1.0f;
    public float w_risk = 1.0f;

    [Header("Eval / Predict")]
    public float evalStepMeters = 0.25f;
    public float maxPredictMeters = 12f;
    [Range(0.7f, 1.0f)] public float bounceDamping = 0.9f;
    [Header("Bonus Arc Hook")]
    public DiskBonusArc bonusArc;
    //public LayerMask otherDiskMask;
    //public float bonusArcInkPenalty = 50f; // 인스펙터에서 조절

    // 내부 상태
    Vector3 _trailPrev;
    float _nextPlanAt;
    readonly float[] speedLevels = { 1.0f, 0.7f }; // 빠름/보통

    // ─────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!paint) paint = FindAnyObjectByType<BoardPaintSystem>();
        if (!mask) mask = FindAnyObjectByType<BoardMaskRenderer>();
        _trailPrev = transform.position;

        // 외부 모디파이어 이벤트 → 내부 값으로 반영
        externalSpeedMul ??= new UnityEvent<float>();
        externalCooldownAdd ??= new UnityEvent<float>();
        externalSpeedMul.AddListener(SetExternalSpeedMul);
        externalCooldownAdd.AddListener(SetExternalCooldownAdd);
    }

    void OnEnable()
    {
        _nextPlanAt = Time.time + 0.2f;
        _trailPrev = transform.position;
        NotifyCooldown(); // HUD 등 초기화
    }

    void Update()
    {
        // 지나가며 적(오염) 채널로 트레일 찍기
        if (enableTrail && paint)
        {
            // 1) 현재 위치 즉시 스탬프(시각 즉시 반영)
            paint.HeadStampNow(BoardPaintSystem.PaintChannel.Enemy,
                            transform.position, trailRadiusWorld, true);
            // 2) 거리 기반 트레일(빈틈 보강)
            /*
            paint.EnqueueTrail(BoardPaintSystem.PaintChannel.Enemy,
                            _trailPrev, transform.position,
                            trailRadiusWorld, -1f, true);
            _trailPrev = transform.position;
            */
        }


        // 쿨타임 틱
        if (useCooldown && CooldownRemain > 0f)
        {
            CooldownRemain = Mathf.Max(0f, CooldownRemain - Time.deltaTime);
            NotifyCooldown();
        }

        // 의사결정
        if (Time.time >= _nextPlanAt)
        {
            _nextPlanAt = Time.time + planInterval;
            TryPlanAndFire();
        }
    }

    // ─────────────────────────────────────
    // 충돌 반사 + (옵션) 튕길 때 쿨타임 보너스
    // ─────────────────────────────────────
    void OnCollisionEnter(Collision c)
    {
        //디스크끼리충돌
        if (c.rigidbody && c.rigidbody.TryGetComponent<DiskLauncher>(out _))
        {
            var pt = c.GetContact(0).point;
            bonusArc?.OnCollisionWithOtherDisk(c.rigidbody.transform, pt);
            return;
        }



        if (((1 << c.collider.gameObject.layer) & wallMask) == 0) return; // 벽만

        var contact = c.GetContact(0);
        rb.position += contact.normal * 0.005f;

        Vector3 v = rb.linearVelocity;
        float into = Vector3.Dot(v, -contact.normal);
        if (into > 0f) rb.linearVelocity = v + contact.normal * into;

        if (useCooldown && cooldownBonusOnBounce && CooldownRemain > 0f && cooldownReducePerBounce > 0f)
            ReduceCooldown(cooldownReducePerBounce);
    }

    // ─────────────────────────────────────
    // Launch (DiskLauncher와 동일 시그니처/동작)
    // ─────────────────────────────────────
    public void Launch(Vector3 dir, float pull)
    {
        if (useCooldown)
        {
            if (CooldownRemain > 0f) return;
        }

        float effPower = pull * powerScale * Mathf.Clamp(_extSpeedMul, 0.1f, 1f);

        // 이전 속도 제거 후 “쾅” 쏘기
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = dir.normalized * effPower;

        if (useCooldown) StartCooldown();
    }

    void StartCooldown()
    {
        float effCooldown = Mathf.Max(0.0001f, cooldownSeconds + Mathf.Max(0f, _extCooldownAdd));
        CooldownRemain = effCooldown;
        NotifyCooldown();
    }

    void ReduceCooldown(float seconds)
    {
        if (!useCooldown || seconds <= 0f) return;
        CooldownRemain = Mathf.Max(0f, CooldownRemain - seconds);
        NotifyCooldown();
    }

    void CancelAddCooldown(float seconds)
    {
        if (!useCooldown || seconds <= 0f) return;
        CooldownRemain = Mathf.Max(0f, CooldownRemain) + seconds;
        NotifyCooldown();
    }

    void NotifyCooldown()
    {
        float dur = Mathf.Max(0.0001f, cooldownSeconds + Mathf.Max(0f, _extCooldownAdd));
        OnCooldownChanged?.Invoke(CooldownRemain, dur);
    }

    public void SetExternalSpeedMul(float v) => _extSpeedMul = Mathf.Clamp(v, 0.1f, 1f);
    public void SetExternalCooldownAdd(float sec) => _extCooldownAdd = Mathf.Max(0f, sec);

    // ─────────────────────────────────────
    // Planner (기존 평가/후보 생성 유지, 발사만 Launch 사용)
    // ─────────────────────────────────────
    void TryPlanAndFire()
    {
        if (!CanLaunchNow) return;

        // 플레이어 드래그 최대치 기준 속도/풀값 계산
        float maxPull = playerAimRef ? playerAimRef.maxPull : fallbackPlayerMaxPull;
        float boost = playerAimRef ? playerAimRef.launchBoost : fallbackPlayerLaunchBoost;
        float basePull = maxPull * boost; // Launch의 pull 인자

        float bestScore = float.NegativeInfinity;
        Vector3 bestDir = Vector3.zero;
        float bestSpd = 0f;

        // 월드 XZ 평면 균일 스캔
        int N = Mathf.Max(4, samples);
        for (int i = 0; i < N; i++)
        {
            float deg = (360f / N) * i;
            Vector3 dir = DirFromDeg(deg);

            foreach (var sp in speedLevels) // {1.0, 0.7}
            {
                // 평가 전용 "예상 속도": pull*powerScale (외부감속은 계획 단계에선 제외)
                float predictSpeed = basePull * powerScale * sp;
                float score = EvaluateCandidate(dir, predictSpeed);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                    bestSpd = sp; // 선택한 속도 레벨
                }
            }
        }

        if (bestScore <= float.NegativeInfinity) return;

        // 실제 발사: Launch(dir, pull) 사용 (외부 감속/쿨다운 적용됨)
        StartCoroutine(FireWithDelay(bestDir, basePull * bestSpd, reactionDelay, aimJitterDeg));
    }

    IEnumerator FireWithDelay(Vector3 dir, float pull, float delay, float jitterDeg)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (jitterDeg > 0f)
        {
            float jitter = Random.Range(-jitterDeg, +jitterDeg);
            dir = Quaternion.Euler(0f, jitter, 0f) * dir;
            dir.Normalize();
        }

        Launch(dir, pull);
    }

    // ─────────────────────────────────────
    // 후보 평가 (Economy: 페인트+디나이 – 리스크)
    // ─────────────────────────────────────
    float EvaluateCandidate(Vector3 dir, float speed)
    {
        var segs = PredictSegments(transform.position, dir.normalized * speed,
                                   lookaheadBounces, maxPredictMeters, wallMask);
        if (segs.Count == 0) return float.NegativeInfinity;

        float paintGain = 0f, denyGain = 0f, risk = 0f;
        Vector3 lastN = Vector3.zero; int repeatNormal = 0;

        foreach (var sg in segs)
        {
            if (lastN != Vector3.zero)
            {
                float parallel = Mathf.Abs(Vector3.Dot(lastN, sg.normal));
                repeatNormal = (parallel > 0.96f) ? (repeatNormal + 1) : 0;
            }
            lastN = sg.normal;
            if (repeatNormal >= 1) risk += 1f;

            float len = (sg.to - sg.from).magnitude;
            int steps = Mathf.Max(1, Mathf.FloorToInt(len / Mathf.Max(0.05f, evalStepMeters)));
            for (int i = 0; i <= steps; i++)
            {
                float t = (steps == 0) ? 0f : (float)i / steps;
                Vector3 p = Vector3.Lerp(sg.from, sg.to, t);
                if (mask && !mask.IsEnemyPaintedWorld(p)) paintGain += 1f;   // 오염 칠하기
                if (mask && mask.IsPlayerPaintedWorld(p)) denyGain += 1f;  // 플레이어 지우기
            }
        }

        return w_paint * paintGain + w_deny * denyGain - w_risk * risk;
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
                Vector3 to = p + v.normalized * left;
                list.Add(new Seg { from = p, to = to, normal = Vector3.zero });
                break;
            }

            float d = hit.distance;
            Vector3 mid = p + v.normalized * Mathf.Max(0f, d);
            list.Add(new Seg { from = p, to = mid, normal = hit.normal });
            left -= d;
            if (left <= 0.05f) break;

            Vector3 r = Vector3.Reflect(v, hit.normal) * bounceDamping;
            p = hit.point + hit.normal * 0.01f;
            v = r;
        }
        return list;
    }

    static Vector3 DirFromDeg(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    }

    // 난이도 프리셋 (선택)
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
    /*
    public void ApplyBonusArcPenalty(Transform other)
    {
        // “내” 게이지를 깎는다(맞은 쪽이 손해).
        Debug.Log("enemygaugepanelty");
        enemyinkgauge?.Add(-bonusArcInkPenalty);    // 적 쪽
    }
*/

}
