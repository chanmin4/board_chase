using UnityEngine;
using System.Collections.Generic;
using System;

public class ZoneVisualManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public SurvivalDirector director;

    [Header("Prefabs & Materials")]
    public GameObject hemispherePrefab;  // 반구(스피어 스케일로 반구처럼)
    public GameObject ringPrefab;        // 밑면 원(얇은 Cylinder 등)
    public Material smallMat, mediumMat, largeMat;
    public Material smallRingMat, mediumRingMat, largeRingMat;

    [Header("Contaminated (Disc)")]
    [NonSerialized] public GameObject contamDiscPrefab;  // 비어있으면 Cylinder로 생성
    public Material contamMat;           // 보라 반투명
    public float contamDiscY = 0.01f;    // 바닥과 간섭 방지

    // 돔 관리(id → 인스턴스)
    class Visual
    {
        public GameObject root;
        public Transform dome;      // 반구
        public Transform ring;      // 밑면 원(진행도에 따라 XZ 스케일)
        public float baseRadius;
    }
    Dictionary<int, Visual> map = new Dictionary<int, Visual>();

    Transform contamRoot; // 오염 디스크 부모

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        // 오염 디스크 용 루트
        contamRoot = new GameObject("ContaminatedDiscs").transform;
        contamRoot.SetParent(transform, false);

        if (director)
        {
            director.OnZonesReset += HandleReset;        // 돔/링만 정리
            director.OnZoneSpawned += HandleSpawn;
            director.OnZoneExpired += HandleExpired;
            director.OnZoneProgress += HandleProgress;
            director.OnZoneContaminatedCircle += HandleContamCircle; // ★ 원형 오염 유지
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

    void HandleConsumed(int id)
{
    // 소비는 '오염 없이' 돔만 제거 → 만료 처리와 동일하게 시각만 삭제
    HandleExpired(id);
}

    // === 돔/링 리셋(오염 디스크는 남김) ===
    void HandleReset()
    {
        foreach (var v in map.Values) if (v.root) Destroy(v.root);
        map.Clear();
        // contamRoot는 그대로 둔다 (오염 디스크 유지)
    }

    // === 돔/링 스폰 ===
    void HandleSpawn(ZoneSnapshot snap)
    {
        var root = new GameObject($"Zone_{snap.id}_{snap.kind}");
        root.transform.SetParent(transform, false);
        root.transform.position = snap.centerWorld;

        // 돔
        GameObject dome = Instantiate(hemispherePrefab, root.transform);
        dome.transform.localPosition = Vector3.zero;
        dome.transform.localScale = new Vector3(snap.baseRadius * 2f, snap.baseRadius, snap.baseRadius * 2f);
        StripAllColliders(dome); 

        var rend = dome.GetComponentInChildren<Renderer>();
        if (rend) rend.sharedMaterial = GetMat(snap.kind);

        // 밑면 원(링) — 시작은 반경 0
        GameObject ring = Instantiate(ringPrefab, root.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(0.0001f, 0.02f, 0.0001f);
        StripAllColliders(ring);   

        var rRend = ring.GetComponentInChildren<Renderer>();
        if (rRend) rRend.sharedMaterial = GetRingMat(snap.kind);

        map[snap.id] = new Visual
        {
            root = root,
            dome = dome.transform,
            ring = ring.transform,
            baseRadius = snap.baseRadius
        };
    }

    // === 돔 제거(만료) ===
    void HandleExpired(int id)
    {
        if (map.TryGetValue(id, out var v))
        {
            if (v.root) Destroy(v.root);
            map.Remove(id);
        }
    }

    // === 진행도에 따라 링 반경 확대 ===
    void HandleProgress(int id, float progress01)
    {
        if (!map.TryGetValue(id, out var v)) return;

        float r = Mathf.Lerp(0f, v.baseRadius, Mathf.Clamp01(progress01));
        v.ring.localScale = new Vector3(r * 2f, v.ring.localScale.y, r * 2f);
        // 필요하면 여기서 돔 투명도 보간 등 추가 가능
    }
    
    void StripAllColliders(GameObject go)
    {
        if (!go) return;
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) Destroy(c);  // 완전히 제거
        // 만약 제거 대신 트리거로 바꾸고 싶다면:
        // foreach (var c in cols) c.isTrigger = true;
    }

    // === 원형 오염 디스크 생성/유지 ===
    void HandleContamCircle(int id, Vector3 centerWorld, float radiusWorld)
    {
        GameObject go;
        if (contamDiscPrefab)
        {
            go = Instantiate(contamDiscPrefab, centerWorld + Vector3.up * contamDiscY, Quaternion.identity, contamRoot);
        }
        else
        {
            // 기본 Cylinder 생성
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(contamRoot, false);
            go.transform.position = centerWorld + Vector3.up * contamDiscY;
            // 콜라이더 제거
            var col = go.GetComponent<Collider>(); if (col) Destroy(col);
            StripAllColliders(go);   
        }

        // XZ = 지름, Y = 얇게
        go.transform.localScale = new Vector3(radiusWorld * 2f, 0.02f, radiusWorld * 2f);

        var r = go.GetComponentInChildren<Renderer>();
        if (r && contamMat) r.sharedMaterial = contamMat;
    }

    // ===== 머티리얼 선택 =====
    Material GetMat(SurvivalDirector.ZoneKind kind)
    {
        switch (kind)
        {
            case SurvivalDirector.ZoneKind.Small: return smallMat ? smallMat : mediumMat;
            case SurvivalDirector.ZoneKind.Medium: return mediumMat ? mediumMat : smallMat;
            default: return largeMat ? largeMat : mediumMat;
        }
    }

    Material GetRingMat(SurvivalDirector.ZoneKind kind)
    {
        switch (kind)
        {
            case SurvivalDirector.ZoneKind.Small: return smallRingMat ? smallRingMat : smallMat;
            case SurvivalDirector.ZoneKind.Medium: return mediumRingMat ? mediumRingMat : mediumMat;
            default: return largeRingMat ? largeRingMat : largeMat;
        }
    }
    

}
