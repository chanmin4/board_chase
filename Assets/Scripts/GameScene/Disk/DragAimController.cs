using UnityEngine;
using UnityEngine.EventSystems;
public enum VisualSanitizeLevel
{
    Noop,          // ← 아무것도 하지 않음 (원인검사용)
    ShadowsOnly,   // 그림자만 끔
    CollisionsOnly,// 콜라이더/리지드바디만 무력화
    FullSafe       // 위 둘 다 (머티리얼/레이어/큐는 건드리지 않음)
}
public class DragAimController : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public DiskLauncher launcher;
    public Transform dragCircle;
    public Transform arrowRoot, arrowBody, arrowHead;

    [Header("Pull Settings")]
    public float minPull = 0.5f;
    public float maxPull = 6f;
    public float deadZone = 0.20f;

    [Header("Power Tuning")]
    public float launchBoost = 6f;   // ← 이동량 키우는 추가 배율

    [Header("Visual Scaling")]
    public float circleMin = 0f, circleMax = 3.0f;
    public float arrowMin = 0f, arrowMax = 3.0f;
    public float arrowBodyBaseZ = 1f;
    [Header("Arrow Anchor at Disk Edge")]
    public bool stickToDisk = true;   // 디스크에 붙여서 따라가기
    public float diskRadius = 0.5f;   // 디스크(콜라이더) 반지름
    public float edgeGap = 0.1f;  // 원 바깥으로 살짝 띄우기
    [Header("Aim Visuals")]
    public float aimHeight = 0.12f; // 디스크 위로 살짝 띄우기
                                    // DragAimController.cs 상단 필드들 사이에 추가
    [Header("Integrations")]
    public SurvivalDirector director;                 // 인스펙터에서 연결(없으면 자동 탐색)
    public int DragCount=0;  //현재 리셋까지 드래그한횟수 
    public int resetDragNum = 2;          // 리셋 드래그 허용량


    [Header("Debug · Aim Visual Sanitize")]

