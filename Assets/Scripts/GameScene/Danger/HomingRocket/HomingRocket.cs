using System;
using UnityEngine;

/// 미사일 시각/로직 프리팹. Awake에서는 "아무것도 생성하지 않음".
/// 반드시 MissileHazardSystem이 Instantiate 후 Setup()을 호출해야 동작함.
public class HomingRocket : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public Transform previewParent;        // 비우면 자동 생성
    public GameObject previewRingPrefab;   // 비우면 Cylinder 자동 생성
    public Material previewRingMat;
    public string previewLayerName = "Ignore Raycast"; // 충돌 안 하는 레이어 권장

    [Header("Motion")]
    public float startHeight = 6f;
    public float groundY = 0.1f;
    public float fallDuration = 2.0f;

    [Header("Radius Growth (World)")]
    public float minRadius = 0.6f;
    public float maxRadius = 2.4f;
    public AnimationCurve growth = AnimationCurve.Linear(0,0,1,1);
    float totalLifetime = 7f;

    [Header("Optional Homing")]
    [NonSerialized]public bool followTargetXZ = true; // 인스펙터 값은 무시되고 Setup에서 강제 설정됨
    public Transform target;
    public float moveSpeed = 7.0f;
    [Header("Sync / Ground Anchor")]
    public bool syncFallWithLifetime = true;   // fallDuration = totalLifetime 동기화
    public bool anchorRingToGround   = true;   // 반경 링을 항상 groundY에 붙임
    public bool explodeOnTouchesRing = true;
    // --- internal ---
    Transform _tf;
    Transform _ringTf;
    public Transform visualRoot;            // 로켓 모델 Transform(없으면 자동 탐색)
