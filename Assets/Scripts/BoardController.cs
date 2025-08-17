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
    [Header("Spawn (옵션)")]
        public Transform tileParent;       // 새 타일 부모(없으면 this)
        public TileCell tilecell;        // 새 타일이 필요할 때 생성할 프리팹

    // index(절대) -> TileCell
    Dictionary<int, TileCell> cells = new();
    int prevL = int.MinValue;
      int startFace = 1;
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
    int L = tc.index;

    // 1) 착지 타입 분석 → 다음 턴 시작 숫자 결정
    switch (tc.type)
    {
        case TileType.Random:
            startFace = Random.Range(1, 7);                           // 주사위 결과
            break;
        case TileType.Face1:
        case TileType.Face2:
        case TileType.Face3:
        case TileType.Face4:
        case TileType.Face5:
        case TileType.Face6:
            startFace = (int)tc.type - (int)TileType.Face1 + 1;       // N
            break;
    }

    // 2) 필요한 범위 보장 (index는 계속 증가)
    EnsureRange(L - 1, L + 11);

    // 3) 현재 창 타입 재배치 (표시는 하지 않음)
    SetWindowTypes(L);

    // 4) type을 읽어 라벨만 갱신
    RefreshLabels(L - 1, L + 11);

    // (선택) 왼쪽 끝 Random(L-1)로 디스크 워프해 다음 턴 시작
    if (launcher != null && cells.TryGetValue(L - 1, out var left) && left?.center)
        launcher.WarpTo(left.center);

    // (선택) 유리벽 이동
    // float fromX = (L - 1) * tileLen, toX = (L + 5) * tileLen;
    // windowwall?.SetWindowSmooth(fromX, toX, halfZ);
}
void SetWindowTypes(int L)
{
    // S0 = Random
    if (cells.TryGetValue(L - 1, out var c0) && c0?.center) c0.center.type = TileType.Random;

    // S1..S6 = Face(startFace..)
    for (int i = 0; i <= 5; i++)
    {
        if (!cells.TryGetValue(L + i, out var cell) || !cell?.center) continue;
        int face = ((startFace - 1 + i) % 6); // 0..5
        cell.center.type = (TileType)((int)TileType.Face1 + face);
    }

    // S7..S12 = Random (미리보기)
    for (int i = 6; i <= 11; i++)
    {
        if (!cells.TryGetValue(L + i, out var cr) || !cr?.center) continue;
        cr.center.type = TileType.Random;
    }
}

TileCell EnsureTile(int idx)
{
    if (cells.TryGetValue(idx, out var exist) && exist) return exist;
    if (tilecell == null) return null;

    var parent = tileParent != null ? tileParent : transform;
    var tile = Instantiate(tilecell, parent);

    // 위치 & 인덱스
    var t = tile.transform;
    t.position = new Vector3(idx * tileLen, 0f, 0f);
    if (!tile.center) tile.center = tile.GetComponentInChildren<TileCenter>();
    if (tile.center)  tile.center.index = idx;

    // ★ 라벨 보장(없으면 자동 생성, 배치)
    tile.RefreshLabelFromType();     // 초기 표시(기본 type 기준)

    cells[idx] = tile;
    return tile;
}

void RefreshLabels(int a, int b)
{
    for (int i = a; i <= b; i++)
        if (cells.TryGetValue(i, out var cell) && cell)
            cell.RefreshLabelFromType();  // type→글씨 자동 갱신
}
    // --- 새로 추가: [a..b] 범위 타일 보장 ---
    void EnsureRange(int a, int b)
    {
        for (int i = a; i <= b; i++)
            EnsureTile(i);

        // 선택: 범위 밖(왼쪽 오래된 타일)은 비활성화(“사라진” 느낌)
        foreach (var kv in cells)
        {
            bool inside = (kv.Key >= a && kv.Key <= b);
            if (kv.Value != null)
                kv.Value.gameObject.SetActive(inside);
        }
    }

}
