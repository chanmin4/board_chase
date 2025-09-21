using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ZoneMiniTimerFollower : MonoBehaviour
{
    [Header("Refs")]
    [NonSerialized]public Canvas canvas;      //null이면 런타임에 World Space Canvas를 생성
    [NonSerialized]public Camera cam;          // null이면 Camera.main
    public RectTransform uiRoot;          // 이 오브젝트의 RectTransform
    public Image bgImage;                 // 선택: 배경 링(회색 등)
    public Image fillImage;               // 진행 링(초록→빨강 등)
    [Range(0.3f, 0.9f)] public float innerHoleRatio = 0.60f; // 도넛 구멍 비율
    [Header("Placement")]
    [Range(0f, 360f)] public float angleDeg = 45f;   // 존의 우상단: 45°
    public float pixelMargin = 30f;                 // 원 바깥 여백(px)

    // 기존 [Header("Placement")] 아래에 붙이기
    [Tooltip("존 중심→타이머까지의 거리에서 '반지름'에 곱해지는 배수. 1=원둘레 위, 1.1=조금 바깥, 0.9=조금 안쪽")]
    [Range(0f, 2f)] public float radialDistanceMul = 1.0f;

    [Tooltip("위 배수로 정해진 거리에서 추가로 더/덜 띄우는 픽셀 값(+바깥, -안쪽)")]
    public float radialExtraPixels = 0f;

    Sprite _fallbackWhite;                // 스프라이트 없을 때 런타임 생성
    RectTransform _fillHoleRT;            // 진행 링 안쪽 구멍
    RectTransform _bgHoleRT;              // 배경 링 안쪽 구멍
    RectTransform canvasRT;
    Canvas _cachedCanvas;
    // ---- 내부 상태 ----
    Vector3 worldCenter;
    float worldRadius;
    float ttlInit = 1f;
    float remain = 1f;

    public float TtlInit => ttlInit; // ZoneVisualManager가 읽기용으로 사용
    void CacheCanvasRefs()
    {
        if (canvas)
        {
            canvasRT = canvas.transform as RectTransform;
            _cachedCanvas = canvas;
            return;
        }

        var any = FindFirstObjectByType<Canvas>();   // 씬의 HUD Canvas
        if (!any) return;                            // ← 못 찾으면 그냥 대기(Setup에서 주입됨)

        canvas = any;
        canvasRT = canvas.transform as RectTransform;
        _cachedCanvas = canvas;
    } 
public void Setup(Canvas screenCanvas, Vector3 centerW, float radiusW, float ttlSeconds)
{
    canvas = screenCanvas;               // ★ 외부 주입만 사용
    uiRoot  = uiRoot  ? uiRoot  : GetComponent<RectTransform>();
    fillImage = fillImage ? fillImage : GetComponentInChildren<Image>(true);

    if (fillImage)
    {
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top; // 12시 시작
        fillImage.fillClockwise = true;                  // 시계방향 감소
    }

    worldCenter = centerW;
    worldRadius = Mathf.Max(0.001f, radiusW);
    ttlInit = Mathf.Max(0.001f, ttlSeconds);
    remain  = ttlInit;

    EnsureSprites();
    EnsureDonutHoles();
}

    public void SetRemain(float remainSeconds)
    {
        remain = Mathf.Clamp(remainSeconds, 0f, ttlInit);
        if (fillImage)
        {
            // 남은비율(1→0)에 맞춰 도넛 채우기 감소
            float p01 = Mathf.Clamp01(remain / ttlInit);
            fillImage.fillAmount = p01;
        }
    }

   void LateUpdate()
{
    if (!uiRoot) uiRoot = (RectTransform)transform;
    if (!cam)    cam    = Camera.main;

    // 캔버스 캐시가 없거나 바뀌었으면 다시 잡기
    if (!canvasRT || canvas != _cachedCanvas) CacheCanvasRefs();
    if (!canvasRT || !cam) return;

    // 캔버스 타입별 카메라 파라미터 (Overlay는 null)
    Camera canvasCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

    // 1) 존 중심 → HUD 캔버스 로컬 좌표
    Vector2 c;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRT,
        cam.WorldToScreenPoint(worldCenter),   // ← 존 월드 중심
        canvasCam,
        out c
    );

    // 2) 반지름을 '픽셀'로 추정 : 카메라 right 방향으로 worldRadius 만큼 떨어진 지점
    Vector3 edgeW = worldCenter + cam.transform.right * worldRadius;
    Vector2 e;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRT,
        cam.WorldToScreenPoint(edgeW),
        canvasCam,
        out e
    );
    float rPx = Vector2.Distance(c, e);

    // 3) 우상단(angleDeg) 방향으로 (반지름 픽셀 + pixelMargin)만큼 이동
    float a   = angleDeg * Mathf.Deg2Rad;               // 0°=우측, 시계+ (HUD 기준)
    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    float distancePx = rPx * Mathf.Max(0f, radialDistanceMul) + pixelMargin + radialExtraPixels;
    Vector2 hudPos = c + d * distancePx;

    uiRoot.anchoredPosition = hudPos;

    // 4) 화면 밖/뒤면 숨기기(옵션)
    Vector3 sp = cam.WorldToScreenPoint(worldCenter);
    bool visible = sp.z > 0f && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
    if (uiRoot.gameObject.activeSelf != visible) uiRoot.gameObject.SetActive(visible);
}
    void EnsureSprites()
{
    if (!_fallbackWhite)
    {
        var tex = new Texture2D(1,1, TextureFormat.RGBA32, false);
        tex.SetPixel(0,0, Color.white); tex.Apply();
        _fallbackWhite = Sprite.Create(tex, new Rect(0,0,1,1), new Vector2(0.5f,0.5f), 1f);
        _fallbackWhite.name = "MiniTimer_White1x1_Runtime";
    }
    if (fillImage && !fillImage.sprite) fillImage.sprite = _fallbackWhite;
    if (bgImage   && !bgImage.sprite)   bgImage.sprite   = _fallbackWhite;

    // 도넛(라디얼) 설정 보장
    if (fillImage)
    {
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.preserveAspect = true;
    }
    if (bgImage)
    {
        bgImage.type = Image.Type.Filled;
        bgImage.fillMethod = Image.FillMethod.Radial360;
        bgImage.fillOrigin = (int)Image.Origin360.Top;
        bgImage.fillClockwise = true;
        bgImage.fillAmount = 1f; // 항상 가득
        bgImage.preserveAspect = true;
    }
}

