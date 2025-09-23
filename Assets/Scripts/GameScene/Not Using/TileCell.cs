/*
// TileCell.cs
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Renderer))]
public class TileCell : MonoBehaviour
{
    public TileCenter center;


    void Reset(){
        if (!center) center = GetComponentInChildren<TileCenter>();
        EnsureLabel3D();          // 프리팹에 없어도 자동 생성
    }

    void Awake()      { EnsureLabel3D(); }
    void OnValidate() { EnsureLabel3D(); }

    // ──────────────────────────────────────────────────────────
    // 라벨 자동 생성/배치 (Canvas 필요 없음, 3D TMP 사용)
    void EnsureLabel3D()
    {
        if (!center) center = GetComponentInChildren<TileCenter>();

    }

    // ──────────────────────────────────────────────────────────

    // TileCenter.type을 읽어 자동으로 숫자/물음표 갱신
    public void RefreshLabelFromType()
    {
        EnsureLabel3D();
        if (!center) return;

        switch (center.type)
        {
            case TileType.Face1: break;
            case TileType.Face2: break;
            case TileType.Face3: break;
            case TileType.Face4: break;
            case TileType.Face5: break;
            case TileType.Face6: break;
            case TileType.Random: break;
        }
    }
}

*/