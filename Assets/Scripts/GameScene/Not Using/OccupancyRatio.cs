using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;              // Legacy UI
using TMPro;
using System;
using Game.Masks;
public class OccupacncyRatio : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("오염 마스크를 그리는 컴포넌트(예: BoardMaskRenderer / PaintMaskRenderer 등)")]
    public MonoBehaviour maskRenderer;
    [Tooltip("maskRenderer에서 텍스처를 가져오지 못할 때 수동 지정")]
    [NonSerialized]public Texture2D EnemyMaskOverride;
    [NonSerialized]public Texture2D playerMaskOverride;

    [Header("Compute")]
    [Tooltip("알파(0..255)를 가중치로 합산 (ON 권장). OFF면 0/1 이진 카운트")]
    public bool weightedAlpha = true;
    [Tooltip("성능용 샘플 간격 (1=모든 픽셀)")]
    [Range(1, 16)] public int sampleStride = 1;
    [Tooltip("갱신 주기(초). 0이면 매 프레임")]
    [Range(0f, 1f)] public float updateInterval = 0.2f;

    [Header("UI Output (하나만 연결해도 됨)")]
    public TextMeshProUGUI tmpTextEnemy; // TextMeshPro
    public TextMeshProUGUI tmpTextPlayer;  

    [Header("Events")]
    public UnityEvent<float> OnEnemyRatioChanged; // 0~1
    public UnityEvent<float> OnPlayerRatioChanged; 
    Texture2D _Enemytex;
    Texture2D _playerTex; 
    float _timer;
    float _lastEnemyRatio = -1f;
    float _lastPlayerRatio = -1f;

    void Awake()
    {
        _Enemytex = ResolveTexture(false);
         _playerTex = ResolveTexture(true);
    }

    void Update()
    {
        if (updateInterval > 0f)
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
        }

        // ─ 오염(적) ─
        if (_Enemytex == null) _Enemytex = ResolveTexture(false);
        if (_Enemytex)
        {
            Color32[] pix;
            try { pix = _Enemytex.GetPixels32(); }
            catch { pix = null; }
            if (pix != null)
            {
                float ratio = ComputeRatio(pix, _Enemytex.width, _Enemytex.height, weightedAlpha, sampleStride);
                if (!Mathf.Approximately(ratio, _lastEnemyRatio))
                {
                    _lastEnemyRatio = ratio;
                    OnEnemyRatioChanged?.Invoke(ratio);
                    WriteToUI_Enemy(ratio);
                }
            }
        }

        // ─ 플레이어 ─
            if (_playerTex == null) _playerTex = ResolveTexture(true);
            if (_playerTex)
            {
                Color32[] pixP;
                try { pixP = _playerTex.GetPixels32(); }
                catch { pixP = null; }
                if (pixP != null)
                {
                    float ratioP = ComputeRatio(pixP, _playerTex.width, _playerTex.height, weightedAlpha, sampleStride);
                    if (!Mathf.Approximately(ratioP, _lastPlayerRatio))
                    {
                        _lastPlayerRatio = ratioP;
                        OnPlayerRatioChanged?.Invoke(ratioP);
                        WriteToUI_Player(ratioP);
                    }
                }
            }
    }


    void WriteToUI_Enemy(float r)
    {
        string s = $"Contam: {(r * 100f):0.0}%";
        if (tmpTextEnemy) tmpTextEnemy.text = s;
    }


    void WriteToUI_Player(float r) // ★ ADD
    {
        string s = $"Player: {(r * 100f):0.0}%";
        if (tmpTextPlayer) tmpTextPlayer.text = s;
    }


    Texture2D ResolveTexture(bool isPlayer)
{
    // 인스펙터 override가 들어오면 그걸 최우선 사용
    if (isPlayer)  { if (playerMaskOverride) return playerMaskOverride; }
    else           { if (EnemyMaskOverride)  return EnemyMaskOverride;  }

    if (!maskRenderer) return null;

    // ★ 확정 경로: IMaskProvider만 신뢰
    if (maskRenderer is IMaskProvider prov)
        return isPlayer ? prov.PlayerMaskTex : prov.EnemyMaskTex;

    Debug.LogWarning("[ContamCoverage] maskRenderer가 IMaskProvider를 구현하지 않았습니다.");
    return null;
}

    static float ComputeRatio(Color32[] pix, int w, int h, bool weighted, int stride)
    {
        if (pix == null || pix.Length == 0) return 0f;
        stride = Mathf.Max(1, stride);

        long sum = 0;
        long denom;

        if (weighted)
        {
            long samples = 0;
            for (int y = 0; y < h; y += stride)
            {
                int row = y * w;
                for (int x = 0; x < w; x += stride)
                {
                    sum += pix[row + x].a; // 0..255
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
