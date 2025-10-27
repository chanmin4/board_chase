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
    public bool makeContamDiscsTriggers = true;
    public string pollutionTag = "Pollution";
    public string pollutionLayer = "Pollution";

    // ---------- Bonus Arc (debug) ----------
    [Header("Bonus Arc (debug)")]
    public Material bonusArcMaterial;              // 단색 머티리얼(없으면 내부 fallback)
    public Color bonusArcColor = Color.red;
    [Range(8, 128)] public int arcSegments = 32;    // 세그먼트 수
    public float arcRadiusOffset = 0.02f;       // 링 밖 오프셋

    [Header("Bonus Arc Sizing")]
    public bool widthScalesWithRadius = true;     // 반지름 비례 굵기
    [Range(0f, 0.3f)] public float widthPerRadius = 0.06f; // 굵기 = baseRadius * 값
    public float minWidth = 0.04f;
    public float maxWidth = 0.25f;
    [Tooltip("아크를 링 밖으로 띄우는 양(반지름 비례). 최종 오프셋은 max(offsetPerRadius*r, arcRadiusOffset) + 굵기/2")]
    [Range(0f, 0.2f)] public float offsetPerRadius = 0.04f;
    [Tooltip("아크를 위로 띄우는 높이(반지름 비례 + 최소값)")]
    [Range(0f, 0.5f)] public float yLiftPerRadius = 0.08f;
    public float minYLift = 0.10f;

    // ---------- Mini Timer (Screen UI) ----------
    [Header("Mini Timer (Screen UI)")]
    public Canvas screenCanvas;              // Screen Space Overlay/Camera
    public GameObject miniTimerPrefab;       // ZoneMiniTimerFollower가 붙은/붙을 프리팹

    // [MOD] ---------- Mini Timer (World-Attached) ----------
    [Header("Mini Timer Attach Mode")] // [MOD]
    public bool attachTimersToZone = true;  // [MOD] true면 Zone 밑(월드)에 부착, false면 기존처럼 화면 캔버스에 생성

    [Header("Legacy Contam Discs (off when using ContamTileRenderer)")]
    //public bool useLegacyContamDiscs = false;
    [Header("Auto Fit (match prefab size to world radius)")]
    public bool autoFitPrefabs = true;              // 프리팹 실제 치수에 맞춰 자동 스케일
    public float ringHeightWorld = 0.02f;           // 링 두께(월드)
    [Header("Layering / Collision")]
    public bool setZoneVisualsToWallLayer = false;   // 켜면 시각물 레이어를 Wall로
    public string wallLayerName = "Wall";
    public bool keepZoneVisualColliders = false;     // 콜라이더 제거하지 않기

    // === [추가] Visual 저장 구조에 필드 2개 추가 ===
    // class/struct Visual 안에 아래 두 줄 추가
    public float ringScalePerRadius; // XZ: scale = radius * ringScalePerRadius
    public float ringScaleY;         // Y 두께 스케일(월드 두께 고정용)


    class Visual
    {
        public GameObject root;
        public Transform dome;
        public Transform ring;
        public float baseRadius;
        public float ringScalePerRadius; // XZ: scaleXZ = radius * ringScalePerRadius
        public float ringScaleY;
    }

    Dictionary<int, Visual> map = new();
    Transform contamRoot;

    // Bonus Arc
    readonly Dictionary<int, LineRenderer> bonusArcs = new();
    static Material _fallbackArcMat;

    // Mini Timers
    readonly Dictionary<int, ZoneMiniTimerFollower> miniTimers = new();

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        contamRoot = new GameObject("ContaminatedDiscs").transform;
        contamRoot.SetParent(transform, false);

        if (!director) return;

        director.OnZonesReset += HandleReset;
        director.OnZoneSpawned += HandleSpawn;
        director.OnZoneExpired += HandleExpired;
        director.OnZoneConsumed += HandleConsumed;
        director.OnZoneProgress += HandleProgress;
        director.OnZoneBonusSectorChanged += HandleBonusSectorChanged;

        if (!screenCanvas)
            screenCanvas = FindFirstObjectByType<Canvas>();

        //if (useLegacyContamDiscs)
        {
            //director.OnZoneContaminatedCircle += HandleContamCircle;
            //irector.OnClearedCircleWorld += HandleClearedCircleWorld;
        }
    }

    void OnDestroy()
    {
        if (!director) return;

        director.OnZonesReset -= HandleReset;
        director.OnZoneSpawned -= HandleSpawn;
        director.OnZoneExpired -= HandleExpired;
        director.OnZoneConsumed -= HandleConsumed;
        director.OnZoneProgress -= HandleProgress;
        director.OnZoneBonusSectorChanged -= HandleBonusSectorChanged;

        // if (useLegacyContamDiscs)
        {
            // director.OnZoneContaminatedCircle -= HandleContamCircle;
            //director.OnClearedCircleWorld     -= HandleClearedCircleWorld;
        }
    }

    // ---- 소비 = 시각물 제거 동일 처리 ----
    void HandleConsumed(int id)
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
        root.transform.SetParent(transform, worldPositionStays: true);
        root.transform.position = new Vector3(snap.centerWorld.x, board.origin.y, snap.centerWorld.z);

        // 돔
        GameObject dome = Instantiate(hemispherePrefab, root.transform, false);
        if (!keepZoneVisualColliders) StripAllColliders(dome);              // ★ 유지 옵션
        if (setZoneVisualsToWallLayer)
            SetLayerRecursively(dome, LayerMask.NameToLayer(wallLayerName)); // ★ 레이어 재귀 적용
        var dRend = dome.GetComponentInChildren<Renderer>();
        if (dRend) dRend.sharedMaterial = snap.domeMat ? snap.domeMat : defaultDomeMat;

        if (autoFitPrefabs && CalcLocalBounds(dome.transform, out var dB))
        {
            float diaLocal = Mathf.Max(0.0001f, Mathf.Max(dB.size.x, dB.size.z));
            float s = (snap.baseRadius * 2f) / diaLocal;           // 지름 맞추기
            dome.transform.localScale = Vector3.one * s;

            float bottomLocal = dB.center.y - dB.size.y * 0.5f;    // 바닥 붙이기
            dome.transform.localPosition = new Vector3(0f, -bottomLocal * s, 0f);
        }
        else
        {
            // 프리팹이 단위 1이라 가정할 때의 기존 스케일(지름=2R, 높이=R)
            dome.transform.localScale = new Vector3(snap.baseRadius * 2f, snap.baseRadius, snap.baseRadius * 2f);
        }


        var v = new Visual
        {
            root = root,
            dome = dome.transform,
            baseRadius = snap.baseRadius
        };

        if (ringPrefab)
        {
            GameObject ring = Instantiate(ringPrefab, root.transform, false);
        if (!keepZoneVisualColliders) StripAllColliders(ring);               // ★ 유지 옵션
        if (setZoneVisualsToWallLayer)
            SetLayerRecursively(ring, LayerMask.NameToLayer(wallLayerName));  // ★ 레이어 재귀 적용

            var rRend = ring.GetComponentInChildren<Renderer>();
            if (rRend) rRend.sharedMaterial = snap.ringMat ? snap.ringMat : defaultRingMat;

            float ringLocalY = 0f;
            float scalePerRadius = 2f;                  // 단위 1 가정 기본값
            float yScale = ringHeightWorld;             // 두께(월드)

            if (autoFitPrefabs && CalcLocalBounds(ring.transform, out var rB))
            {
                float diaLocal = Mathf.Max(0.0001f, Mathf.Max(rB.size.x, rB.size.z));
                scalePerRadius = 2f / diaLocal;                             // scaleXZ = radius * 이값
                yScale = ringHeightWorld / Mathf.Max(0.0001f, rB.size.y);

                float bottomLocal = rB.center.y - rB.size.y * 0.5f;         // 바닥 붙이기
                ringLocalY = -bottomLocal * yScale;
            }

            // 초기(진행도 0) 상태 적용
            ring.transform.localPosition = new Vector3(0f, ringLocalY, 0f);
            ring.transform.localScale = new Vector3(0.0001f, yScale, 0.0001f);

            // 저장
            v.ring = ring.transform;
            v.ringScalePerRadius = scalePerRadius;
            v.ringScaleY = yScale;
        }

        // 여기서 한 번에 map에 넣기(중간에 덜 채워진 상태로 쓰지 않게)
        map[snap.id] = v;

        // ---- 미니 타이머 ----
        if (miniTimerPrefab)
        {
            if (!attachTimersToZone && screenCanvas) // 화면 캔버스에 생성(디스크 무시)
            {
                var ui = Instantiate(miniTimerPrefab, screenCanvas.transform);
                ui.transform.SetAsFirstSibling();

                var f = ui.GetComponent<ZoneMiniTimerFollower>();
                if (!f) f = ui.AddComponent<ZoneMiniTimerFollower>();

                // ★ 존 좌표로 고정(디스크 참조 없음)
                f.Setup(screenCanvas, snap.centerWorld, snap.baseRadius,
                        Mathf.Max(0.01f, snap.time_to_live));
                f.SetRemain(snap.remain);

                miniTimers[snap.id] = f;
            }
            else // [MOD] Zone 밑(월드)에 직부착
            {
                var ui = Instantiate(miniTimerPrefab, root.transform);
                ui.transform.localPosition = Vector3.zero;

                var f = ui.GetComponent<ZoneMiniTimerFollower>() ?? ui.AddComponent<ZoneMiniTimerFollower>();
                f.SetupWorld(root.transform, snap.baseRadius,
                             Mathf.Max(0.01f, snap.time_to_live), 45f);
                f.SetRemain(snap.remain);

                miniTimers[snap.id] = f;
            }
        }

    }
    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (!go) return;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
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
            float r = Mathf.Lerp(0f, v.baseRadius, Mathf.Clamp01(progress01));  // 목표 반지름(월드)
            if (v.ring)
            {
                float sxz = Mathf.Max(0f, r) * Mathf.Max(0.0001f, v.ringScalePerRadius);
                v.ring.localScale = new Vector3(sxz, v.ring.localScale.y, sxz);
            }
        }

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

    static bool CalcLocalBounds(Transform root, out Bounds b)
    {
        b = new Bounds(Vector3.zero, Vector3.zero);
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return false;

        // 첫 렌더러로 초기화
        var rb = rends[0].bounds;
        var min = root.InverseTransformPoint(rb.min);
        var max = root.InverseTransformPoint(rb.max);
        b = new Bounds((min + max) * 0.5f, max - min);

        for (int i = 1; i < rends.Length; i++)
        {
            rb = rends[i].bounds;
            min = root.InverseTransformPoint(rb.min);
            max = root.InverseTransformPoint(rb.max);
            b.Encapsulate(min);
            b.Encapsulate(max);
        }
        return true;
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
    /*
    void HandleContamCircle(int id, Vector3 centerWorld, float radiusWorld)
    {
        float yBase = board ? board.origin.y : centerWorld.y;
        Vector3 pos = new Vector3(centerWorld.x, yBase + contamDiscY, centerWorld.z);

        GameObject go;
        if (contamDiscPrefab)
            go = Instantiate(contamDiscPrefab, pos, Quaternion.identity, contamRoot);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(contamRoot, false);
            go.transform.position = pos;
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
*/
    // ---- 공용 유틸 ----
    void StripAllColliders(GameObject go)
    {
        if (!go) return;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
    }
/*
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
    */
}
