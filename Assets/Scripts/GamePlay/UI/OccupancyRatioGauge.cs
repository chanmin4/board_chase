using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Masks;
using System;
using UnityEngine.Events;
[DisallowMultipleComponent]
public class OccupancyRatioGauge : MonoBehaviour
{
    // ===== Source (텍스처 직접 읽기) =====
    [Header("Source")]
    [Tooltip("IMaskProvider 구현 컴포넌트")]
    public BoardMaskRenderer maskRenderer; // IMaskProvider
    [Tooltip("필요 시 수동 오버라이드")]
    [NonSerialized] public Texture2D enemyMaskOverride;
    [NonSerialized] public Texture2D playerMaskOverride;

    [Header("Compute (same as OccupacncyRatio)")]
    [Tooltip("알파(0..255) 가중 합산 / OFF면 이진")]
    public bool weightedAlpha = true;
    [Tooltip("성능용 샘플 간격 (1=모든 픽셀)")]
    [Range(1, 16)] public int sampleStride = 1;
    [Tooltip("갱신 주기(초). 0이면 매 프레임")]
    [Range(0f, 1f)] public float updateInterval = 0.2f;

    // ===== UI =====
    [Header("UI")]
    [Tooltip("Filled/Horizontal, Origin=Left")]
    public Image fillPlayer;
    [Tooltip("Filled/Horizontal, Origin=Right")]
    public Image fillEnemy;

    [Header("Optional Labels")]
    public TextMeshProUGUI labelPlayer;
    public TextMeshProUGUI labelEnemy;

    [Header("Display")]
    [Tooltip("항상 100%로 정규화(줄다리기) vs 절대점유율")]
    public bool normalizeTo100 = true;
    [Tooltip("게이지 부드럽게 따라오기")]
    [Range(0.5f, 20f)] public float smoothSpeed = 8f;
    [Tooltip("일시정지에서도 매끈 이동하려면 ON")]
    public bool useUnscaledTime = true;

    // ===== runtime =====
    Texture2D _enemyTex, _playerTex;
    float _timer;
    float lastAbsP, lastAbsE; // 0..1 (보드 전체 대비 절대점유)
    float targetP, targetE;   // 표시 목표값(정규화 반영)
    float curP, curE;         // 현재 표시값

    // 외부에서 못쓰더라도 디버그/연동용 getter 제공
    public float PlayerAbs => lastAbsP;
    public float EnemyAbs  => lastAbsE;
    public float PlayerShown => normalizeTo100 ? targetP : lastAbsP;
    public float EnemyShown  => normalizeTo100 ? targetE : lastAbsE;

    void OnEnable()
    {
        _enemyTex  = ResolveTexture(false);
        _playerTex = ResolveTexture(true);
        ForceSample();
        InstantApply();
    }

    void Update()
    {
        // 1) 비율 샘플링 (간격 기반)
        if (updateInterval > 0f)
        {
            _timer += Time.deltaTime; // painter 쪽과 동일하게 '스케일드'로
            if (_timer >= updateInterval) { _timer = 0f; SampleNow(); }
        }
        else
        {
            SampleNow();
        }

        // 2) 게이지 보간
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        curP = Mathf.MoveTowards(curP, targetP, smoothSpeed * dt);
        curE = Mathf.MoveTowards(curE, targetE, smoothSpeed * dt);
        if (fillPlayer) fillPlayer.fillAmount = curP;
        if (fillEnemy)  fillEnemy.fillAmount  = curE;
    }

    // ---- sampling / 계산 ----
    void SampleNow()
    {
        if (_enemyTex  == null) _enemyTex  = ResolveTexture(false);
        if (_playerTex == null) _playerTex = ResolveTexture(true);

        lastAbsE = (_enemyTex  != null) ? ComputeFromTex(_enemyTex)  : 0f;
        lastAbsP = (_playerTex != null) ? ComputeFromTex(_playerTex) : 0f;
        RecomputeTargetsAndLabels();
    }

    void ForceSample() { _timer = 0f; SampleNow(); }

    float ComputeFromTex(Texture2D t)
    {
        var pix = SafeGetPixels(t);
        if (pix == null) return 0f;
        return ComputeRatio(pix, t.width, t.height, weightedAlpha, sampleStride);
    }

    void RecomputeTargetsAndLabels()
    {
        float sum = lastAbsP + lastAbsE;
        if (normalizeTo100)
        {
            if (sum > 1e-5f) { targetP = lastAbsP / sum; targetE = lastAbsE / sum; }
            else             { targetP = 0f;             targetE = 0f;            }
        }
        else
        {
            targetP = lastAbsP;
            targetE = lastAbsE;
        }

        if (labelPlayer) labelPlayer.text = $"Player\n{Mathf.RoundToInt((normalizeTo100?targetP:lastAbsP)*100f)}%";
        if (labelEnemy)  labelEnemy.text  = $"Enemy\n{Mathf.RoundToInt((normalizeTo100?targetE:lastAbsE)*100f)}%";
    }

    void InstantApply()
    {
        curP = targetP; curE = targetE;
        if (fillPlayer) fillPlayer.fillAmount = curP;
        if (fillEnemy)  fillEnemy.fillAmount  = curE;
    }

    static Color32[] SafeGetPixels(Texture2D t) { try { return t.GetPixels32(); } catch { return null; } }

    // === 여기부터가 OccupacncyRatio의 핵심 "점유율 계산"만 이식 ===
    Texture2D ResolveTexture(bool isPlayer)
    {
        if (isPlayer) { if (playerMaskOverride != null) return playerMaskOverride; }
        else          { if (enemyMaskOverride  != null) return enemyMaskOverride;  }

        if (!maskRenderer) return null;
        if (maskRenderer is IMaskProvider prov) // BoardMaskRenderer가 제공
            return isPlayer ? prov.PlayerMaskTex : prov.EnemyMaskTex;

        Debug.LogWarning("[OccupancyRatioGauge] maskRenderer가 IMaskProvider를 구현하지 않았습니다.");
        return null;
    }

    static float ComputeRatio(Color32[] pix, int w, int h, bool weighted, int stride)
    {
        if (pix == null || pix.Length == 0) return 0f;
        stride = Mathf.Max(1, stride);

        long sum = 0, denom;

        if (weighted)
        {
            long samples = 0;
            for (int y = 0; y < h; y += stride)
            {
                int row = y * w;
                for (int x = 0; x < w; x += stride)
                {
                    sum += pix[row + x].a; // 알파 가중치
                    samples++;
                }
            }
            denom = 255L * samples;
        }
        else
        {
            long samples = 0;
            for (int y = 0; y < h; y += stride)
            {
                int row = y * w;
                for (int x = 0; x < w; x += stride)
                {
                    sum += (pix[row + x].a > 0) ? 1 : 0;
                    samples++;
                }
            }
            denom = samples;
        }

        if (denom <= 0) return 0f;
        return Mathf.Clamp01((float)sum / (float)denom);
    }
}