void EnsureDonutHoles()
{
    // 진행 링 구멍
    if (fillImage && !_fillHoleRT)
    {
        var go = new GameObject("FillHole", typeof(RectTransform), typeof(Image));
        _fillHoleRT = go.GetComponent<RectTransform>();
        _fillHoleRT.SetParent(fillImage.transform, false);
        _fillHoleRT.anchorMin = _fillHoleRT.anchorMax = new Vector2(0.5f, 0.5f);
        _fillHoleRT.sizeDelta = Vector2.zero;
        _fillHoleRT.localScale = Vector3.one * innerHoleRatio;

        var img = go.GetComponent<Image>();
        img.sprite = _fallbackWhite;
        img.type = Image.Type.Simple;
        img.color = Color.black * 0f; // 완전 투명(배경 뚫린 듯 보이게)
        img.raycastTarget = false;
    }

    // 배경 링 구멍(선택)
    if (bgImage && !_bgHoleRT)
    {
        var go = new GameObject("BgHole", typeof(RectTransform), typeof(Image));
        _bgHoleRT = go.GetComponent<RectTransform>();
        _bgHoleRT.SetParent(bgImage.transform, false);
        _bgHoleRT.anchorMin = _bgHoleRT.anchorMax = new Vector2(0.5f, 0.5f);
        _bgHoleRT.sizeDelta = Vector2.zero;
        _bgHoleRT.localScale = Vector3.one * innerHoleRatio;

        var img = go.GetComponent<Image>();
        img.sprite = _fallbackWhite;
        img.type = Image.Type.Simple;
        img.color = Color.black * 0f;
        img.raycastTarget = false;
    }
}
}
