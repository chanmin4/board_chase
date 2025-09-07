using System;
using UnityEngine;

public class Risk_MissileCountUp : MonoBehaviour
{
   [Header("Installer 호환용(사용되진 않음)")]
    public MonoBehaviour homingMissile;
    public bool applyOnStart = true;//플레이시작시 자동 apply

    [Header("Targets")]
    [Tooltip("비워두면 씬에서 전부 자동 검색")]
    public BarrageMissileSpawner[] spawners;

    [Header("Param")]
    [Min(1)] public int missilecnt = 1;
    int[] orig_missilecnt;
    bool captured; //원본 캡쳐여부

    void Awake()
    {
        if (spawners == null || spawners.Length == 0)
            spawners = UnityEngine.Object.FindObjectsByType<BarrageMissileSpawner>(
            FindObjectsInactive.Include,   // ← 예전의 true (비활성 포함)
            FindObjectsSortMode.None       // 정렬 불필요하면 None이 가장 빠름
            );

        if (spawners != null && spawners.Length > 0)
        {
            orig_missilecnt = new int[spawners.Length];
            for (int i = 0; i < spawners.Length; i++)
            {
                orig_missilecnt[i]     = spawners[i].missileCount;
            }
            captured = true;
        }
    }

    void Start()    { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!captured) return;
        int cnt = Math.Max(1, missilecnt);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].missileCount = orig_missilecnt[i]+cnt;
        }
    }

    public void Revert()
    {
        if (!captured) return;
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].missileCount     = orig_missilecnt[i];
        }
    }
}