public VisualSanitizeLevel sanitizeLevel = VisualSanitizeLevel.FullSafe;
    [Header("Debug")]
    public bool debugForceHideAim = false;
    float baseCircleDiameter = 1f;

    Plane ground;
    bool dragging;
    //bool canDrag = true;               // ← 발사 중엔 잠금
    Vector3 startPos, currPos, dragDir; // dragDir = start→current
    float pull;
    bool launchedThisDrag = false;

    public event System.Action DragPush;
    public event System.Action DragPull;


    void Start()
    {
        if (!cam) cam = Camera.main;
         if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        ground = new Plane(Vector3.up, new Vector3(0f, launcher.transform.position.y, 0f));
        SetVis(false);

        if (stickToDisk && transform.parent != launcher.transform)
            transform.SetParent(launcher.transform, true);  // 계층에서도 붙음

        if (diskRadius <= 0f)
        {
            var col = launcher.GetComponent<Collider>();
            if (col is SphereCollider sc) diskRadius = sc.radius * launcher.transform.lossyScale.x;
            else if (col is CapsuleCollider cc) diskRadius = cc.radius * launcher.transform.lossyScale.x;
            else if (col is BoxCollider bc) diskRadius = Mathf.Max(bc.size.x, bc.size.z) * 0.5f * launcher.transform.lossyScale.x;
        }

        // 드래그 에임 비물리화(간섭 방지)
        MakePureVisual(dragCircle);
        MakePureVisual(arrowRoot);
        baseCircleDiameter = ComputeRendererWidthWorld(dragCircle);
    if (baseCircleDiameter <= 0f) baseCircleDiameter = 1f;
    }

    void Update()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ───────────── 드래그 시작 ─────────────
        if (!dragging && Input.GetMouseButtonDown(0))
        {
            if (!overUI && RayToGround(Input.mousePosition, out startPos) && launcher.CanLaunchNow)
            {
                launchedThisDrag = false;
                dragging = true; SetVis(true); InitAt(startPos);
                DragPull?.Invoke(); 
            }
            // UI 위에서 누른 경우: 드래그 시작을 ‘무시’만 하고 그대로 종료
        }

        // 취소(우클릭/ESC)
        if (dragging && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            dragging = false; SetVis(false);
            pull = 0f;
            if (launcher) launcher.CancelAddCooldown(1f);
            DragPush?.Invoke();
            return;
        }

        // ───────────── 드래그 중 ─────────────
        if (dragging)
        {
            // 마우스 눌린 채 이동 업데이트: UI 위일 땐 위치 업데이트만 스킵 (놓기는 아래에서 처리)
            if (Input.GetMouseButton(0))
            {
                if (!overUI && RayToGround(Input.mousePosition, out currPos))
                {
                    var fromCenter = currPos - DiskCenter();
                    fromCenter.y = 0f;

                    float raw = fromCenter.magnitude;
                    float eff = Mathf.Max(0f, raw - deadZone);
                    pull = Mathf.Clamp(eff, 0f, maxPull);

                    if (raw > 1e-5f) dragDir = fromCenter.normalized;

                    UpdateVis(pull / maxPull);
                }
            }

            // 놓기: UI 위에서도 반드시 처리!
            if (Input.GetMouseButtonUp(0))
            {
                if (DragCount >=resetDragNum)
                {
                    //director?.ResetWallHits();
                }
                else
                {
                    DragCount++;
                }
                dragging = false; SetVis(false);
                DragPush?.Invoke();
                if (!launchedThisDrag && pull >= minPull)
                {
                    launchedThisDrag = true;
                    Vector3 launchDir = -dragDir;
                    float strength = pull * launchBoost;

                    var rb = launcher.GetComponent<Rigidbody>();
                    if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

                    launcher.Launch(launchDir, strength);
                }
                pull = 0f;
            }

            // 안전장치: 포커스 잃거나 버튼 상태가 꼬였을 때
            if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0) && !Input.GetMouseButtonDown(0))
            {
                // 버튼이 완전히 올라가 있는데 dragging이 남아있다면 시각만 접어둠
                // (여기서 바로 발사하지는 않음)
                if (dragCircle && dragCircle.gameObject.activeSelf) SetVis(false);
            }
        }
    }

    Vector3 DiskCenter() => launcher ? launcher.transform.position : Vector3.zero;

    // 디스크 가장자리 바깥 기준점
    Vector3 EdgeAnchor(Vector3 launchDir)
    {
        var center = DiskCenter();
        return center + launchDir.normalized * (diskRadius + edgeGap);
    }

    bool RayToGround(Vector3 sp, out Vector3 world)
    {
        var ray = cam.ScreenPointToRay(sp);
        if (ground.Raycast(ray, out float t)) { world = ray.GetPoint(t); return true; }
        world = default; return false;
    }
    // 스프라이트/메시 상관없이 폭(월드유닛)을 구함
    float ComputeRendererWidthWorld(Transform t) {
        if (!t) return 1f;
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr && sr.sprite) {
            // Sprite.bounds = 로컬(스프라이트) 기준, lossyScale로 월드 변환
            return sr.sprite.bounds.size.x * t.lossyScale.x;
        }
        var mr = t.GetComponent<MeshRenderer>();
        if (mr) {
            // bounds = 월드 기준. 로컬 스케일 제거해서 순수 메시 폭 추정
            return mr.bounds.size.x; // 이미 월드폭이므로 그대로
        }
        return 1f;
    }
    void SetVis(bool on)
    {
        if (dragCircle) dragCircle.gameObject.SetActive(on);
        if (arrowRoot) arrowRoot.gameObject.SetActive(on);
    }

    void InitAt(Vector3 _)
    {
        var center = DiskCenter();
        float y = center.y+aimHeight;  

        // 원(드래그 서클)은 디스크 중심에
        if (dragCircle) dragCircle.position = new Vector3(center.x, y, center.z);

        // 화살표는 디스크 가장자리 바깥에서 시작
        var launchDir = (-dragDir).sqrMagnitude > 1e-6f ? -dragDir : Vector3.forward;
        var basePos = EdgeAnchor(launchDir);
        if (arrowRoot) arrowRoot.position = new Vector3(basePos.x, y, basePos.z);

        UpdateVis(0f);
    }

    void UpdateVis(float t)
{
    // t = 0~1

    // 🔵 원(DragCircle): 목표 "반지름"을 화살표와 동일 단위로 사용
    float radius = Mathf.Lerp(circleMin, circleMax, t);
    if (dragCircle)
    {
        // 목표 지름(= 2 * 반지름) 을 현재 스프라이트 기본 지름에 맞춰 정규화
        float targetDiameter = Mathf.Max(0f, 2f * radius);
        float scaleFactor = (baseCircleDiameter > 0f) ? (targetDiameter / baseCircleDiameter) : 1f;

        // X/Y 동일 스케일, Z는 1 (스프라이트는 XY평면, X=90°로 눕혀둔 상태)
        dragCircle.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }

    if (!arrowRoot) return;

    // 🔺 화살표 세팅 (기존 동일)
    var launchDir = (-dragDir).sqrMagnitude > 1e-6f ? -dragDir : Vector3.forward;
    var basePos   = EdgeAnchor(launchDir);
    float y       = DiskCenter().y + aimHeight;

    arrowRoot.position = new Vector3(basePos.x, y, basePos.z);
    arrowRoot.rotation = Quaternion.LookRotation(launchDir, Vector3.up);

    bool show = t > 0.0001f;
    if (arrowBody) arrowBody.gameObject.SetActive(show);
    if (arrowHead) arrowHead.gameObject.SetActive(show);
    if (!show) return;

    // 🔺 화살표 길이도 "반지름"과 같은 수치 사용 → 비율 1:1
    float len = radius;
    if (arrowBody)
    {
        arrowBody.localScale    = new Vector3(0.1f, 0.1f, len);
        arrowBody.localPosition = new Vector3(0, 0, len * 0.5f);
    }
    if (arrowHead) arrowHead.localPosition = new Vector3(0, 0, len);
}

    void LateUpdate()
    {
        if (!launcher) return;

        // 드래그 중이든 아니든, 다음번 표시를 위해 기준점만 선반영
        var launchDir = (-dragDir).sqrMagnitude > 1e-6f ? -dragDir : Vector3.forward;

        if (dragCircle)
        {
            var c = DiskCenter();
            dragCircle.position = new Vector3(c.x, c.y+aimHeight, c.z);
        }
        if (arrowRoot)
        {
            var basePos = EdgeAnchor(launchDir);
            arrowRoot.position = new Vector3(basePos.x, DiskCenter().y+aimHeight, basePos.z);
        }
    }
void MakePureVisual(Transform root)
{
    if (!root) return;

    if (sanitizeLevel == VisualSanitizeLevel.Noop) return;

    if (sanitizeLevel == VisualSanitizeLevel.CollisionsOnly ||
        sanitizeLevel == VisualSanitizeLevel.FullSafe)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        { rb.detectCollisions = false; rb.isKinematic = true; rb.useGravity = false; }
    }

    if (sanitizeLevel == VisualSanitizeLevel.ShadowsOnly ||
        sanitizeLevel == VisualSanitizeLevel.FullSafe)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    // ⚠️ 여기서는 머티리얼(_ZWrite/_ZTest/renderQueue 등)과 레이어는 절대 건드리지 않음
}
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
    }

}
