using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public Transform previewParent;
    public GameObject previewRingPrefab;
    public Material previewRingMat;
    public string previewLayerName = "Ignore Raycast";

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
    public bool  followTargetXZ = false;   // 인스펙터 무시하고 코드에서 켭니다
    public Transform target;
    public float moveSpeed = 7.0f;

    Transform _tf;
    float _elapsed, _fallElapsed;
    Transform _ringTf;
    bool _configured = false;

    public void Setup(SurvivalDirector dir, float lifetimeSeconds)
    {
        director = dir;
        totalLifetime = Mathf.Max(0.1f, lifetimeSeconds);

        // ✅ 반드시 호밍 켜고 타겟 지정(우선순위: director.player > Player 태그)
        followTargetXZ = true;
        target = (dir && dir.player) ? dir.player
                 : GameObject.FindGameObjectWithTag("Player")?.transform;

        _configured = true;

        if (!target) Debug.LogWarning("[Missile] target is NULL. Check director.player or Player tag.");
        if (!director) Debug.LogWarning("[Missile] director is NULL. Explode() will do nothing.");

        InitVisuals();
    }

    void Awake()
    {
        _tf = transform;

        var p = _tf.position; p.y = startHeight; _tf.position = p;

        MakePureVisual(gameObject);

        if (!previewParent)
        {
            var go = new GameObject("Preview");
            go.layer = LayerMask.NameToLayer(previewLayerName);
            previewParent = go.transform;
            previewParent.SetParent(transform, false);
        }

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

    void InitVisuals() { /* 비워도 OK — Awake에서 이미 생성 */ }

    void Update()
    {
        if (!_configured) return;  // ← Setup 전에 아무것도 안함

        float dt = Time.deltaTime;
        _elapsed     += dt;
        _fallElapsed += dt;

        float f = Mathf.Clamp01(_fallElapsed / Mathf.Max(0.0001f, fallDuration));
        float y = Mathf.Lerp(startHeight, groundY, f);

        Vector3 pos = _tf.position;
        if (followTargetXZ && target)
        {
            Vector3 t = target.position; t.y = pos.y;
            pos = Vector3.MoveTowards(pos, t, moveSpeed * dt);
        }
        _tf.position = new Vector3(pos.x, y, pos.z);

        float t01   = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, totalLifetime));
        float k     = Mathf.Clamp01(growth.Evaluate(t01));
        float radius = Mathf.Lerp(minRadius, maxRadius, k);
        if (_ringTf) _ringTf.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
    }

    public void Explode()
    {
        if (!director) { Destroy(gameObject); return; }

        float radius = _ringTf ? (_ringTf.localScale.x * 0.5f) : minRadius;
        director.ContaminateCircleWorld(transform.position, radius);
        if (previewParent) Destroy(previewParent.gameObject);
        Destroy(gameObject);
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
