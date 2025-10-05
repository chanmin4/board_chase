using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ZoneMiniTimerFollower : MonoBehaviour
{
    // 기존 필드들(타이머 비주얼 등)은 그대로 두세요.
    public RectTransform uiRoot;
    public Image bgImage;
    public Image fillImage;

    // ====== 고정 월드 기준(스크린 캔버스용) ======
    Vector3 worldCenter;           // 이 좌표만 따라감 (디스크 무시)
    float   worldRadius;
    float   ttlInit;
    float   remain;

    // ====== 카메라/캔버스 캐시 ======
    Canvas canvas;
    Camera uiCam;                  // Overlay면 null
    RectTransform canvasRT;

    // ====== 월드 부착 모드(선택) ======
    // [MOD] 존 밑(월드)로 붙이고 싶을 때만 사용
    public bool followInWorldSpace = true;                  // [MOD]
    public Transform zoneTransform;                          // [MOD]
    public float angleDeg = 45f, yLift = 0.6f;               // [MOD]
    public float radialDistanceMul = 1.1f, radialExtraPx=30f; // [MOD]

    // ---------- 외부에서 호출 ----------
    // (1) 화면 캔버스에 붙이는 기본 방식 (디스크와 무관)
    public void Setup(Canvas screenCanvas, Vector3 centerWorld, float radiusWorld, float ttlSeconds)
    {
        followInWorldSpace = false;                                  // [MOD]
        canvas  = screenCanvas;
        uiCam   = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        canvasRT= canvas.transform as RectTransform;
        uiRoot  = uiRoot ? uiRoot : (RectTransform)transform;

        worldCenter = centerWorld;   // ★ 디스크 말고 '존' 좌표만 저장
        worldRadius = Mathf.Max(0.001f, radiusWorld);
        ttlInit     = Mathf.Max(0.001f, ttlSeconds);
        remain      = ttlInit;

        UpdateScreenPos(); // 1회 갱신
        UpdateFill();
    }

    // (2) 화면 캔버스 안 쓰고, 존 밑(월드)에 직부착하는 옵션
    public void SetupWorld(Transform zone, float radiusWorld, float ttlSeconds, float angleDegForWorld = 45f) // [MOD]
    {
        followInWorldSpace = true;     // [MOD]
        zoneTransform = zone;          // [MOD]
        worldRadius = Mathf.Max(0.001f, radiusWorld);
        ttlInit     = Mathf.Max(0.001f, ttlSeconds);
        remain      = ttlInit;
        angleDeg    = angleDegForWorld;

        // WorldSpace Canvas 보장(프리팹에 이미 있으면 그대로 사용)
        var c = GetComponentInParent<Canvas>();
        if (!c) c = gameObject.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        if (!c.worldCamera) c.worldCamera = Camera.main;
        canvas = c;
        uiRoot = uiRoot ? uiRoot : (RectTransform)transform;
        uiRoot.sizeDelta   = new Vector2(0.4f, 0.4f);   // 20cm x 20cm
        uiRoot.localScale  = Vector3.one * 0.03f;      // or 원하는 비율
        uiRoot.localRotation = Quaternion.Euler(90f, 0f, 0f); // [FIX] 월드 모드 초기 회전: 바닥에 눕힘

        UpdateWorldPos(); // [MOD]
        UpdateFill();
    }

    // 필요 시 남은 시간 외부 갱신
    public void SetRemain(float s) { remain = Mathf.Clamp(s, 0, ttlInit); UpdateFill(); }
    public float TtlInit => ttlInit;  // 기존 코드 호환용 읽기

    // ---------- 내부 ----------
    void LateUpdate()
    {
        if (followInWorldSpace) { UpdateWorldPos(); return; } // [MOD]
        UpdateScreenPos();
    }

    void UpdateScreenPos()
    {
        if (!canvas || !uiRoot) return;

        var cam = uiCam ? uiCam : Camera.main;
        if (!cam) return;

        // 월드→스크린
        var sp = cam.WorldToScreenPoint(worldCenter);

        // 반지름을 '픽셀'로 근사
        var spEdge = cam.WorldToScreenPoint(worldCenter + cam.transform.right * worldRadius);
        float rPx = Vector2.Distance((Vector2)sp, (Vector2)spEdge);

        // 위치 계산(각도/여백 등은 네가 쓰던 값 유지 가능)
        const float pixelMargin = 30f;
        float a = Mathf.Deg2Rad * angleDeg;
        Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
        Vector2 scr = (Vector2)sp + dir * (rPx * Mathf.Max(0f, radialDistanceMul) + pixelMargin + radialExtraPx);

        // 스크린→캔버스 로컬
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, scr, uiCam, out var local))
            uiRoot.anchoredPosition = local;

        // 화면 밖이면 숨김(원하면 유지)
        bool visible = sp.z > 0f && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
        if (uiRoot.gameObject.activeSelf != visible) uiRoot.gameObject.SetActive(visible);
    }

    // [MOD] 월드 부착 위치 갱신
    void UpdateWorldPos()
    {
        if (!zoneTransform) return;
        float a = Mathf.Deg2Rad * angleDeg;
        Vector3 dir = new(Mathf.Cos(a), 0, Mathf.Sin(a));
        float extraWorld = radialExtraPx * 0.01f; // 픽셀→월드 대충 환산
        float dist = worldRadius * Mathf.Max(0f, radialDistanceMul) + extraWorld;

        transform.position = zoneTransform.position + dir * dist + Vector3.up * yLift;

        // [FIX] 매 프레임 카메라 쪽으로 세우던 회전 제거하고, 바닥에 눕힌 각도로 고정
        // if (Camera.main) transform.rotation =
        //     Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f); // [FIX]
    }

    void UpdateFill()
    {
        if (!fillImage) return;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = ttlInit <= 0 ? 0 : Mathf.Clamp01(remain / ttlInit);
    }
}
