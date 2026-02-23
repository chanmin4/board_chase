using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[AddComponentMenu("Board/Fence Strip (10-step, simple)")]
public class FenceStripEvery10 : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid grid;        // origin/크기
    public Transform wall;        // 이 벽 기준(Left/Right/Front/Back 중 하나)

    [Header("Fence")]
    public GameObject fencePrefab;

    //[Header("Side Mode (필수 선택)")]
    public enum SideGroup { LeftRight, FrontBack }
    public SideGroup sideGroup = SideGroup.LeftRight;

    [Header("Params")]
    public float unit = 10f;      // 간격
    public float fenceScaleX = 5f; // 인스턴스 scale.x

    [Header("Group")]
    public string groupName = "_Fences";

    [ContextMenu("Build Fences")]
    public void BuildFences()
    {
        if (!grid || !fencePrefab || !wall) return;

        // 보드 내부 사각형 (월드 좌표)
        Rect br = grid.GetBoardRectXZ();
        float y  = grid.origin.y;

        // 어느 쪽 가장자리(X or Z)로 붙일지 결정
        float xMin = br.xMin, xMax = br.xMax;
        float zMin = br.yMin, zMax = br.yMax;

        Transform parent = wall;
        ClearGroup(parent);
        var group = new GameObject(groupName).transform;
        group.SetParent(parent, false);

        if (sideGroup == SideGroup.LeftRight)
        {
            // X를 가장 가까운 xMin/xMax로 고정, Z축으로 10..max 포함
            float fixedX = (Mathf.Abs(wall.position.x - xMin) < Mathf.Abs(wall.position.x - xMax)) ? xMin : xMax;

            // 회전/스케일
            Quaternion rot = Quaternion.Euler(0f, 90f, 0f);

            for (float z = zMin + unit; z <= zMax + 1e-4f; z += unit)
            {
                Vector3 pos = new Vector3(fixedX, y, z);
#if UNITY_EDITOR
                var go = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, group);
                go.transform.SetPositionAndRotation(pos, rot);
#else
                var go = Instantiate(fencePrefab, pos, rot, group);
#endif
                var s = go.transform.localScale;
                s.x = fenceScaleX;
                s.y = 0.4f;
                go.transform.localScale = s;
            }
        }
        else // FrontBack
        {
            // Z를 가장 가까운 zMin/zMax로 고정, X축으로 min..max-10 (최대 미포함)
            float fixedZ = (Mathf.Abs(wall.position.z - zMin) < Mathf.Abs(wall.position.z - zMax)) ? zMin : zMax;

            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);

            for (float x = xMin; x <= xMax - unit + 1e-4f; x += unit)
            {
                Vector3 pos = new Vector3(x, y, fixedZ);
#if UNITY_EDITOR
                var go = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, group);
                go.transform.SetPositionAndRotation(pos, rot);
#else
                var go = Instantiate(fencePrefab, pos, rot, group);
#endif
                var s = go.transform.localScale;
                s.x = fenceScaleX;
                s.y = 0.4f;
                go.transform.localScale = s;
            }
        }
    }

    [ContextMenu("Clear Fences")]
    public void ClearFences()
    {
        if (!wall) return;
        ClearGroup(wall);
    }

    void ClearGroup(Transform parent)
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
