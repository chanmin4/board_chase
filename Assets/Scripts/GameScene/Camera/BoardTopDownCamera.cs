using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class BoardTopDownCamera : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;

    [Header("View")]
    public bool useOrthographic = true;     // 기본: 직교 탑뷰
    public float topDownHeight = 15f;       // 직교일 때 카메라 높이(Y)
    public float extraPaddingWorld = 1.0f;  // 보드 외곽에 추가 여유(월드 m)

    [Header("Auto Fit")]
    public bool autoFit = true;             // 해상도/인스펙터 변경 시 자동 맞춤

    Camera cam;
    float lastAspect = -1f;

    void Reset() { cam = GetComponent<Camera>(); useOrthographic = true; }
    void OnEnable() { if (!cam) cam = GetComponent<Camera>(); if (autoFit) FitNow(); }
#if UNITY_EDITOR
    void OnValidate() { if (!cam) cam = GetComponent<Camera>(); if (autoFit) FitNow(); }
    void Update() { if (!Application.isPlaying && autoFit) CheckAspect(); }
#endif
    void LateUpdate() { if (Application.isPlaying && autoFit) CheckAspect(); }

    void CheckAspect()
    {
        if (!cam || !board) return;
        if (Mathf.Abs(cam.aspect - lastAspect) > 0.001f) FitNow();
    }

    [ContextMenu("Fit Now")]
    public void FitNow()
    {
        if (!board || !cam) return;

        // 보드 외곽(Rect: x=worldX, y=worldZ)
        Rect r = board.GetWallOuterRectXZ();
        float W = r.width  + extraPaddingWorld * 2f;
        float H = r.height + extraPaddingWorld * 2f;

        Vector2 c = r.center;

        // 탑뷰 위치/회전
        Vector3 pos = transform.position;
        pos.x = c.x;
        pos.z = c.y;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (useOrthographic)
        {
            cam.orthographic = true;
            pos.y = board.origin.y + topDownHeight;    // 높이는 보기 좋게 고정
            transform.position = pos;

            // 직교 사이즈 계산: 세로/가로 중 큰 쪽에 맞춤
            float halfH = H * 0.5f;
            float halfW = (W * 0.5f) / Mathf.Max(0.0001f, cam.aspect);
            cam.orthographicSize = Mathf.Max(halfH, halfW);
        }
        else
        {
            // 원근 탑뷰: FOV로부터 필요 거리 계산
            cam.orthographic = false;
            float size = Mathf.Max(H * 0.5f, (W * 0.5f) / Mathf.Max(0.0001f, cam.aspect));
            float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float dist = size / Mathf.Tan(halfFovRad);
            pos.y = board.origin.y + dist; // 바로 위에서 내려다봄
            transform.position = pos;
        }

        lastAspect = cam.aspect;
    }
}
