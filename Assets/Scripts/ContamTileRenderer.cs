// ContamTileRenderer.cs
using UnityEngine;
using System.Collections.Generic;

public class ContamTileRenderer : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Render")]
    public Mesh quadMesh;                 // XZ용 쿼드가 없으면 기본 Quad도 OK(아래 회전 토글)
    public Material contamMat;            // URP Unlit Transparent, Enable GPU Instancing 체크
    [Range(0f,1f)] public float alpha = 0.6f;

    [Header("Tuning")]
    public float yOffset = 0.2f;
    public bool useBaseColorAlpha = true;
    public bool meshIsXYQuad = true;      // 기본 Quad면 true, XZ용 쿼드(asset)면 false

    const int BATCH = 1023;
    readonly HashSet<Vector2Int> tiles = new();
    readonly List<Matrix4x4> matrices = new();
    bool dirty = true;

    void OnEnable()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        Subscribe(true);
        RebuildMatrices();
    }
    void OnDisable() => Subscribe(false);

    void Subscribe(bool on)
    {
        if (!director) return;
        if (on)
        {
            director.OnZonesReset             += HandleReset;
            director.OnZoneContaminatedCircle += HandleContamCircle;
            director.OnClearedCircleWorld     += HandleClearedCircle;
        }
        else
        {
            director.OnZonesReset             -= HandleReset;
            director.OnZoneContaminatedCircle -= HandleContamCircle;
            director.OnClearedCircleWorld     -= HandleClearedCircle;
        }
    }

    // ☆ 세트 리셋 때 '오염 타일'은 유지해야 함
    void HandleReset() { dirty = true; }

    void HandleContamCircle(int _, Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;
        float rTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);
        foreach (var t in director.CollectCircleTilesPublic(new Vector2Int(cx, cy), rTiles))
            tiles.Add(t);
        dirty = true;
        Debug.Log($"[Contam] +tiles, total={tiles.Count}");
    }

    void HandleClearedCircle(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;
        float rTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);
        foreach (var t in director.CollectCircleTilesPublic(new Vector2Int(cx, cy), rTiles))
            tiles.Remove(t);
        dirty = true;
        Debug.Log($"[Contam] -tiles, total={tiles.Count}");
    }

    void LateUpdate()
    {
        if (dirty) RebuildMatrices();
        if (!quadMesh || !contamMat || matrices.Count == 0) return;
        // ① 인스턴싱 강제 ON (머티리얼 체크박스 안 켜져 있을 때 대비)
        if (!contamMat.enableInstancing) contamMat.enableInstancing = true;

        // ② 컬링 Off (뒷면도 보이게) - URP Unlit/Lit 모두 _Cull 사용
        if (contamMat.HasProperty("_Cull"))
            contamMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        // 알파 바인딩
        if (useBaseColorAlpha && contamMat.HasProperty("_BaseColor"))
        { var c = contamMat.GetColor("_BaseColor"); c.a = alpha; contamMat.SetColor("_BaseColor", c); }
        else if (contamMat.HasProperty("_Color"))
        { var c = contamMat.GetColor("_Color"); c.a = alpha; contamMat.SetColor("_Color", c); }
        else if (contamMat.HasProperty("_Alpha"))
        { contamMat.SetFloat("_Alpha", alpha); }

        // 인스턴싱 드로우(머티리얼에서 Enable GPU Instancing 체크 필수)
        int i = 0;
        while (i < matrices.Count)
        {
            int count = Mathf.Min(BATCH, matrices.Count - i);
            Graphics.DrawMeshInstanced(quadMesh, 0, contamMat, matrices.GetRange(i, count));
            i += count;
        }
        
    }

    void RebuildMatrices()
    {
        matrices.Clear();
        if (!board) return;

        float s = board.tileSize;
        Vector3 o = board.origin;

        foreach (var t in tiles)
        {
            Vector3 c = new(o.x + (t.x + 0.5f) * s, o.y + yOffset, o.z + (t.y + 0.5f) * s);
            var rot = meshIsXYQuad ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
            var m = Matrix4x4.TRS(c, rot, new Vector3(s, 1f, s));
            matrices.Add(m);
        }
        dirty = false;
    }
}
