using UnityEngine;

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
    public float deadZone = 0.15f;

    [Header("Power Tuning")]
    public float launchBoost = 4f;   // ← 이동량 키우는 추가 배율

    [Header("Visual Scaling")]
    public float circleMin = 0.6f, circleMax = 2.0f;
    public float arrowMin  = 0.4f, arrowMax  = 3.5f;
    public float arrowBodyBaseZ = 1f;

    Plane ground;
    bool dragging;
    bool canDrag = true;               // ← 발사 중엔 잠금
    Vector3 startPos, currPos, dragDir; // dragDir = start→current
    float pull;

    void Start()
    {
        if (!cam) cam = Camera.main;
        ground = new Plane(Vector3.up, Vector3.zero);
        SetVis(false);

        // 멈추면 다시 드래그 허용
       
    }

    void Update()
    {

        // 시작
        if (Input.GetMouseButtonDown(0) && RayToGround(Input.mousePosition, out startPos))
        {
            dragging = true; SetVis(true); InitAt(startPos);
        }

        // 취소: 우클릭 또는 ESC
        if (dragging && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            dragging = false; SetVis(false);
            pull = 0f;                               // ← 리셋
            return;                                  // ← 발사 안 함
        }

        // 드래그 중
        if (dragging && Input.GetMouseButton(0) && RayToGround(Input.mousePosition, out currPos))
        {
            var delta = currPos - startPos; delta.y = 0f;
            float raw = delta.magnitude; if (raw < deadZone) raw = 0f;
            pull    = Mathf.Clamp(raw, 0f, maxPull);
            dragDir = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector3.forward;
            UpdateVis(pull / maxPull);
        }

        // 놓기(발사)
        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false; SetVis(false);
            if (pull >= minPull)
            {
                Vector3 launchDir = -dragDir;
                float strength = pull * launchBoost;
                canDrag = false;
                launcher.Launch(launchDir, strength);
            }
            pull = 0f;
        }
    }

    bool RayToGround(Vector3 sp, out Vector3 world)
    {
        var ray = cam.ScreenPointToRay(sp);
        if (ground.Raycast(ray, out float t)) { world = ray.GetPoint(t); return true; }
        world = default; return false;
    }

    void SetVis(bool on)
    {
        if (dragCircle) dragCircle.gameObject.SetActive(on);
        if (arrowRoot)  arrowRoot.gameObject.SetActive(on);
    }

    void InitAt(Vector3 pos)
    {
        float y = launcher.transform.position.y;
        if (dragCircle) dragCircle.position = new Vector3(pos.x, y, pos.z);
        if (arrowRoot)  arrowRoot.position  = new Vector3(pos.x, y, pos.z);
        UpdateVis(0f);
    }

    void UpdateVis(float t)
    {
        // 드래그량 기반 원 크기
        if (dragCircle)
        {
            float s = Mathf.Lerp(circleMin, circleMax, t);
            dragCircle.localScale = Vector3.one * s;
        }

        // 화살은 "발사 방향"을 가리키도록 -dragDir 사용
        if (arrowRoot)
        {
            Vector3 launchDir = (-dragDir).sqrMagnitude > 1e-6f ? -dragDir : Vector3.forward;
            arrowRoot.rotation = Quaternion.LookRotation(launchDir, Vector3.up);

            float len = Mathf.Lerp(arrowMin, arrowMax, t);
            if (arrowBody)
            {
                var s = arrowBody.localScale;
                float baseZ = Mathf.Abs(arrowBodyBaseZ) < 1e-6f ? 1f : arrowBodyBaseZ;
                arrowBody.localScale    = new Vector3(s.x, s.y, len / baseZ);
                arrowBody.localPosition = new Vector3(0, 0, (len * .5f) * .9f);
            }
            if (arrowHead) arrowHead.localPosition = new Vector3(0, 0, len);
        }
    }
}
