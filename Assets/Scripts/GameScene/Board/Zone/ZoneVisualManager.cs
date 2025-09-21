using UnityEngine;
using System.Collections.Generic;
using System;

public class ZoneVisualManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public SurvivalDirector director;

    [Header("Prefabs & Default Materials")]
    public GameObject hemispherePrefab;      // 반구 프리팹(스피어를 y 반만 보이게한 형태여도 OK)
    public GameObject ringPrefab;            // 밑면 원(얇은 Cylinder/Disc 등)
    public Material defaultDomeMat;
    public Material defaultRingMat;

    [Header("Contaminated (Disc Visual)")]
    public GameObject contamDiscPrefab;      // 없으면 Cylinder로 생성
    public Material contamMat;               // 보라/자주 반투명
    public float contamDiscY = 0.01f;        // 바닥과의 간섭 방지
    public bool makeContamDiscsTriggers = true;
    public string pollutionTag = "Pollution";
    public string pollutionLayer = "Pollution";

    // ★★ Bonus Arc (debug overlay) — 보너스 섹터를 원 둘레의 빨간 아크로 표시 ★★
    [Header("Bonus Arc (debug)")]
    public Material bonusArcMaterial;              // 단색 머티리얼(없으면 런타임 기본 생성)
    public Color    bonusArcColor = Color.red;
    [Range(8,128)] public int arcSegments = 32;    // 아크 세그먼트 수(매끄러움)
[Header("Bonus Arc Sizing")]
[Range(0f, 0.3f)] public float widthPerRadius = 0.06f; // 굵기 = baseRadius * 이값
public float minWidth = 0.04f;
public float maxWidth = 0.25f;

[Tooltip("아크를 링 밖으로 띄우는 양(반지름 비례). 최종 오프셋은 max(offsetPerRadius*r, arcRadiusOffset) + 굵기/2")]
[Range(0f, 0.2f)] public float offsetPerRadius = 0.04f;

