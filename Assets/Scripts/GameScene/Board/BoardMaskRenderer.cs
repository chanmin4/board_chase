using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class PaintMaskRenderer : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Render (Contam / Enemy)")]
    public Material maskMat;                       // 적(오염)용 머티리얼 (Unlit/ContamMask, _MaskTex 사용)
    [Range(2, 64)] public int pixelsPerTile = 12;
    public float yOffset = 0.02f;

    [Header("Render (Player)")]
    public Material playerMat;                     // 플레이어용 머티리얼 (동일 셰이더 권장)
    public string playerMaskProperty = "_MaskTex"; // 셰이더의 마스크 프로퍼티명
    [Range(2, 64)] public int playerPixelsPerTile = 4;

    // runtime
    MeshRenderer _mr;
    Material _contamMatInst;   // 인스턴스
    Material _playerMatInst;   // 인스턴스

    Texture2D _contamMask;     // 적(오염) 마스크
    Color32[] _contamBuf;

    Texture2D _playerMask;     // 플레이어 마스크
    Color32[] _playerBuf;

    void OnEnable()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();

        BuildBoardQuadAndMaterials();         // 쿼드 1장 + 재질 2장
        BuildContamMaskTexture();             // 적(오염) 마스크 텍스처 생성/연결
        BuildPlayerMaskTexture();             // 플레이어 마스크 텍스처 생성/연결
        Subscribe(true);
    }

    void OnDisable() => Subscribe(false);

    void Subscribe(bool on)
    {
        if (!director) return;
        if (on)
        {
            director.OnZonesReset                += HandleReset;
            director.OnZoneContaminatedCircle    += HandleContamCircle;   // 적 페인트
            director.OnClearedCircleWorld        += HandleClearedCircle;  // 오염만 클리어
            director.OnPlayerPaintCircleWorld    += HandlePlayerPaint;    // 플레이어 페인트
        }
        else
        {
            director.OnZonesReset                -= HandleReset;
            director.OnZoneContaminatedCircle    -= HandleContamCircle;
            director.OnClearedCircleWorld        -= HandleClearedCircle;
            director.OnPlayerPaintCircleWorld    -= HandlePlayerPaint;
        }
    }

    // ========== Handlers ==========
    void HandleReset()
    {
        // 필요 시 초기화 원하면 주석 해제
        // ClearAll(_contamMask, _contamBuf);
        // ClearAll(_playerMask, _playerBuf);
    }

    void HandleContamCircle(int _, Vector3 centerW, float radiusW)
    {
        // 적(오염) 찍기
        StampCircle(_contamMask, _contamBuf, pixelsPerTile, centerW, radiusW, 255);
        _contamMask.Apply(false, false);

        // 라스트터치: 같은 영역에서 플레이어는 0으로
        if (_playerMask)
        {
            StampCircle(_playerMask, _playerBuf, playerPixelsPerTile, centerW, radiusW, 0);
            _playerMask.Apply(false, false);
        }
    }

    void HandleClearedCircle(Vector3 centerW, float radiusW)
    {
        // ‘청소’ 이벤트는 오염만 0 — 플레이어 색은 유지
        StampCircle(_contamMask, _contamBuf, pixelsPerTile, centerW, radiusW, 0);
        _contamMask.Apply(false, false);
    }

    void HandlePlayerPaint(Vector3 centerW, float radiusW, bool clearPollutionMask)
    {
        // 플레이어 찍기
        if (_playerMask)
        {
            StampCircle(_playerMask, _playerBuf, playerPixelsPerTile, centerW, radiusW, 255);
            _playerMask.Apply(false, false);
        }

        // 옵션: 같은 영역의 오염 0으로 (라스트터치)
        if (clearPollutionMask && _contamMask)
        {
            StampCircle(_contamMask, _contamBuf, pixelsPerTile, centerW, radiusW, 0);
            _contamMask.Apply(false, false);
        }
    }

    // ========== Build ==========

    void BuildBoardQuadAndMaterials()
    {
        float tile = board.tileSize;
        float w = board.width * tile;
        float h = board.height * tile;
        Vector3 o = board.origin;
        Vector3 center = new(o.x + w * 0.5f, o.y + yOffset, o.z + h * 0.5f);

        var mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();

        // XZ 유닛 쿼드(-0.5~0.5)
        var m = new Mesh { name = "BoardQuad_XZ" };
        m.vertices  = new[] { new Vector3(-0.5f,0,-0.5f), new Vector3(0.5f,0,-0.5f), new Vector3(0.5f,0,0.5f), new Vector3(-0.5f,0,0.5f) };
        m.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        m.triangles = new[] { 0,1,2, 0,2,3 };
        m.RecalculateNormals(); m.RecalculateBounds();
        mf.sharedMesh = m;

        transform.position   = center;
        transform.rotation   = Quaternion.identity;
        transform.localScale = new Vector3(w, 1f, h);

        // 재질 2장(적/플레이어) 인스턴스 할당
        _contamMatInst = maskMat ? new Material(maskMat) : null;
        _playerMatInst = playerMat ? new Material(playerMat) : null;

        var mats = new Material[2];
        mats[0] = _contamMatInst;
        mats[1] = _playerMatInst;
        _mr.sharedMaterials = mats;

        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows = false;

        if (_contamMatInst) _contamMatInst.renderQueue = Mathf.Max(_contamMatInst.renderQueue, 3600);
        if (_playerMatInst) _playerMatInst.renderQueue = Mathf.Max(_playerMatInst.renderQueue, 3700);
    }

    void BuildContamMaskTexture()
    {
        BuildMaskFor(_contamMatInst, pixelsPerTile, out _contamMask, out _contamBuf, "_MaskTex");
    }

    void BuildPlayerMaskTexture()
    {
        BuildMaskFor(_playerMatInst, playerPixelsPerTile, out _playerMask, out _playerBuf, playerMaskProperty);
    }

    // 공용: 머티리얼 프로퍼티에 보드 크기 기반 Alpha8 텍스처 생성/연결
    void BuildMaskFor(Material targetMat, int ppu, out Texture2D tex, out Color32[] buf, string prop = "_MaskTex")
    {
        int w = Mathf.Max(1, board.width  * ppu);
        int h = Mathf.Max(1, board.height * ppu);

        tex = new Texture2D(w, h, TextureFormat.Alpha8, false, true);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        buf = new Color32[w * h];
        for (int i = 0; i < buf.Length; i++) buf[i] = new Color32(0,0,0,0);
        tex.SetPixels32(buf);
        tex.Apply(false, false);

        if (!targetMat)
        {
            Debug.LogWarning($"[PaintMask] targetMat null for {prop}");
            return;
        }
        if (!targetMat.HasProperty(prop))
        {
            Debug.LogWarning($"[PaintMask] Material {targetMat.name} has no property {prop}");
            return;
        }
        targetMat.SetTexture(prop, tex);
    }

    // ========== Stamp ==========

    // 텍스처 해상도(ppu) 기준으로 원형 스탬프(알파만 갱신)
    void StampCircle(Texture2D tex, Color32[] buf, int ppu, Vector3 centerW, float radiusW, byte alpha)
    {
        if (tex == null || buf == null || board == null) return;

        int TW = tex.width;
        int TH = tex.height;

        float tile = board.tileSize;
        Vector3 o = board.origin;

        float x01 = (centerW.x - o.x) / (board.width  * tile);
        float y01 = (centerW.z - o.z) / (board.height * tile);

        int cx = Mathf.RoundToInt(x01 * (TW - 1));
        int cy = Mathf.RoundToInt(y01 * (TH - 1));

        int r = Mathf.CeilToInt(radiusW * ppu / Mathf.Max(0.0001f, tile));
        int minX = Mathf.Max(0, cx - r);
        int maxX = Mathf.Min(TW - 1, cx + r);
        int minY = Mathf.Max(0, cy - r);
        int maxY = Mathf.Min(TH - 1, cy + r);

        float r2 = (r + 0.5f) * (r + 0.5f);
        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int row = y * TW;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy * dy > r2) continue;
                int idx = row + x;
                buf[idx].a = alpha; // 0=지우기, 255=칠하기
            }
        }
        tex.SetPixels32(buf);
    }

    // ========== Debug/Query (옵션) ==========
    public Texture2D ContamMaskTex => _contamMask;
    public Texture2D PlayerMaskTex => _playerMask;

    public bool IsContaminatedWorld(Vector3 worldPos)
    {
        if (_contamBuf == null || _contamMask == null) return false;
        int px, py;
        if (!WorldToPixel(worldPos, _contamMask.width, _contamMask.height, out px, out py)) return false;
        return _contamBuf[py * _contamMask.width + px].a > 0;
    }

    bool WorldToPixel(Vector3 wpos, int TW, int TH, out int px, out int py)
    {
        float tile = board.tileSize;
        Vector3 o = board.origin;
        float x01 = (wpos.x - o.x) / (board.width  * tile);
        float y01 = (wpos.z - o.z) / (board.height * tile);
        px = Mathf.RoundToInt(x01 * (TW - 1));
        py = Mathf.RoundToInt(y01 * (TH - 1));
        return (px >= 0 && px < TW && py >= 0 && py < TH);
    }

    void ClearAll(Texture2D tex, Color32[] buf)
    {
        if (tex == null || buf == null) return;
        for (int i = 0; i < buf.Length; i++) buf[i] = new Color32(0,0,0,0);
        tex.SetPixels32(buf);
        tex.Apply(false, false);
    }
}
