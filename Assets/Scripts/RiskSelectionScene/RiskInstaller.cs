using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class RiskInstaller : MonoBehaviour
{
    [Header("Apply When")]
    [Tooltip("비우면 다음 씬 로드될 때 첫 씬에 바로 적용, 채우면 해당 이름일 때만")]
    public string applyOnSceneName = "";
    public bool autoDestroyAfterApply = true;

    bool _applied;

    // ── DEBUG (인스펙터에서 바로 확인) ─────────────────────────────
    [Header("Debug Set/Selection (ReadOnly)")]
    public RiskSet dbgSet;
    public List<RiskDef> dbgSelected = new();
    public string[] dbgSelectedTitles = new string[0];

    [Header("Debug Aggregates (ReadOnly)")]
    public float dbgDragCooldownExtra = 0f;
    public float dbgMissileSpeedMul   = 1f;
    public float dbgExplosionRadiusMul= 1f;
    public bool  dbgSpawnEachCycle    = false;
    public int   dbgMissileCountAdd   = 0;

    [Header("Debug Targets (ReadOnly)")]
    public DiskLauncher dbgLauncher;
    public HomingRocket dbgMissile;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static RiskInstaller Spawn(string applyOnSceneName = "")
    {
        var go = new GameObject("__RiskInstaller");
        var inst = go.AddComponent<RiskInstaller>();
        inst.applyOnSceneName = applyOnSceneName;
        return inst;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (_applied) return;
        if (!string.IsNullOrEmpty(applyOnSceneName) && s.name != applyOnSceneName) return;

        ApplyAll();
        _applied = true;
        if (autoDestroyAfterApply) Destroy(gameObject);
    }

    [ContextMenu("ApplyAll (Manual)")]
    void ApplyAll()
    {
        // 1) 세션 스냅샷/디버그
        dbgSet = RiskSession.Set;
        dbgSelected = RiskSession.Selected?.Where(d => d).ToList() ?? new List<RiskDef>();
        dbgSelectedTitles = dbgSelected.Select(d => d.title).ToArray();

        if (dbgSelected.Count == 0)
        {
            Debug.Log("[RiskInstaller] 선택이 비어있음 — 적용할 게 없음");
            return;
        }

        // 2) 대상 탐색 (씬에만 있으면 됨)
        dbgLauncher = FindAnyObjectByType<DiskLauncher>();
        dbgMissile  = FindAnyObjectByType<HomingRocket>();
        if (!dbgLauncher) Debug.LogWarning("[RiskInstaller] DiskLauncher를 못 찾음");
        if (!dbgMissile)  Debug.LogWarning("[RiskInstaller] HomingMissile를 못 찾음");

        // 3) 누적값 초기화
        float dragCooldownExtra   = 0f;
        float missileSpeedMul     = 1f;
        float explosionRadiusMul  = 1f;
        bool? spawnEachCycle      = null;
        int   missileCountAdd     = 0;

        // 4) 선택 항목 집계
        foreach (var def in dbgSelected)
        {
            switch (def.type)
            {
                case RiskType.DragCooldownAdd:
                    // ★ 여기가 쿨다운 초를 더하는 부분
                    dragCooldownExtra += Mathf.Max(0f, def.float_parameter1);
                    break;

                case RiskType.MissileSpeedUp:
                    missileSpeedMul *= Mathf.Max(0.01f, def.float_parameter1);
                    break;

                case RiskType.MissileExplosionUp:
                    explosionRadiusMul *= Mathf.Max(0.01f, def.float_parameter1);
                    break;

                case RiskType.MissileSpawnEveryCycle:
                    spawnEachCycle = def.bool_parameter;
                    break;

                case RiskType.MissileCountUp:
                    missileCountAdd += Mathf.RoundToInt(def.float_parameter1);
                    break;
            }
        }

        // 디버그 반영
        dbgDragCooldownExtra   = dragCooldownExtra;
        dbgMissileSpeedMul     = missileSpeedMul;
        dbgExplosionRadiusMul  = explosionRadiusMul;
        dbgSpawnEachCycle      = spawnEachCycle ?? dbgSpawnEachCycle;
        dbgMissileCountAdd     = missileCountAdd;

        // 5) 실제 적용
        // Drag Cooldown
        if (dbgLauncher && dragCooldownExtra > 0f)
        {
            var patch = dbgLauncher.GetComponent<Risk_DragCooldown>();
            if (!patch) patch = dbgLauncher.gameObject.AddComponent<Risk_DragCooldown>();
            patch.disklauncher = dbgLauncher;
            patch.addSeconds   = dragCooldownExtra;      // ★ 여기로 값 전달
            patch.applyOnStart = false;
            patch.Apply();
            Debug.Log($"[RiskInstaller] DragCooldownAdd +{dragCooldownExtra:0.##}s 적용");
        }

        // Missile speed
        if (dbgMissile && !Mathf.Approximately(missileSpeedMul, 1f))
        {
            var p = dbgMissile.GetComponent<Risk_MissileSpeedUp>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileSpeedUp>();
            p.homingMissile = dbgMissile;
            p.speedMul      = missileSpeedMul;
            p.applyOnStart  = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileSpeedUp x{missileSpeedMul:0.##} 적용");
        }

        // Explosion radius
        if (dbgMissile && !Mathf.Approximately(explosionRadiusMul, 1f))
        {
            var p = dbgMissile.GetComponent<Risk_MissileExplosionUp>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileExplosionUp>();
            p.homingMissile = dbgMissile;
            p.radiusMul     = explosionRadiusMul;
            p.applyOnStart  = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileExplosionUp x{explosionRadiusMul:0.##} 적용");
        }

        // TODO: spawnEachCycle / missileCountAdd도 필요해지면 같은 패턴으로

        // 6) 1판 페이로드 비우기
        RiskSession.Clear();
    }
}
