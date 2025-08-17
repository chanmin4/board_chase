// TileCell.cs
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Renderer))]
public class TileCell : MonoBehaviour
{
    public TileCenter center;
    public TMP_Text label;

    [Header("Label Auto Setup")]
    public float labelHeight = 0.015f;               // 바닥 지글거림 방지
    public Vector3 labelScale = new(0.1f, 0.1f, 0.1f);

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
        if (!label)
        {
            // 3D TextMeshPro 생성
            var go = new GameObject("Text (TMP)");
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshPro>();

            // 기본 가독성 옵션
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.5f;
            label.fontSizeMax = 6f;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = "?";
        }

        // 타일 중앙에 눕혀서 작게
        var t = label.transform;
        t.localPosition = new Vector3(0f, labelHeight, 0f);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);
        t.localScale    = labelScale;
    }

    // ──────────────────────────────────────────────────────────
    // 원하는 글씨를 직접 쓰고 싶을 때 호출
    public void SetText(string s)
    {
        EnsureLabel3D();
        label.text = s;
    }

    // TileCenter.type을 읽어 자동으로 숫자/물음표 갱신
    public void RefreshLabelFromType()
    {
        EnsureLabel3D();
        if (!center) return;

        switch (center.type)
        {
            case TileType.Face1: label.text = "1"; break;
            case TileType.Face2: label.text = "2"; break;
            case TileType.Face3: label.text = "3"; break;
            case TileType.Face4: label.text = "4"; break;
            case TileType.Face5: label.text = "5"; break;
            case TileType.Face6: label.text = "6"; break;
            case TileType.Random: default: label.text = "?"; break;
        }
    }
}
