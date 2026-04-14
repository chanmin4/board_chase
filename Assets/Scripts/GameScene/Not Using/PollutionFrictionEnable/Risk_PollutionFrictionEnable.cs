/*
using UnityEngine;

[DisallowMultipleComponent]
public class Risk_PollutionFrictionEnable : MonoBehaviour
{
    public bool applyOnStart = true;

    [Header("Targets (비우면 자동검색)")]
    public DiskLauncher[] disks;

    [Header("Param")]
    public bool enableFriction = true;           // bool
    [Min(0f)] public float dampingPerSec = 0.1f; // float

    // 원복용
    bool[] origEnabled;
    float[] origDamping;
    bool captured;

    void Awake()
    {
        if (disks == null || disks.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            disks = Object.FindObjectsByType<DiskLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            disks = FindObjectsOfType<DiskLauncher>(true);
#endif
        }

        if (disks != null && disks.Length > 0)
        {
            int n = disks.Length;
            origEnabled = new bool[n];
            origDamping = new float[n];

            for (int i = 0; i < n; i++)
            {
                var dl = disks[i];
                if (!dl) continue;
                var comp = dl.GetComponent<PollutionDampingOnDisk>();
                if (comp)
                {
                    origEnabled[i] = comp.riskEnabled;
                    origDamping[i] = comp.dampingPerSec;
                }
                else
                {
                    // 없던 경우 기본값 기록
                    origEnabled[i] = false;
                    origDamping[i] = 0f;
                }
            }
            captured = true;
        }
    }

    void Start() { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable() { if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!captured || disks == null) return;
        for (int i = 0; i < disks.Length; i++)
        {
            var dl = disks[i];
            if (!dl) continue;
            var comp = dl.GetComponent<PollutionDampingOnDisk>()
                       ?? dl.gameObject.AddComponent<PollutionDampingOnDisk>();
            comp.riskEnabled = enableFriction;
            comp.dampingPerSec = Mathf.Max(0f, dampingPerSec);
        }
    }

    public void Revert()
    {
        if (!captured || disks == null) return;
        for (int i = 0; i < disks.Length; i++)
        {
            var dl = disks[i];
            if (!dl) continue;
            var comp = dl.GetComponent<PollutionDampingOnDisk>();
            if (!comp) continue;
            // 제거하지 않고 원래 값만 복구 (없던 애는 false/0으로 돌아감)
            comp.riskEnabled = origEnabled[i];
            comp.dampingPerSec = origDamping[i];
        }
    }
}
*/