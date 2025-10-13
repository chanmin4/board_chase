using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;              // Legacy UI
using TMPro;

public class ContamCoverageMeterUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("오염 마스크를 그리는 컴포넌트(예: BoardMaskRenderer / PaintMaskRenderer 등)")]
    public MonoBehaviour maskRenderer;
    [Tooltip("maskRenderer에서 텍스처를 가져오지 못할 때 수동 지정")]
    public Texture2D maskTexOverride;

    [Header("Compute")]
    [Tooltip("알파(0..255)를 가중치로 합산 (ON 권장). OFF면 0/1 이진 카운트")]
    public bool weightedAlpha = true;
    [Tooltip("성능용 샘플 간격 (1=모든 픽셀)")]
    [Range(1, 16)] public int sampleStride = 1;
    [Tooltip("갱신 주기(초). 0이면 매 프레임")]
    [Range(0f, 1f)] public float updateInterval = 0.2f;

    [Header("UI Output (하나만 연결해도 됨)")]
    public TextMeshProUGUI tmpText; // TextMeshPro

    [Header("Events")]
    public UnityEvent<float> OnContamRatioChanged; // 0~1

    Texture2D _tex;
    float _timer;
    float _lastRatio = -1f;

    void Awake()
    {
        _tex = ResolveTexture();
    }

    void Update()
    {
        if (updateInterval > 0f)
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
        }

        if (_tex == null) _tex = ResolveTexture();
        if (_tex == null) return;

        // 텍스처에서 픽셀 읽기 (읽기 가능 Read/Write 텍스처여야 함)
        Color32[] pix;
        try { pix = _tex.GetPixels32(); }
        catch
        {
            Debug.LogWarning("[ContamCoverage] Mask texture not readable.");
            return;
        }

        float ratio = ComputeRatio(pix, _tex.width, _tex.height, weightedAlpha, sampleStride);

        if (!Mathf.Approximately(ratio, _lastRatio))
        {
            _lastRatio = ratio;
            OnContamRatioChanged?.Invoke(ratio);
            WriteToUI(ratio);
        }
    }

    void WriteToUI(float r)
    {
        string s = $"Contam: {(r * 100f):0.0}%";
        if (tmpText) tmpText.text = s;
    }

    Texture2D ResolveTexture()
{
    if (maskTexOverride) return maskTexOverride;
    if (!maskRenderer) return null;

    var t = maskRenderer.GetType();
    var flags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public   |
                System.Reflection.BindingFlags.NonPublic;

    // 1) 프로퍼티 먼저 (권장 이름들 시도)
    var p = t.GetProperty("ContamMaskTex", flags)
          ?? t.GetProperty("MaskTex", flags)
          ?? t.GetProperty("maskTex", flags);
    if (p != null && typeof(Texture2D).IsAssignableFrom(p.PropertyType))
    {
        var tex = (Texture2D)p.GetValue(maskRenderer);
        if (tex) return tex;
    }

    // 2) 필드 (private _mask도 잡힘)
    var f = t.GetField("_mask", flags)
          ?? t.GetField("maskTex", flags)
          ?? t.GetField("contamMaskTex", flags);
    if (f != null && typeof(Texture2D).IsAssignableFrom(f.FieldType))
    {
        var tex = (Texture2D)f.GetValue(maskRenderer);
        if (tex) return tex;
    }

    Debug.LogWarning($"[ContamCoverage] No mask texture found on {t.Name}. " +
                     $"Expose a getter (e.g., public Texture2D ContamMaskTex => _mask) or assign MaskTexOverride.");
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
