using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[AddComponentMenu("Board/Wall Fence By Scale")]
public class WallFenceByScale : MonoBehaviour
{
    [Header("Prefab & Spacing")]
    public GameObject fencePrefab;
    [Min(0.01f)] public float spacing = 5f; // “5마다 1개”
    public float endMargin = 0f;            // 양 끝에서 조금 물리기
    public float yOffsetWorld = 0f;         // 벽의 중심 Y에서 높이 보정
    public float yawOffsetLocal = 0f;       // 울타리 정면 보정(도)

    [Header("Axis")]
    public AxisMode axisMode = AxisMode.AutoLongestXZ; // 벽이 X/Z 중 어떤 축으로 길게 늘어났는지
    public enum AxisMode { AutoLongestXZ, ForceX, ForceZ }

    [Header("Group")]
    public string groupName = "_Fences";

    // ---- 수동 실행 ----
    [ContextMenu("Build On This Wall")]
    public void BuildOnThisWall()
    {
        if (!fencePrefab) return;

        // 1) parent/visual 가져온 다음, 축 기준도 visual로 통일
    Transform parent = transform;
    var vis = transform.Find("visual");
    if (vis) parent = vis;

    ClearInternal(parent);
    var group = new GameObject(groupName).transform;
    group.SetParent(parent, false);

    // [NEW] === 축/길이 계산 기준을 axisRef(= visual 있으면 그쪽)로 ===
    Transform axisRef = vis ? vis : transform;      // ★ 변경 포인트
    Vector3 ls = axisRef.lossyScale;

    // 2) 어느 축으로 늘어났는지 판단 + 진행 방향/길이 계산
    bool alongX =
        axisMode == AxisMode.ForceX ||
        (axisMode == AxisMode.AutoLongestXZ && Mathf.Abs(ls.x) >= Mathf.Abs(ls.z));

    Vector3 dir   = (alongX ? axisRef.right   : axisRef.forward).normalized;  // ★ 변경
    float   length=  alongX ? Mathf.Abs(ls.x) : Mathf.Abs(ls.z);               // ★ 변경

    float half  = 0.5f * length;
    float start = -half + endMargin;
    float end   =  half - endMargin;

    // 3) 회전도 축 기준으로
    Quaternion rot = Quaternion.AngleAxis(yawOffsetLocal, Vector3.up)
                * Quaternion.LookRotation(dir, Vector3.up);

    // 4) 위치도 axisRef 중심 기준으로
    float step = Mathf.Max(0.01f, spacing);
            for (float d = start; d <= end + 1e-4f; d += step)
            {
                Vector3 pos = axisRef.position + dir * d;   // ★ 변경
                pos.y = axisRef.position.y + yOffsetWorld;  // ★ 변경
                Instantiate(fencePrefab, pos, rot, group);
            }

    }

    [ContextMenu("Clear On This Wall")]
    public void ClearOnThisWall()
    {
        Transform parent = transform;
        var vis = transform.Find("visual");
        if (vis) parent = vis;
        ClearInternal(parent);
    }

    void ClearInternal(Transform parent)
    {
        if (!parent || string.IsNullOrEmpty(groupName)) return;
        var g = parent.Find(groupName);
#if UNITY_EDITOR
        if (g) { if (Application.isPlaying) Destroy(g.gameObject); else DestroyImmediate(g.gameObject); }
#else
        if (g) DestroyImmediate(g.gameObject);
#endif
    }
}
