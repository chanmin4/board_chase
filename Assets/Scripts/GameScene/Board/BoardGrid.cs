using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class BoardGrid : MonoBehaviour
{
    [Header("Grid")]
    public int width = 40;
    public int height = 40;
    public float tileSize = 1f;
    public Vector3 origin = Vector3.zero;   // 좌하단(월드)

    // ─────────────────────────────────────────────────────────────
    [Header("Walls (attach existing WindowWall)")]
    public WindowWall windowWall;          // 기존 프리팹/오브젝트 drag&drop
    [Tooltip("보드 경계 밖으로 벽을 얼마나 띄울지(월드 유닛).")]
    public float wallPadding = 0f;
    [Tooltip("값이 바뀔 때마다 자동으로 벽을 갱신합니다.")]
    public bool autoLayoutWalls = true;

    // ─────────────────────────────────────────────────────────────
    [Header("Rotating Spinners (optional)")]
    public Transform spinnerA;
    public Transform spinnerB;

    [Tooltip("보드 폭 대비 X 위치 비율 (0~1). 예: 0.333 = 1/3, 0.666 = 2/3")]
    [Range(0f, 1f)] public float spinnerA_XFrac = 1f / 4f;
    [Range(0f, 1f)] public float spinnerB_XFrac = 3f / 4f;

    [Tooltip("보드 높이(Z) 대비 세로 위치 비율 (0=아래, 0.5=중앙, 1=위)")]
    [Range(0f, 1f)] public float spinnerZFrac = 0.5f;

    [Tooltip("스피너의 월드 Y 높이(바닥에서 띄우고 싶을 때)")]
    public float spinnerY = 0.0f;

    [Tooltip("값이 바뀔 때마다 자동으로 스피너를 배치합니다.")]
    public bool autoPlaceSpinners = true;

    // ─────────────────────────────────────────────────────────────
    // 기존 헬퍼 유지
    public bool InBounds(int ix, int iy) => ix >= 0 && iy >= 0 && ix < width && iy < height;

    public bool WorldToIndex(Vector3 world, out int ix, out int iy)
    {
        Vector3 local = world - origin;
        ix = Mathf.FloorToInt(local.x / tileSize);
        iy = Mathf.FloorToInt(local.z / tileSize);
        return InBounds(ix, iy);
    }

    public Vector3 IndexToWorld(int ix, int iy)
    {
        return origin + new Vector3((ix + 0.5f) * tileSize, 0f, (iy + 0.5f) * tileSize);
    }

    public bool SnapToNearest(ref Vector3 pos, out int ix, out int iy)
    {
        if (!WorldToIndex(pos, out ix, out iy))
        {
            Vector3 local = pos - origin;
            ix = Mathf.Clamp(Mathf.FloorToInt(local.x / tileSize), 0, width - 1);
            iy = Mathf.Clamp(Mathf.FloorToInt(local.z / tileSize), 0, height - 1);
        }
        pos = IndexToWorld(ix, iy);
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        ApplyWallsAndSpinners();
    }

    void OnValidate()
    {
        if (autoLayoutWalls || autoPlaceSpinners)
            ApplyWallsAndSpinners();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying && (autoLayoutWalls || autoPlaceSpinners))
            ApplyWallsAndSpinners();
    }
