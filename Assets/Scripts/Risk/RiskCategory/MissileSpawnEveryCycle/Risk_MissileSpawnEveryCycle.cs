using System;
using UnityEngine;

public class Risk_MissileSpawnEveryCycle : MonoBehaviour
{
   [Header("Installer 호환용(사용되진 않음)")]
    public MonoBehaviour homingMissile;
    public bool applyOnStart = true;//플레이시작시 자동 apply

    [Header("Targets")]
    [Tooltip("비워두면 씬에서 전부 자동 검색")]
    public BarrageMissileSpawner[] spawners;

    [Header("Param")]
    [Min(1)] public int MissileSpawnCycle= 1;
    int[] orig_missilespawncycle;
    bool captured; //원본 캡쳐여부
/*
    void Awake()
    {
        if (spawners == null || spawners.Length == 0)
            spawners = UnityEngine.Object.FindObjectsByType<BarrageMissileSpawner>(
            FindObjectsInactive.Include,   // ← 예전의 true (비활성 포함)
            FindObjectsSortMode.None       // 정렬 불필요하면 None이 가장 빠름
            );

        if (spawners != null && spawners.Length > 0)
        {
            orig_missilespawncycle = new int[spawners.Length];
            for (int i = 0; i < spawners.Length; i++)
            {
                orig_missilespawncycle[i]     = spawners[i].spawncycle;
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
        int cycle = Math.Max(1, MissileSpawnCycle);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].spawncycle = MissileSpawnCycle;
        }
    }

    public void Revert()
    {
        if (!captured) return;
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].spawncycle     = orig_missilespawncycle[i];
        }
    }
    */
}
