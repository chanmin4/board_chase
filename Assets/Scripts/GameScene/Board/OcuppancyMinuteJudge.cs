using UnityEngine;

[DisallowMultipleComponent]
public class OccupancyMinuteJudge : MonoBehaviour
{
    [Header("Refs")]
    public OccupacncyRatio occupancy; // 플레이어/적 비율 이벤트 제공자
    public GameOverUI gameOver;       // 실패 시 호출

    [Header("Timing")]
    [Tooltip("슬로모션과 무관하게 실시간으로 판정하려면 ON")]
    public bool useUnscaledTime = true;
    [Tooltip("첫 판정 시각(초) — 기본 60s")]
    public float firstCheckAtSeconds = 60f;
    [Tooltip("매 60초마다 반복 판정할지")]
    public bool repeatEveryMinute = true;

    [Header("Rule")]
    [Tooltip("동률인 경우 생존으로 처리")]
    public bool tieCountsAsSurvive = true;

    [Header("Pause Handling")]
    [Tooltip("timeScale≈0(일시정지)인 동안은 판정용 시간에서 제외")]
    public bool ignoreWhilePaused = true;
    [Range(0f, 0.01f)] public float pauseEpsilon = 0.0001f;
    [Header("Sync with HUD")]
    [Tooltip("HUD 타이머(SurvivalTimerHUD)의 스케일드 경과 시간을 기준으로 판정")]
    public bool useHudTimer = true;
    public SurvivalTimerHUD timerHUD;   // 인스펙터에 HUD 연결
    float nextCheckSecScaled;



    [Header("Debug")]
    public bool autoStart = true;

    // [추가] 일시정지 누적 시간(언스케일드로 적산)
    float pausedAccum = 0f;
    float playerRatio, enemyRatio;
    float nextCheckTime;
    bool  running;

    float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

    void Awake()
    {
        if (!occupancy) occupancy = FindAnyObjectByType<OccupacncyRatio>();
        if (!gameOver)  gameOver  = FindAnyObjectByType<GameOverUI>();
    }

    void OnEnable()
    {
        if (occupancy)
        {
            occupancy.OnPlayerRatioChanged.AddListener(OnPlayerRatio);
            occupancy.OnEnemyRatioChanged.AddListener(OnEnemyRatio);
        }
        if (autoStart) StartJudge();
    }

    void OnDisable()
    {
        if (occupancy)
        {
            occupancy.OnPlayerRatioChanged.RemoveListener(OnPlayerRatio);
            occupancy.OnEnemyRatioChanged.RemoveListener(OnEnemyRatio);
        }
        running = false;
    }
    float GetElapsedForJudge()
    {
        if (useHudTimer && timerHUD) return timerHUD.ElapsedSeconds();  // ★ HUD 기준
                                                                        // HUD 없으면 기존 로직(언스케일드+일시정지 보정) 사용
        return NowAdj;
    }


    float NowAdj
{
    get
    {
        if (useUnscaledTime)
        {
            if (ignoreWhilePaused && Time.timeScale <= pauseEpsilon)
                pausedAccum += Time.unscaledDeltaTime;   // 일시정지 시간 적산
            return Time.unscaledTime - pausedAccum;       // 보정된 시간
        }
        return Time.time;
    }
}

    public void StartJudge()
    {
        running = true;
        pausedAccum = 0f;

        float t = (useHudTimer && timerHUD) ? timerHUD.ElapsedSeconds() : 0f;

        if (repeatEveryMinute)
        {
            // 이미 시간이 흘렀다면, 바로 '다음 분 경계'로 맞춤
            nextCheckSecScaled = (t < firstCheckAtSeconds)
                ? firstCheckAtSeconds
                : Mathf.Ceil((t + 0.0001f) / 60f) * 60f;
        }
        else
        {
            nextCheckSecScaled = firstCheckAtSeconds;
        }
    }


    void Update()
    {
        if (!running) return;

        float t = GetElapsedForJudge();

        // ★ 프레임 스킵 보정: 60, 120, 180… 여러 개를 한 번에 넘겼으면 while로 모두 처리
        while (t >= nextCheckSecScaled)
        {
            Evaluate();

            if (repeatEveryMinute) nextCheckSecScaled += 60f;
            else { running = false; break; }
        }
    }

    void Evaluate()
    {
        bool survive = tieCountsAsSurvive ? (playerRatio >= enemyRatio) : (playerRatio > enemyRatio);

        if (!survive)
        {
            if (gameOver) gameOver.ShowGameOver();
            else Debug.LogWarning("[OccupancyMinuteJudge] GameOverUI missing; would trigger GameOver.");
        }

        Debug.Log($"[OccupancyMinuteJudge] t={Now:0.00}s player={playerRatio:P1} enemy={enemyRatio:P1} => {(survive ? "SURVIVE" : "FAIL")}");
    }

    void OnPlayerRatio(float r) => playerRatio = r;
    void OnEnemyRatio(float r)  => enemyRatio  = r;
}
