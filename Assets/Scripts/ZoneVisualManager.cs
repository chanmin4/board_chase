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

    class Visual
    {
        public GameObject root;
        public Transform dome;
        public Transform ring;
        public float baseRadius;
    }
    Dictionary<int, Visual> map = new Dictionary<int, Visual>();
    Transform contamRoot;

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
            director.OnZoneContaminatedCircle += HandleContamCircle;
            director.OnZoneConsumed += HandleConsumed;
        }
    }

    void OnDestroy()
    {
        if (!director) return;
        director.OnZonesReset -= HandleReset;
        director.OnZoneSpawned -= HandleSpawn;
        director.OnZoneExpired -= HandleExpired;
        director.OnZoneProgress -= HandleProgress;
        director.OnZoneContaminatedCircle -= HandleContamCircle;
        director.OnZoneConsumed -= HandleConsumed;
    }

    // 소비(성공 진입) → 돔/링 제거
    void HandleConsumed(int id) => HandleExpired(id);

    // 돔/링 전체 리셋(오염 디스크는 유지)
    void HandleReset()
    {
        foreach (var v in map.Values) if (v.root) Destroy(v.root);
        map.Clear();
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
        dome.transform.localScale    = new Vector3(snap.baseRadius * 2f, snap.baseRadius, snap.baseRadius * 2f);
        StripAllColliders(dome);

        var dRend = dome.GetComponentInChildren<Renderer>();
        if (dRend) dRend.sharedMaterial = snap.domeMat ? snap.domeMat : defaultDomeMat;

        // 링(초기 반경 0 → 진행도에 따라 확장)
        GameObject ring = Instantiate(ringPrefab, root.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale    = new Vector3(0.0001f, 0.02f, 0.0001f);
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
        if (!map.TryGetValue(id, out var v)) return;
        if (v.root) Destroy(v.root);
        map.Remove(id);
    }

    // 진행도에 따라 링 반경 보간(0 → baseRadius)
    void HandleProgress(int id, float progress01)
    {
        if (!map.TryGetValue(id, out var v)) return;
        float r = Mathf.Lerp(0f, v.baseRadius, Mathf.Clamp01(progress01));
        v.ring.localScale = new Vector3(r * 2f, v.ring.localScale.y, r * 2f);
        // 필요하면 돔 투명도/색 보간도 여기서 함께 처리 가능
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
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        StripAllColliders(go);
    }

    go.transform.localScale = new Vector3(radiusWorld * 2f, 0.02f, radiusWorld * 2f);

    //  모든 렌더러에 contamMat 강제 적용 (프리팹 머티 무시)
    if (contamMat)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.sharedMaterial = contamMat;
    }
}

    void StripAllColliders(GameObject go)
    {
        if (!go) return;
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) Destroy(c);
    }
}