[Tooltip("visualRoot의 -up 방향으로 팁까지의 로컬 거리(미터).")]
public float tipOffset = 0.5f;          // 팁까지 오프셋(로컬, -up 기준)
[Tooltip("팁과 링의 XZ 접촉 허용 오차(미터).")]
public float tipContactMargin = 0.02f;  // 접촉 판정 여유
[Tooltip("팁이 바닥에 거의 닿았는지(Y) 허용 오차.")]
public float tipGroundMargin = 0.05f;   // 수직 허용 오차
    float _elapsed, _fallElapsed;
    bool _configured = false; // Setup을 받았는지

    // 호환 오버로드(기존 호출 유지용)
    public void Setup(SurvivalDirector dir, float lifetimeSeconds)
        => Setup(dir, lifetimeSeconds, null, -1f, true);

    // 메인 Setup
    public void Setup(
        SurvivalDirector dir,
        float lifetimeSeconds,
        Transform homingTarget,
        float homingSpeed,
        bool enableHoming)
    {
        director = dir;
        totalLifetime = Mathf.Max(0.1f, lifetimeSeconds);

        // 프리팹이 꺼져 저장되어 있어도 런타임 강제 ON
        enabled = true;

        // 타겟/추적 강제 설정
        followTargetXZ = enableHoming;
        if (enableHoming)
        {
            target = homingTarget
                     ?? (dir ? dir.player : null)
                     ?? GameObject.FindGameObjectWithTag("Player")?.transform;

            if (homingSpeed > 0f) moveSpeed = homingSpeed;
        }
        if (syncFallWithLifetime) fallDuration = totalLifetime;
         _tf.position = new Vector3(_tf.position.x, groundY + startHeight, _tf.position.z);
        _configured = true;

        // 비주얼 생성은 여기서만
        InitVisuals();
    }

    void Awake()
    {
        // ⚠️ 어떤 생성/세팅도 하지 말 것 (프리뷰, 링, 레이어 등 전부 금지)
        _tf = transform;
    }

    void Update()
    {
        if (!_configured) return;

        float dt = Time.deltaTime;
        _elapsed += dt;
        _fallElapsed += dt;

        // ── 하강(0→1)
        float f = Mathf.Clamp01(_fallElapsed / Mathf.Max(0.0001f, fallDuration));
        float y = Mathf.Lerp(startHeight, 0f, f); // startHeight에서 ground까지 상대값
        var pos = _tf.position;
        pos.y = groundY + y;

        // XZ 추적(기존)
        if (followTargetXZ && (!target && director)) target = director.player;
        if (followTargetXZ && target)
        {
            Vector3 t = target.position; t.y = pos.y;
            pos = Vector3.MoveTowards(pos, t, moveSpeed * dt);
        }
        _tf.position = pos;

        // 링은 항상 groundY에 고정
        if (anchorRingToGround && previewParent)
            previewParent.position = new Vector3(_tf.position.x, groundY + 0.001f, _tf.position.z);

        // ── 반경 성장 (0→1)
        float t01 = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, totalLifetime));
        float k = Mathf.Clamp01(growth.Evaluate(t01));
        float radius = Mathf.Lerp(minRadius, maxRadius, k);
        if (_ringTf) _ringTf.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        // ── NEW: 팁 접촉 판정
        if (explodeOnTouchesRing && visualRoot && previewParent)
        {
            // 팁의 월드 위치 (local -up 방향)
            float yScale = Mathf.Abs(visualRoot.lossyScale.y);
            Vector3 tip = visualRoot.position + (-visualRoot.up) * (tipOffset * yScale);

            // 바닥에 거의 닿았는지(수직 허용 오차)
            bool nearGround = Mathf.Abs(tip.y - groundY) <= tipGroundMargin;

            // 링 중심과의 XZ 거리
            Vector2 tipXZ = new Vector2(tip.x, tip.z);
            Vector2 ctrXZ = new Vector2(previewParent.position.x, previewParent.position.z);
            float dist = Vector2.Distance(tipXZ, ctrXZ);

            if (nearGround && dist <= radius + tipContactMargin)
            {
                Explode();
                return;
            }
        }
    }



    public void Explode()
    {
        if (!director) { Destroy(gameObject); return; }

        float radius = _ringTf ? (_ringTf.localScale.x * 0.5f) : minRadius;

        // 프리뷰 제거
        if (previewParent) Destroy(previewParent.gameObject);

        // 오염 생성
        director.ContaminateCircleWorld(transform.position, radius);

        Destroy(gameObject);
    }

    // ----- 내부 유틸 -----
    void InitVisuals()
    {
        // 시작 높이
        var p = _tf.position; p.y = startHeight; _tf.position = p;

        // 본체/자식 충돌 제거 + 레이어 지정
        MakePureVisual(gameObject);

        // 프리뷰 부모 생성
        if (!previewParent)
        {
            var go = new GameObject("Preview");
            go.layer = LayerMask.NameToLayer(previewLayerName);
            previewParent = go.transform;
            previewParent.SetParent(transform, false);
        }

        // 링 생성
        if (!previewRingPrefab)
        {
            previewRingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = previewRingPrefab.GetComponent<Collider>();
            if (col) Destroy(col);
        }

        var ring = Instantiate(previewRingPrefab, previewParent);
        ring.name = "RadiusRing";
        ring.layer = LayerMask.NameToLayer(previewLayerName);
        MakePureVisual(ring);

        _ringTf = ring.transform;
        _ringTf.localPosition = Vector3.zero;
        _ringTf.localRotation = Quaternion.identity;
        _ringTf.localScale    = new Vector3(minRadius * 2f, 0.02f, minRadius * 2f);

        var r = ring.GetComponentInChildren<Renderer>();
        if (r && previewRingMat) r.sharedMaterial = previewRingMat;
    }

    void MakePureVisual(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) Destroy(c);
        var rb = go.GetComponent<Rigidbody>();
        if (rb) Destroy(rb);

        int layer = LayerMask.NameToLayer(previewLayerName);
        SetLayerRecursively(go.transform, layer);
    }
    void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i), layer);
    }
}
