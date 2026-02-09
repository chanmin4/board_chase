using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerDisk : MonoBehaviour
{
    [Header("Refs (auto-find if null)")]
    public BoardGrid board;
    public SurvivalDirector director;            // zones disabled 버전이면 PaintPlayerCircleWorld 존재
    public BoardMaskRenderer maskRenderer;       // BoardPaintSystem 없을 때 fallback용
    public BoardPaintSystem paintSystem;         // ★ 권장(게이지 소모 포함)
    public SurvivalGauge gauge;
    public Camera aimCamera;

    [Header("Legacy Disable")]
    public bool disableDragAimOnAwake = true;
    public bool disableLegacyLauncherMinSpeed = true;
    public bool disableLegacyCooldownMode = true;

    [Header("Aim Plane")]
    public bool clampAimToBoardRect = true;

    public Rigidbody Rb { get; private set; }

    // 레거시(프로젝트에 DiskLauncher가 많이 물려있으니, 당장은 "삭제/리네임" 하지 말고 그대로 두는 걸 권장)
   //public DragAimController legacyDragAim { get; private set; }

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();

        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!paintSystem) paintSystem = FindAnyObjectByType<BoardPaintSystem>();
        if (!gauge) gauge = FindAnyObjectByType<SurvivalGauge>();
        if (!aimCamera) aimCamera = Camera.main;
    }

    public float GroundY
    {
        get
        {
            if (board) return board.origin.y;
            return transform.position.y;
        }
    }

    public bool TryGetAimPoint(out Vector3 aimPoint)
    {
        aimPoint = transform.position;

        if (!aimCamera)
            aimCamera = Camera.main;
        if (!aimCamera) return false;

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);

        // 보드 평면(y=board.origin.y)과 교차
        Plane plane = new Plane(Vector3.up, new Vector3(0f, GroundY, 0f));
        if (!plane.Raycast(ray, out float t))
            return false;

        Vector3 p = ray.GetPoint(t);

        if (clampAimToBoardRect && board)
        {
            Rect r = board.GetBoardRectXZ();
            p.x = Mathf.Clamp(p.x, r.xMin, r.xMax);
            p.z = Mathf.Clamp(p.z, r.yMin, r.yMax);
            p.y = GroundY;
        }

        aimPoint = p;
        return true;
    }

    /// <summary>
    /// 페인트 스탬프(가능하면 BoardPaintSystem 사용: 게이지 소모 + 마스크 업데이트)
    /// </summary>
    public bool TryPaintPlayerCircle(Vector3 centerWorld, float radiusWorld, bool clearEnemyMask = true)
    {
        // 1) 권장: BoardPaintSystem(게이지 소모 포함)
        if (paintSystem)
            return paintSystem.TryStampCircleNow(BoardPaintSystem.PaintChannel.Player, centerWorld, radiusWorld, clearEnemyMask);

        // 2) fallback: director 이벤트 파이프 (게이지 소모는 직접 처리 X)
        if (director)
        {
            director.PaintPlayerCircleWorld(centerWorld, radiusWorld, applyBoardClean: false, clearPollutionMask: clearEnemyMask);
            return true;
        }

        // 3) fallback: maskRenderer 직접
        if (maskRenderer)
        {
            maskRenderer.PaintPlayerCircleWorld_Batched(centerWorld, radiusWorld, clearEnemyMask);
            return true;
        }

        return false;
    }
}
