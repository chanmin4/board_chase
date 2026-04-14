using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetRegenTimerHUD : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    
    // ★ 추가: 각각의 스포너 참조
    public RocketHazardSystem rocketSpawner;     // 로켓 스폰러
    public BarrageMissileSpawner missileSpawner; // 미사일 스폰러(Interval 모드)

    // ★ 변경: 단일 라벨 → 로켓/미사일 UI 분리
    [Header("Rocket UI")]
    public TMP_Text rocketLabel;
    public Image    rocketFill;

    [Header("Missile UI")]
    public TMP_Text missileLabel;
    public Image    missileFill;

    [Header("Format")]
    public bool showMilliseconds = false;
    public string rocketPrefix = "ROCKET ";
    public string rocketSuffix = "";
    public string missilePrefix = "MISSILE ";
    public string missileSuffix = "";

    [Header("Colors")]
    public Color colorHigh = new Color(0.2f, 1f, 0.4f, 1f);   // >50%
    public Color colorMid  = new Color(1f, 0.9f, 0.2f, 1f);   // 20~50%
    public Color colorLow  = new Color(1f, 0.3f, 0.3f, 1f);   // <20%

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        // ★ 편의: 참조가 비어 있으면 씬에서 찾아보기
        if (!rocketSpawner)  rocketSpawner  = FindAnyObjectByType<RocketHazardSystem>();
        if (!missileSpawner) missileSpawner = FindAnyObjectByType<BarrageMissileSpawner>();
    }

    void Update()
    {
        // ★ 로켓 HUD 갱신
        if (rocketSpawner && rocketSpawner.spawnInterval > 0f)
        {
            float rocketinterval_timer = rocketSpawner.spawnInterval;
            float elapsed = (rocketinterval_timer > 0f && rocketSpawner.lastFireTime >= 0f)
            ? (Time.time - rocketSpawner.lastFireTime): 0f;
            float remain = Mathf.Clamp(rocketinterval_timer - Mathf.Repeat(elapsed, rocketinterval_timer), 0f, rocketinterval_timer);
            float ratio = (rocketinterval_timer> 0f) ? (remain / rocketinterval_timer) : 0f;
            if (rocketLabel)
            {
                rocketLabel.text = rocketPrefix + FormatTime(rocketinterval_timer, showMilliseconds) + rocketSuffix;
                rocketLabel.color = PickColor(ratio);
                rocketLabel.gameObject.SetActive(true);
            }
            if (rocketFill)
            {
                rocketFill.fillAmount = ratio;
                rocketFill.color      = PickColor(ratio);
                rocketFill.gameObject.SetActive(true);
            }
        }
        else
        {
            // ★ 스포너 없거나 비활성일 때 감추기
            if (rocketLabel) rocketLabel.gameObject.SetActive(false);
            if (rocketFill)  rocketFill.gameObject.SetActive(false);
        }

        // ★ 미사일 HUD 갱신 (Interval 모드일 때만 의미 있음)
        if (missileSpawner && missileSpawner.spawnInterval > 0f)
        {
            float missileinterval_timer = missileSpawner.spawnInterval;
            float elapsed = (missileinterval_timer > 0f && rocketSpawner.lastFireTime >= 0f)
            ? (Time.time - rocketSpawner.lastFireTime): 0f;
            float remain = Mathf.Clamp(missileinterval_timer - Mathf.Repeat(elapsed, missileinterval_timer), 0f, missileinterval_timer);
            float ratio = (missileinterval_timer> 0f) ? (remain / missileinterval_timer) : 0f;if (missileLabel)
            {
                missileLabel.text  = missilePrefix + FormatTime(missileinterval_timer, showMilliseconds) + missileSuffix;
                missileLabel.color = PickColor(ratio);
                missileLabel.gameObject.SetActive(true);
            }
            if (missileFill)
            {
                missileFill.fillAmount = ratio;
                missileFill.color      = PickColor(ratio);
                missileFill.gameObject.SetActive(true);
            }
        }
        else
        {
            if (missileLabel) missileLabel.gameObject.SetActive(false);
            if (missileFill)  missileFill.gameObject.SetActive(false);
        }
    }


    string FormatTime(float seconds, bool withMs)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60;
        if (withMs)
            return $"{m:00}:{s:00.0}";
        else
            return $"{m:00}:{(int)s:00}";
    }

    Color PickColor(float remainingRatio)
    {
        // remainingRatio: 1=가득 남음, 0=없음
        if (remainingRatio > 0.5f)  return colorHigh;
        if (remainingRatio > 0.20f) return colorMid;
        return colorLow;
    }
}
