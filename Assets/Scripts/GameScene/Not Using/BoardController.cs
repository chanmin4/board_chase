/*
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
    //int prevL = int.MinValue;
      int startFace = 1;
    void Awake()
    {
        // 씬의 모든 TileCell 등록 (TileCenter.index 필수)
        foreach (var cell in FindObjectsByType<TileCell>(FindObjectsSortMode.None))
            if (cell && cell.center != null)
                cells[cell.center.index] = cell;
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

}


}

*/