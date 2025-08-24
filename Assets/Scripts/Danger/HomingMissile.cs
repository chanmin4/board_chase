using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public Transform previewParent;        // 비워두면 자동 생성
    public GameObject previewRingPrefab;   // 비워두면 Cylinder 자동 생성
    public Material previewRingMat;
    public string previewLayerName = "Ignore Raycast"; // 충돌 안 하는 레이어 권장

    [Header("Motion")]
    public float startHeight = 6f;
    public float groundY = 0.1f;
    public float fallDuration = 2.0f;

    [Header("Radius Growth (World)")]
    public float minRadius = 0.6f;
    public float maxRadius = 2.4f;
    public AnimationCurve growth = AnimationCurve.Linear(0,0, 1,1);
    float totalLifetime = 7f;

    [Header("Optional Homing")]
    public bool  followTargetXZ = false;  // 필요하면 켜기
    public Transform target;
    public float moveSpeed = 7.0f;        // 추적 속도

    Transform _tf;
    float _elapsed, _fallElapsed;
    Transform _ringTf;

    public void Setup(SurvivalDirector dir, float lifetimeSeconds)
    {
        director = dir;
        totalLifetime = Mathf.Max(0.1f, lifetimeSeconds);
        if (!target) target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Awake()
    {
        _tf = transform;

        // 시작 높이
        var p = _tf.position; p.y = startHeight; _tf.position = p;

        // 본체 및 자식에서 콜라이더/리지드바디 제거 + 레이어 설정
        MakePureVisual(gameObject);

        // 프리뷰 부모
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

    void Update()
    {
        float dt = Time.deltaTime;
        _elapsed     += dt;
        _fallElapsed += dt;

        // 하강
        float f = Mathf.Clamp01(_fallElapsed / Mathf.Max(0.0001f, fallDuration));
        float y = Mathf.Lerp(startHeight, groundY, f);

        // (옵션) XZ 추적
        Vector3 pos = _tf.position;
        if (followTargetXZ && target)
        {
            Vector3 t = target.position; t.y = pos.y;
            pos = Vector3.MoveTowards(pos, t, moveSpeed * dt);
        }

        _tf.position = new Vector3(pos.x, y, pos.z);

        // 반경 성장
        float t01   = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, totalLifetime));
        float k     = Mathf.Clamp01(growth.Evaluate(t01));
        float radius = Mathf.Lerp(minRadius, maxRadius, k);
        if (_ringTf) _ringTf.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
    }

    public void Explode()
    {
        float radius = _ringTf ? (_ringTf.localScale.x * 0.5f) : minRadius;

        // 프리뷰 잔재 제거
        if (previewParent) Destroy(previewParent.gameObject);

        // 오염 장판 생성(ZoneVisualManager가 contamMat 덮어씀)
        director?.ContaminateCircleWorld(transform.position, radius);

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