[Tooltip("아크를 위로 띄우는 높이(반지름 비례 + 최소값)")]
[Range(0f, 0.5f)] public float yLiftPerRadius = 0.08f;
    public float minYLift = 0.10f;


    [Header("Legacy Contam Discs (off when using ContamTileRenderer)")]
    public bool useLegacyContamDiscs = false;

    class Visual
    {
        public GameObject root;
        public Transform dome;
        public Transform ring;
        public float baseRadius;
    }

    Dictionary<int, Visual> map = new Dictionary<int, Visual>();
    
    Transform contamRoot;

    // ★ 보너스 아크 라인: zoneId → LineRenderer
    readonly Dictionary<int, LineRenderer> bonusArcs = new();
    // (fallback용) 보너스 아크 머티리얼 캐시
    static Material _fallbackArcMat;

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        contamRoot = new GameObject("ContaminatedDiscs").transform;
        contamRoot.SetParent(transform, false);

        if (director)
        {
            director.OnZonesReset += HandleReset;
            director.OnZoneSpawned += HandleSpawn;
            director.OnZoneExpired += HandleExpired;
            director.OnZoneProgress += HandleProgress;
            director.OnZoneConsumed += HandleConsumed;

            // ★ 보너스 섹터 각도/아크 변경 알림 구독
            director.OnZoneBonusSectorChanged += HandleBonusSectorChanged;

            if (useLegacyContamDiscs)
            {
                director.OnZoneContaminatedCircle += HandleContamCircle;
                director.OnClearedCircleWorld += HandleClearedCircleWorld;
            }
        }
    }

    void OnDestroy()
    {
        if (!director) return;

        director.OnZonesReset -= HandleReset;
        director.OnZoneSpawned -= HandleSpawn;
        director.OnZoneExpired -= HandleExpired;
        director.OnZoneProgress -= HandleProgress;
        director.OnZoneConsumed -= HandleConsumed;

        // ★ 보너스 섹터 구독 해제
        director.OnZoneBonusSectorChanged -= HandleBonusSectorChanged;

        if (useLegacyContamDiscs)
        {
            director.OnZoneContaminatedCircle -= HandleContamCircle;
            director.OnClearedCircleWorld -= HandleClearedCircleWorld;
        }
    }

    // 소비(성공 진입) → 돔/링 제거
    void HandleConsumed(int id) => HandleExpired(id);

    // 돔/링 전체 리셋(오염 디스크는 유지)
    void HandleReset()
    {
        foreach (var v in map.Values) if (v.root) Destroy(v.root);
        map.Clear();

        // ★ 보너스 아크 정리
        foreach (var kv in bonusArcs)
            if (kv.Value) Destroy(kv.Value.gameObject);
        bonusArcs.Clear();
        // contamRoot는 그대로 유지 (지나간 세트 오염을 맵에 남김)
    }

    // 스폰: 스냅샷의 재질/반경 적용
    void HandleSpawn(ZoneSnapshot snap)
    {
        Debug.Log($"[ZoneVisual] Spawn id={snap.id} profile={snap.profileIndex} r={snap.baseRadius}");
        var root = new GameObject($"Zone_{snap.id}_P{snap.profileIndex}");
        root.transform.SetParent(transform, false);
        root.transform.position = snap.centerWorld;

        // 돔
        GameObject dome = Instantiate(hemispherePrefab, root.transform);
        dome.transform.localPosition = Vector3.zero;
        dome.transform.localScale = new Vector3(snap.baseRadius * 2f, snap.baseRadius, snap.baseRadius * 2f);
        StripAllColliders(dome);

        var dRend = dome.GetComponentInChildren<Renderer>();
        if (dRend) dRend.sharedMaterial = snap.domeMat ? snap.domeMat : defaultDomeMat;

        // 링(초기 반경 0 → 진행도에 따라 확장)
        GameObject ring = Instantiate(ringPrefab, root.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(0.0001f, 0.02f, 0.0001f);
        StripAllColliders(ring);

        var rRend = ring.GetComponentInChildren<Renderer>();
        if (rRend) rRend.sharedMaterial = snap.ringMat ? snap.ringMat : defaultRingMat;

        map[snap.id] = new Visual
        {
            root = root,
            dome = dome.transform,
            ring = ring.transform,
            baseRadius = snap.baseRadius
        };
    }

    // 만료(세트 종료로 오염 처리 후) → 해당 돔/링만 삭제
    void HandleExpired(int id)
    {
        if (map.TryGetValue(id, out var v))
        {
            if (v.root) Destroy(v.root);
            map.Remove(id);
        }

        // ★ 보너스 아크도 함께 제거
        if (bonusArcs.TryGetValue(id, out var lr) && lr)
            Destroy(lr.gameObject);
        bonusArcs.Remove(id);
    }

    // 진행도에 따라 링 반경 보간(0 → baseRadius)
    void HandleProgress(int id, float progress01)
    {
        if (!map.TryGetValue(id, out var v)) return;
        float r = Mathf.Lerp(0f, v.baseRadius, Mathf.Clamp01(progress01));
        v.ring.localScale = new Vector3(r * 2f, v.ring.localScale.y, r * 2f);
        // 필요하면 돔/링 색도 여기서 보간 가능
    }

    // === 보너스 섹터: 빨간 아크 그리기 ===
    void HandleBonusSectorChanged(int id, float angleDeg, float arcDeg)
    {
        if (!map.TryGetValue(id, out var v) || v == null) return;

        var lr = GetOrCreateArc(id, v);

        // ① "비주얼"에서 실제 반지름을 읽어온다.
        //    - dome(반구)의 X 스케일은 '지름'이므로 /2 → 반지름
        //    - ring(실린더)도 X 스케일이 지름이므로 동일하게 /2
        //    - 둘 다 없으면 baseRadius로 폴백
        float rVisual =
            (v.dome ? v.dome.localScale.x * 0.5f :
            (v.ring ? v.ring.localScale.x * 0.5f : v.baseRadius));

        // ② 굵기/오프셋/높이를 "반지름" 기준으로 산출
        float width = Mathf.Clamp(rVisual * widthPerRadius, minWidth, maxWidth);
        float rArc = rVisual + Mathf.Max(offsetPerRadius * rVisual, 0f) + width * 0.5f; // 링과 겹치지 않게 굵기/2 더함
        float yLocal = Mathf.Max(minYLift, yLiftPerRadius * rVisual); // 위로 살짝 띄우기(카메라/메시 간섭 방지)

        // ③ LineRenderer 세팅
        lr.useWorldSpace = false;         // 존 루트의 로컬에서 그린다
        lr.startWidth = lr.endWidth = width;
        lr.positionCount = Mathf.Max(8, arcSegments) + 1;

        float half = arcDeg * 0.5f;
        float a0 = Mathf.Deg2Rad * (angleDeg - half);
        float a1 = Mathf.Deg2Rad * (angleDeg + half);

        int N = lr.positionCount - 1;
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N;
            float a = Mathf.Lerp(a0, a1, t);
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rArc, yLocal, Mathf.Sin(a) * rArc));
        }