#endif

    [ContextMenu("Apply Walls & Spinners")]
    public void ApplyWallsAndSpinners()
    {
        if (autoLayoutWalls) LayoutWalls();
        if (autoPlaceSpinners) PlaceSpinners();
    }

    // ─────────────────────────────────────────────────────────────
    // 보드 외곽에 기존 WindowWall(wallL/R/F/B)를 "부착" (포지션/스케일만 조정)
    public void LayoutWalls()
    {
        if (!windowWall) return;

        float ts = Mathf.Max(0.0001f, tileSize);
        int   wN = Mathf.Max(0, width);
        int   hN = Mathf.Max(0, height);

        float boardW = wN * ts;
        float boardH = hN * ts;

        // 보드 외곽 + 패딩
        float xMin = origin.x - wallPadding;
        float xMax = origin.x + boardW + wallPadding;
        float zMin = origin.z - wallPadding;
        float zMax = origin.z + boardH + wallPadding;

        float midX = (xMin + xMax) * 0.5f;
        float midZ = (zMin + zMax) * 0.5f;

        float outerW = Mathf.Max(0f, xMax - xMin);
        float outerH = Mathf.Max(0f, zMax - zMin);

        float th = Mathf.Max(0.0001f, windowWall.thickness);
        float hh = Mathf.Max(0.0001f, windowWall.height);
        float centerY = origin.y + hh * 0.5f;
        // 좌/우 벽 (길이 = outerH, 두께 = th)
        if (windowWall.wallL)
        {
            windowWall.wallL.position = new Vector3(xMin, centerY, midZ);
            //windowWall.wallL.localScale = new Vector3(th, hh, outerH);
        }
        else Debug.LogWarning("[BoardGrid] WindowWall.wallL is null — skipped.");

        if (windowWall.wallR)
        {
            windowWall.wallR.position = new Vector3(xMax, centerY, midZ);
            //windowWall.wallR.localScale = new Vector3(th, hh, outerH);
        }
        else Debug.LogWarning("[BoardGrid] WindowWall.wallR is null — skipped.");

        // 앞/뒤 벽 (길이 = outerW, 두께 = th)
        if (windowWall.wallF)
        {
            windowWall.wallF.position = new Vector3(midX, centerY, zMax);
            //windowWall.wallF.localScale = new Vector3(outerW, hh, th);
        }
        else Debug.LogWarning("[BoardGrid] WindowWall.wallF is null — skipped.");

        if (windowWall.wallB)
        {
            windowWall.wallB.position = new Vector3(midX, centerY, zMin);
            //windowWall.wallB.localScale = new Vector3(outerW, hh, th);
        }
        else Debug.LogWarning("[BoardGrid] WindowWall.wallB is null — skipped.");
    }

    // ─────────────────────────────────────────────────────────────
    // 스피너 두 개를 비율 기반으로 배치(위치만 조정; 회전/레이어/컴포넌트는 그대로)
    public void PlaceSpinners()
    {
        float ts = Mathf.Max(0.0001f, tileSize);
        int   wN = Mathf.Max(0, width);
        int   hN = Mathf.Max(0, height);

        float boardW = wN * ts;
        float boardH = hN * ts;

        float x0 = origin.x;
        float z0 = origin.z;

        float zPos = z0 + boardH * Mathf.Clamp01(spinnerZFrac);

        if (spinnerA)
        {
            float xA = x0 + boardW * Mathf.Clamp01(spinnerA_XFrac);
            spinnerA.position = new Vector3(xA, origin.y + spinnerY, zPos);
        }
        if (spinnerB)
        {
            float xB = x0 + boardW * Mathf.Clamp01(spinnerB_XFrac);
            spinnerB.position = new Vector3(xB, origin.y + spinnerY, zPos);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 카메라가 쓸 수 있도록 외곽 Rect 제공(xz 평면)
    public Rect GetBoardRectXZ()
    {
        float ts = Mathf.Max(0.0001f, tileSize);
        float w = Mathf.Max(0, width)  * ts;
        float h = Mathf.Max(0, height) * ts;
        return new Rect(origin.x, origin.z, w, h);
    }

    public Rect GetWallOuterRectXZ()
    {
        float ts = Mathf.Max(0.0001f, tileSize);
        float w = Mathf.Max(0, width) * ts;
        float h = Mathf.Max(0, height) * ts;

        float halfTh = 0f;
        if (windowWall) halfTh = Mathf.Max(0.0001f, windowWall.thickness) * 0.5f;

        float pad = wallPadding + halfTh;

        // 보드 바깥으로 '패딩 + 벽 두께의 절반'만큼 더 잡아준다
        float x = origin.x - pad;
        float z = origin.z - pad;
        float W = w + pad * 2f;
        float H = h + pad * 2f;
        return new Rect(x, z, W, H);
    }

}
