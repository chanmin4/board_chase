using UnityEngine;
using System.Collections.Generic;

public class ZoneVisualManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public SurvivalDirector director;

    [Header("Prefabs & Default Materials")]
    public GameObject hemispherePrefab;      // 돔 프리팹
    public GameObject ringPrefab;            // 밑면 원(얇은 Cylinder/Disc)
    public Material defaultDomeMat;
    public Material defaultRingMat;

    [Header("Contaminated (Disc Visual)")]
    public GameObject contamDiscPrefab;      // 없으면 Cylinder 생성
    public Material contamMat;
    public float contamDiscY = 0.01f;
    public bool  makeContamDiscsTriggers = true;
    public string pollutionTag   = "Pollution";
    public string pollutionLayer = "Pollution";

    // ---------- Bonus Arc (debug) ----------
    [Header("Bonus Arc (debug)")]
    public Material bonusArcMaterial;              // 단색 머티리얼(없으면 내부 fallback)
    public Color    bonusArcColor = Color.red;
    [Range(8,128)] public int arcSegments = 32;    // 세그먼트 수
    public float    arcRadiusOffset = 0.02f;       // 링 밖 오프셋

    [Header("Bonus Arc Sizing")]
    public bool  widthScalesWithRadius = true;     // 반지름 비례 굵기
    [Range(0f,0.3f)] public float widthPerRadius = 0.06f; // 굵기 = baseRadius * 값
    public float minWidth = 0.04f;
    public float maxWidth = 0.25f;
    [Tooltip("아크를 링 밖으로 띄우는 양(반지름 비례). 최종 오프셋은 max(offsetPerRadius*r, arcRadiusOffset) + 굵기/2")]
    [Range(0f,0.2f)] public float offsetPerRadius = 0.04f;
    [Tooltip("아크를 위로 띄우는 높이(반지름 비례 + 최소값)")]
    [Range(0f,0.5f)] public float yLiftPerRadius = 0.08f;
    public float minYLift = 0.10f;

    // ---------- Mini Timer (Screen UI) ----------
    [Header("Mini Timer (Screen UI)")]
    public Canvas screenCanvas;              // Screen Space Overlay/Camera
    public GameObject miniTimerPrefab;       // ZoneMiniTimerFollower가 붙은/붙을 프리팹

    [Header("Legacy Contam Discs (off when using ContamTileRenderer)")]
    public bool useLegacyContamDiscs = false;

    class Visual
    {
        public GameObject root;
        public Transform dome;
        public Transform ring;
        public float baseRadius;
    }

    Dictionary<int, Visual> map = new();
    Transform contamRoot;

    // Bonus Arc
    readonly Dictionary<int, LineRenderer> bonusArcs = new();
    static Material _fallbackArcMat;

    // Mini Timers (screen UI)
    readonly Dictionary<int, ZoneMiniTimerFollower> miniTimers = new();

    void Awake()
    {
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        contamRoot = new GameObject("ContaminatedDiscs").transform;
        contamRoot.SetParent(transform, false);

        if (!director) return;

        director.OnZonesReset     += HandleReset;
        director.OnZoneSpawned    += HandleSpawn;
        director.OnZoneExpired    += HandleExpired;
        director.OnZoneConsumed   += HandleConsumed;
        director.OnZoneProgress   += HandleProgress;
        director.OnZoneBonusSectorChanged += HandleBonusSectorChanged;


    if (!screenCanvas)
        screenCanvas = FindFirstObjectByType<Canvas>();

        if (useLegacyContamDiscs)
        {
            director.OnZoneContaminatedCircle += HandleContamCircle;
            director.OnClearedCircleWorld += HandleClearedCircleWorld;
        }
    }

    void OnDestroy()
    {
        if (!director) return;

        director.OnZonesReset     -= HandleReset;
        director.OnZoneSpawned    -= HandleSpawn;
        director.OnZoneExpired    -= HandleExpired;
        director.OnZoneConsumed   -= HandleConsumed;
        director.OnZoneProgress   -= HandleProgress;
        director.OnZoneBonusSectorChanged -= HandleBonusSectorChanged;

        if (useLegacyContamDiscs)
        {
            director.OnZoneContaminatedCircle -= HandleContamCircle;
            director.OnClearedCircleWorld     -= HandleClearedCircleWorld;
        }
    }

    // ---- 소비 = 시각물 제거 동일 처리 ----
    void HandleConsumed(int id) => HandleExpired(id);

    // ---- 전체 리셋 ----
    void HandleReset()
    {
        foreach (var v in map.Values) if (v.root) Destroy(v.root);
        map.Clear();

        foreach (var kv in bonusArcs) if (kv.Value) Destroy(kv.Value.gameObject);
        bonusArcs.Clear();

        foreach (var kv in miniTimers) if (kv.Value) Destroy(kv.Value.gameObject);
        miniTimers.Clear();
    }

    // ---- 스폰 ----
    void HandleSpawn(ZoneSnapshot snap)
    {
        var root = new GameObject($"Zone_{snap.id}_P{snap.profileIndex}");
        root.transform.SetParent(transform, false);
        root.transform.position = snap.centerWorld;

        // 돔
        GameObject dome = Instantiate(hemispherePrefab, root.transform);
        dome.transform.localPosition = Vector3.zero;
        dome.transform.localScale    = new Vector3(snap.baseRadius * 2f, snap.baseRadius, snap.baseRadius * 2f);
        StripAllColliders(dome);
        var dRend = dome.GetComponentInChildren<Renderer>();
        if (dRend) dRend.sharedMaterial = snap.domeMat ? snap.domeMat : defaultDomeMat;
        map[snap.id] = new Visual
        {
            root = root,
            dome = dome.transform,
            baseRadius = snap.baseRadius
        };
        if (ringPrefab)
        {
            // 링(초기 0 → 진행도에 따라 확장)
            GameObject ring = Instantiate(ringPrefab, root.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = new Vector3(0.0001f, 0.02f, 0.0001f);
            StripAllColliders(ring);
            var rRend = ring.GetComponentInChildren<Renderer>();
            if (rRend) rRend.sharedMaterial = snap.ringMat ? snap.ringMat : defaultRingMat;
            map[snap.id].ring = ring.transform;
        }

        // 미니 타이머(HUD)
            if (screenCanvas && miniTimerPrefab)
            {
                var ui = Instantiate(miniTimerPrefab, screenCanvas.transform);
                ui.transform.SetAsFirstSibling();
                var follower = ui.GetComponent<ZoneMiniTimerFollower>();
                if (!follower) follower = ui.AddComponent<ZoneMiniTimerFollower>();
                follower.Setup(screenCanvas, snap.centerWorld, snap.baseRadius,
                               Mathf.Max(0.01f, snap.time_to_live));
                follower.SetRemain(snap.remain);
                miniTimers[snap.id] = follower;
            }
    }

    // ---- 만료/소멸 ----
    void HandleExpired(int id)
    {
        if (map.TryGetValue(id, out var v))
        {
            if (v.root) Destroy(v.root);
            map.Remove(id);
        }
        if (bonusArcs.TryGetValue(id, out var lr) && lr)
            Destroy(lr.gameObject);
        bonusArcs.Remove(id);

        if (miniTimers.TryGetValue(id, out var mt) && mt)
            Destroy(mt.gameObject);
        miniTimers.Remove(id);
    }

    // ---- 진행도(0→1 경과비율) ----
    void HandleProgress(int id, float progress01)
    {
        if (map.TryGetValue(id, out var v))
        {
            float r = Mathf.Lerp(0f, v.baseRadius, Mathf.Clamp01(progress01));
            if(v.ring!=null)v.ring.localScale = new Vector3(r * 2f, v.ring.localScale.y, r * 2f);
        }

        // HUD 도넛: 남은 시간 갱신
        if (miniTimers.TryGetValue(id, out var mt) && mt)
        {
            float remain = Mathf.Max(0f, (1f - Mathf.Clamp01(progress01)) * mt.TtlInit);
            mt.SetRemain(remain);
        }
    }

    // ---- 보너스 섹터 아크 ----
    void HandleBonusSectorChanged(int id, float angleDeg, float arcDeg)
    {
        if (!map.TryGetValue(id, out var v) || v == null) return;

        var lr = GetOrCreateArc(id, v);

        int N = Mathf.Max(8, arcSegments);
        lr.positionCount = N + 1;

        // 굵기/오프셋/높이: 반지름 비례
        float w = widthScalesWithRadius ? Mathf.Clamp(v.baseRadius * widthPerRadius, minWidth, maxWidth) : minWidth;
        lr.startWidth = lr.endWidth = w;

        float extra = Mathf.Max(arcRadiusOffset, offsetPerRadius * v.baseRadius) + w * 0.5f;
        float radius = Mathf.Max(0.01f, v.baseRadius + extra);

        float y = Mathf.Max(minYLift, v.baseRadius * yLiftPerRadius);

        float half = arcDeg * 0.5f;
        float a0 = Mathf.Deg2Rad * (angleDeg - half);
        float a1 = Mathf.Deg2Rad * (angleDeg + half);

        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N;
            float a = Mathf.Lerp(a0, a1, t);
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
        }
    }

    LineRenderer GetOrCreateArc(int id, Visual v)
    {
        if (bonusArcs.TryGetValue(id, out var lr) && lr) return lr;

        var go = new GameObject($"BonusArc_{id}");
        go.transform.SetParent(v.root.transform, false);
        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;

        var mat = bonusArcMaterial ? new Material(bonusArcMaterial) : GetFallbackArcMaterial();
        mat.renderQueue = 5000; // Overlay
        lr.material = mat;
        lr.startColor = lr.endColor = bonusArcColor;

        bonusArcs[id] = lr;
        return lr;
    }

    static Material GetFallbackArcMaterial()
    {
        if (_fallbackArcMat) return _fallbackArcMat;
        var sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Unlit/Color");
        _fallbackArcMat = new Material(sh) { renderQueue = 5000 };
        return _fallbackArcMat;
    }

    // ---- 오염 디스크(보라) ----
    void HandleContamCircle(int id, Vector3 centerWorld, float radiusWorld)
    {
        GameObject go;
        if (contamDiscPrefab)
            go = Instantiate(contamDiscPrefab, centerWorld + Vector3.up * contamDiscY, Quaternion.identity, contamRoot);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(contamRoot, false);
            go.transform.position = centerWorld + Vector3.up * contamDiscY;
            var col = go.GetComponent<Collider>(); if (!col) col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
        }
        go.transform.localScale = new Vector3(radiusWorld * 2f, 0.02f, radiusWorld * 2f);

        if (contamMat)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = contamMat;

        if (makeContamDiscsTriggers)
        {
            int layer = LayerMask.NameToLayer(pollutionLayer);
            SetTagLayerAndTriggerRecursively(go, pollutionTag, layer, true);
        }
    }

    void HandleClearedCircleWorld(Vector3 cW, float rW)
    {
        if (!contamRoot) return;
        var toRemove = new List<Transform>();
        for (int i = 0; i < contamRoot.childCount; i++)
        {
            var t = contamRoot.GetChild(i);
            if (!t) continue;
            var p = t.position; p.y = cW.y;
            float discRadius = t.localScale.x * 0.5f;
            float d = Vector3.Distance(p, cW);
            if (d <= rW + 0.01f) toRemove.Add(t);
        }
        foreach (var t in toRemove) Destroy(t.gameObject);
    }

    // ---- 공용 유틸 ----
    void StripAllColliders(GameObject go)
    {
        if (!go) return;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
    }

    static void SetTagLayerAndTriggerRecursively(GameObject go, string tag, int layer, bool makeTrigger)
    {
        if (!go) return;
        var transforms = go.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (!string.IsNullOrEmpty(tag)) t.gameObject.tag = tag;
            if (layer >= 0) t.gameObject.layer = layer;
            if (makeTrigger)
            {
                var col = t.GetComponent<Collider>();
                if (col) col.isTrigger = true;
            }
        }
    }
}
