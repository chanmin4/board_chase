using UnityEngine;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    [Header("Refs")]
    public DiskLauncher launcher;     // 디스크 멈춤 이벤트 소스
    public WindowWall windowwall;     // RegionWindow와 동일 역할(투명 유리벽)

    [Header("Board Metric")]
    public float tileLen = 2.0f;      // 칸 길이(월드)
    public float halfZ   = 1.5f;      // 유리벽 Z 반폭

    [Header("Colors")]
    public Color activeColor   = new(1,1,1,1);      // 활성(뒤1~앞6)
    public Color inactiveColor = new(1,1,1,0.25f);  // 비활성(그 외)
    public Color previewColor  = new(1,1,1,0.7f);   // tile_7~tile_12 미리보기(랜덤)

    // index(절대) -> TileCell
    Dictionary<int, TileCell> cells = new();

    void Awake()
    {
        // 씬의 모든 TileCell 등록 (TileCenter.index 필수)
        foreach (var cell in FindObjectsByType<TileCell>(FindObjectsSortMode.None))
            if (cell && cell.center != null)
                cells[cell.center.index] = cell;
    }

    void OnEnable()
    {
        if (launcher != null) launcher.OnStoppedOnTile += HandleLanded;
    }
    void OnDisable()
    {
        if (launcher != null) launcher.OnStoppedOnTile -= HandleLanded;
    }

    void HandleLanded(TileCenter tc)
    {
        if (tc == null) return;
        int L = tc.index;          // 이번에 멈춘 칸(=tile_1)

        // 1) 유리벽은 [L-1 .. L+5] 범위(뒤1+앞6)만 감쌈
        float fromX = (L - 1) * tileLen;
        float toX   = (L + 5) * tileLen;
        if (windowwall != null) windowwall.SetWindowSmooth(fromX, toX, halfZ);

        // 2) 타일 스타일 재배치
        UpdateTiles(L);

        // 3) 착지 타일 효과(랜덤 주사위 등)
        ResolveEffect(tc);
    }

    void UpdateTiles(int L)
    {
        // 일단 전부 비활성 톤
        foreach (var kv in cells) kv.Value.SetColor(inactiveColor, inactiveColor.a);

        // random_1 = L-1
        PaintRandom(L - 1, preview:false);

        // tile_1..tile_6 = L..L+5
        for (int i = 0; i <= 5; i++)
            PaintFace(L + i, i + 1); // Face1~6

        // tile_7..tile_12 = L+6..L+11 (미리보기 랜덤)
        for (int i = 6; i <= 11; i++)
            PaintRandom(L + i, preview:true);
    }

    void PaintFace(int idx, int face)
    {
        if (!cells.TryGetValue(idx, out var cell)) return;
        cell.SetColor(activeColor, activeColor.a);
        if (cell.center) cell.center.type = (TileType)((int)TileType.Face1 + (face - 1));
        // 숫자 텍스트/스프라이트 갱신은 여기에서
    }

    void PaintRandom(int idx, bool preview)
    {
        if (!cells.TryGetValue(idx, out var cell)) return;
        var col = preview ? previewColor : activeColor;
        var a   = preview ? previewColor.a : activeColor.a;
        cell.SetColor(col, a);
        if (cell.center) cell.center.type = TileType.Random;
        // 미리보기면 "?" 아이콘 표시 등
    }

    void ResolveEffect(TileCenter landed)
    {
        // 착지한 칸의 즉시 효과 처리
        switch (landed.type)
        {
            case TileType.Random:
                int roll = Random.Range(1, 7);
                Debug.Log($"[Random] roll={roll} at {landed.index}");
                // 연출/버프/패널티 적용 지점
                break;

            case TileType.Face1:
            case TileType.Face2:
            case TileType.Face3:
            case TileType.Face4:
            case TileType.Face5:
            case TileType.Face6:
                int face = (int)landed.type - (int)TileType.Face1 + 1;
                Debug.Log($"[Face] {face} at {landed.index}");
                // 페이스별 룰 적용 지점
                break;

            default:
                break;
        }
    }
}