#if UNITY_EDITOR
        Debug.Log($"[BonusArc] id={id} rVis={rVisual:F2} rArc={rArc:F2} w={width:F2} y={yLocal:F2} a={angleDeg:F1}±{half:F1}");
#endif
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
    lr.textureMode = LineTextureMode.Stretch;

    var mat = bonusArcMaterial ? new Material(bonusArcMaterial) : GetFallbackArcMaterial();
    // 항상 위에 보이도록
    mat.renderQueue = 5000;
    mat.SetInt("_ZWrite", 0); // 일부 셰이더에서 동작 (없으면 무시)
    lr.material = mat;

    lr.startColor = lr.endColor = bonusArcColor;

    bonusArcs[id] = lr;
    return lr;
}

    static Material GetFallbackArcMaterial()
    {
        if (_fallbackArcMat) return _fallbackArcMat;
        // 파이프라인 상관없이 쓸 수 있는 단색 셰이더로 시도
        var sh = Shader.Find("Sprites/Default");    // 없으면 Unlit/Color로 재시도
        if (!sh) sh = Shader.Find("Unlit/Color");
         _fallbackArcMat = new Material(sh) { renderQueue = 5000 }; // Overlay
        return _fallbackArcMat;
    }

    // 오염 디스크(보라색) 생성
    void HandleContamCircle(int id, Vector3 centerWorld, float radiusWorld)
    {
        GameObject go;
        if (contamDiscPrefab)
        {
            go = Instantiate(contamDiscPrefab, centerWorld + Vector3.up * contamDiscY, Quaternion.identity, contamRoot);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(contamRoot, false);
            go.transform.position = centerWorld + Vector3.up * contamDiscY;
            var col = go.GetComponent<Collider>();
            if (!col) col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
        }

        go.transform.localScale = new Vector3(radiusWorld * 2f, 0.02f, radiusWorld * 2f);

        //  모든 렌더러에 contamMat 강제 적용 (프리팹 머티 무시)
        if (contamMat)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.sharedMaterial = contamMat;
        }
        if (makeContamDiscsTriggers)
        {
            int layer = LayerMask.NameToLayer(pollutionLayer);   // 인스펙터의 "Pollution" 레이어명
            SetTagLayerAndTriggerRecursively(go, pollutionTag, layer, true);
        }
    }

    void HandleClearedCircleWorld(Vector3 cW, float rW)
    {
        if (!contamRoot) return;
        // contamRoot 하위의 보라 디스크들을 훑어서,
        // '디스크 중심이 청소 원 안'이면 제거
        var toRemove = new List<Transform>();
        for (int i = 0; i < contamRoot.childCount; i++)
        {
            var t = contamRoot.GetChild(i);
            if (!t) continue;
            var p = t.position; p.y = cW.y;
            float discRadius = t.localScale.x * 0.5f; // 생성 시 x=지름으로 잡아둔 경우
            float d = Vector3.Distance(p, cW);
            if (d <= rW + 0.01f)
            {
                toRemove.Add(t);
            }
        }
        foreach (var t in toRemove) Destroy(t.gameObject);
    }

    void StripAllColliders(GameObject go)
    {
        if (!go) return;
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) Destroy(c);
    }

    static void SetTagLayerAndTriggerRecursively(GameObject go, string tag, int layer, bool makeTrigger)
    {
        if (!go) return;

        // 태그가 프로젝트에 없으면 예외가 나서 방어
        bool canTag = !string.IsNullOrEmpty(tag);
        if (canTag)
        {
            try { _ = UnityEngine.EventSystems.EventSystem.current; /* no-op */ }
            catch { canTag = false; }
        }

        var transforms = go.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (canTag) t.gameObject.tag = tag;
            if (layer >= 0) t.gameObject.layer = layer;

            if (makeTrigger)
            {
                var col = t.GetComponent<Collider>();
                if (col) col.isTrigger = true;
            }
        }
    }
}
