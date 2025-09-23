// CameraFitToBoardSimple.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraFitToBoard : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;       // 보드 그리드(외곽 Rect는 board.GetWallOuterRectXZ() 사용)

    [Header("Framing")]
    [Tooltip("카메라 프레이밍 여백(월드 유닛)")]
    public float cameraPadding = 0f;

    [Tooltip("카메라 Y 높이(탑다운)")]
    public float cameraHeight = 10f;

    [Tooltip("벽 바깥이 보이지 않게 레터/필러박스 사용")]
    public bool useLetterbox = true;

    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        ApplyNow();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        ApplyNow();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying) ApplyNow();
    }
#endif

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        if (!board) return;
        if (!cam) cam = GetComponent<Camera>();

        cam.orthographic = true;

        // 1) 보드 외곽 Rect(xz 평면) + 카메라 여백
        var r = board.GetWallOuterRectXZ();
        r.xMin -= cameraPadding; r.xMax += cameraPadding;
        r.yMin -= cameraPadding; r.yMax += cameraPadding;

        // 2) 카메라 위치/각도(탑다운)
        var center = new Vector3(r.center.x, cameraHeight, r.center.y);
        transform.position = center;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 3) 오쏘사이즈 & 레터박스
        float outW = Mathf.Max(0.0001f, r.width);
        float outH = Mathf.Max(0.0001f, r.height);
        float halfW = outW * 0.5f;
        float halfH = outH * 0.5f;

        float sizeByH = halfH;                                  // 세로 기준
        float sizeByW = halfW / Mathf.Max(0.0001f, cam.aspect); // 가로를 세로로 환산

        if (!useLetterbox)
        {
            // 화면 전체에 담되(Contain), 여백은 허용
            cam.rect = new Rect(0, 0, 1, 1);
            cam.orthographicSize = Mathf.Max(sizeByH, sizeByW);
        }
        else
        {
            // 한 변을 정확히 맞추고, 나머지는 레터/필러박스
            cam.orthographicSize = Mathf.Min(sizeByH, sizeByW);

            float targetAspect = outW / outH;
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            if (screenAspect > targetAspect) // 좌우가 더 넓음 → 필러박스
            {
                float w = targetAspect / screenAspect;
                cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else                             // 상하가 더 큼 → 레터박스
            {
                float h = screenAspect / targetAspect;
                cam.rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }
    }

    void OnDisable()
    {
        // 껐을 때 화면 정상화(원하면 제거 가능)
        if (cam) cam.rect = new Rect(0, 0, 1, 1);
    }
}
