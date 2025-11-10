using UnityEngine;

[DisallowMultipleComponent]
public class OccupancyMinuteJudge : MonoBehaviour
{
    [Header("Refs")]
    public OccupancyRatioGauge occupancy; // 게이지(비율 계산 내장)
    public GameOverUI gameOver;
    [Tooltip("첫 판정 시각(초) — 기본 60s")]
    public float firstCheckAtSeconds = 60f;
    [Tooltip("매 60초마다 반복 판정할지")]
    public bool repeatEveryMinute = true;

    [Header("Rule")]
    [Tooltip("동률인 경우 생존으로 처리")]
    public bool tieCountsAsSurvive = true;

    [Header("Timer Source")]
    [Tooltip("HUD 타이머(SurvivalTimerHUD)의 스케일드 경과 시간을 기준으로 판정")]
    public bool useHudTimer = true;
    public SurvivalTimerHUD timerHUD;   // 인스펙터에 HUD 연결

    [Header("Debug")]
    public bool autoStart = true;

    float nextCheckSecScaled;
    bool  running;

    void Awake()
    {
        if (!occupancy) occupancy = FindAnyObjectByType<OccupancyRatioGauge>();
        if (!gameOver)  gameOver  = FindAnyObjectByType<GameOverUI>();
    }

    void OnEnable()
    {
        if (autoStart) StartJudge();
    }

    void OnDisable()
    {
        running = false;
    }

    float GetElapsedForJudge()
    {
        if (useHudTimer && timerHUD) return timerHUD.ElapsedSeconds();  // HUD 기준
        return Time.time;                                               // 스케일드 시간
    }

    public void StartJudge()
    {
        running = true;

        float t = (useHudTimer && timerHUD) ? timerHUD.ElapsedSeconds() : 0f;
        if (repeatEveryMinute)
        {
            nextCheckSecScaled = (t < firstCheckAtSeconds)
                ? firstCheckAtSeconds
                : Mathf.Ceil((t + 0.0001f) / 60f) * 60f;  // 다음 분 경계
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

        // 프레임 스킵 대비: 60,120,... 여러 경계를 한 번에 넘으면 while로 모두 처리
        while (t >= nextCheckSecScaled)
        {
            Evaluate();

            if (repeatEveryMinute) nextCheckSecScaled += 60f;
            else { running = false; break; }
        }
    }

    void Evaluate()
    {
        if (!occupancy)
        {
            Debug.LogWarning("[OccupancyMinuteJudge] occupancy gauge missing.");
            return;
        }

        // 필요하면 게이지에서 최신 샘플을 강제로 갱신하도록 아래 한 줄을 추가할 수 있음
        // occupancy.ForceSampleAndApply(); // (아래 '선택사항' 패치 참조)

        float p = occupancy.PlayerShown;
        float e = occupancy.EnemyShown;

        bool survive = tieCountsAsSurvive ? (p >= e) : (p > e);
        if (!survive)
        {
            if (gameOver) gameOver.ShowGameOver();
            else Debug.LogWarning("[OccupancyMinuteJudge] Would trigger GameOver.");
        }
    }
}
