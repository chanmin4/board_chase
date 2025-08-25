using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class ContamMaskRenderer : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Render")]
    public Material maskMat;             // 아래 2) 셰이더로 만든 머티리얼 할당
    [Range(2, 64)] public int pixelsPerTile = 8;
    public float yOffset = 0.01f;        // 바닥과 Z-fight 방지

    Texture2D _mask;
    Color32[] _buf;
    int _w, _h;                          // 텍스처 크기(픽셀)
    bool _dirty;

    void OnEnable()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();

        BuildMeshQuad();     // 보드 크기에 맞는 XZ 쿼드 한 장
        BuildMaskTexture();  // 보드 전체 마스크 텍스처 준비
        Subscribe(true);
    }
    void OnDisable() => Subscribe(false);

    void Subscribe(bool on)
    {
        if (!director) return;
        if (on)
        {
            director.OnZonesReset             += HandleReset;               // 세트 리셋: 비주얼 유지
            director.OnZoneContaminatedCircle += HandleContamCircle;        // 오염 추가
            director.OnClearedCircleWorld     += HandleClearedCircle;       // 지우개(청소)
        }
        else
        {
            director.OnZonesReset             -= HandleReset;
            director.OnZoneContaminatedCircle -= HandleContamCircle;
            director.OnClearedCircleWorld     -= HandleClearedCircle;
        }
    }

    void HandleReset() { /* 비주얼 유지 */ }

    void HandleContamCircle(int _, Vector3 centerW, float radiusW)
    {
        StampCircle(centerW, radiusW, true);   // 오염 추가(=1)
    }

    void HandleClearedCircle(Vector3 centerW, float radiusW)
    {
        StampCircle(centerW, radiusW, false);  // 청소(=0)
    }

    void BuildMaskTexture()
    {
        _w = Mathf.Max(1, board.width  * pixelsPerTile);
        _h = Mathf.Max(1, board.height * pixelsPerTile);

        _mask = new Texture2D(_w, _h, TextureFormat.Alpha8, false, true);
        _mask.wrapMode = TextureWrapMode.Clamp;
        _mask.filterMode = FilterMode.Bilinear; // 가장자리를 부드럽게
        _buf = new Color32[_w * _h];

        // 초기화(전부 0)
        for (int i = 0; i < _buf.Length; i++) _buf[i] = new Color32(0,0,0,0);
        _mask.SetPixels32(_buf);
        _mask.Apply(false, false);

        if (!maskMat) Debug.LogWarning("[ContamMask] maskMat not assigned");
        else maskMat.SetTexture("_MaskTex", _mask);
    }

    void BuildMeshQuad()
    {
        // 보드 폭/높이(월드)와 중심
        float tile = board.tileSize;
        float w = board.width  * tile;
        float h = board.height * tile;
        Vector3 origin = board.origin;
        Vector3 center = new Vector3(origin.x + w * 0.5f, origin.y + yOffset, origin.z + h * 0.5f);

        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        // XZ 1x1 유닛 쿼드 생성(-0.5..0.5)
        var m = new Mesh { name = "BoardQuad_XZ" };
        m.vertices  = new Vector3[] {
            new(-0.5f, 0f, -0.5f),
            new( 0.5f, 0f, -0.5f),
            new( 0.5f, 0f,  0.5f),
            new(-0.5f, 0f,  0.5f)
        };
        m.uv = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
        m.triangles = new int[] { 0,1,2, 0,2,3 };
        m.RecalculateNormals(); m.RecalculateBounds();
        mf.sharedMesh = m;

        transform.position = center;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(w, 1f, h);

        if (maskMat) mr.sharedMaterial = maskMat;
    }

    // 보드 월드 → 텍스처 픽셀 좌표
    bool WorldToPixel(Vector3 wpos, out int px, out int py)
    {
        float tile = board.tileSize;
        Vector3 o = board.origin;
        float x01 = (wpos.x - o.x) / (board.width * tile);
        float y01 = (wpos.z - o.z) / (board.height * tile);
        px = Mathf.RoundToInt(x01 * (_w - 1));
        py = Mathf.RoundToInt(y01 * (_h - 1));
        return (px >= 0 && px < _w && py >= 0 && py < _h);
    }

    void StampCircle(Vector3 centerW, float radiusW, bool add)
    {
        if (!WorldToPixel(centerW, out int cx, out int cy)) return;

        int r = Mathf.CeilToInt(radiusW * pixelsPerTile / Mathf.Max(0.0001f, board.tileSize));
        int minX = Mathf.Max(0, cx - r);
        int maxX = Mathf.Min(_w - 1, cx + r);
        int minY = Mathf.Max(0, cy - r);
        int maxY = Mathf.Min(_h - 1, cy + r);

        int w = _w;
        float r2 = (r + 0.5f) * (r + 0.5f); // 살짝 라운드
        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                // 원판 내부만 처리
                if (dx * dx + dy * dy <= r2)
                {
                    int idx = y * w + x;
                    if (add) _buf[idx] = new Color32(0, 0, 0, 255); // 오염 = 1
                    else     _buf[idx] = new Color32(0, 0, 0,   0); // 청소 = 0
                }
            }
        }
        _mask.SetPixels32(_buf);
        _mask.Apply(false, false);
        _dirty = true;
    }
}
