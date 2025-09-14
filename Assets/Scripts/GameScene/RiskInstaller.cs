using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System;

public class RiskInstaller : MonoBehaviour
{
    [Header("Apply When")]
    [Tooltip("비우면 다음 씬 로드될 때 첫 씬에 바로 적용, 채우면 해당 이름일 때만")]
    public string applyOnSceneName = "";

    bool _applied;

    // ── DEBUG (인스펙터에서 바로 확인) ─────────────────────────────
    [Header("Debug View (ReadOnly)")]
    [SerializeField] int dbgTotalPoints = 0;
    [SerializeField, TextArea(3, 12)] string dbgSelectedSummary = "";
    [SerializeField, TextArea(2, 6)] string dbgTypeCountSummary = "";



    [Header("Debug Set/Selection (ReadOnly)")]
    public RiskSet dbgSet;
    public List<RiskDef> dbgSelected = new();
    public string[] dbgSelectedTitles = new string[0];

    [Header("Debug Aggregates (ReadOnly)")]
    public float dbgDragCooldownExtra = 0f;
    public float dbgMissileSpeedMul = 1f;
    public float dbgExplosionRadiusMul = 1f;
    public int dbgSpawnCycle = 1;
    public int dbgMissileCountAdd = 0;
    public int dbgZoneReqHitsUp_S = 0;
    public int dbgZoneReqHitsUp_M = 0;
    public int dbgZoneReqHitsUp_L = 0;





    [Header("Debug Targets (ReadOnly)")]
    public DiskLauncher dbgLauncher;
    public HomingRocket dbgMissile;
    public SurvivalDirector dbgSurvivalDirector;
    public CardManager dbgCardManager;
    [Tooltip("Apply 직후 RiskSession을 바로 비울지 여부(기본: 비우지 않음). Retry를 지원하려면 false 권장")]
    public bool clearOnApply = false;   // false일시 재도전 리스크 그대로 적용
    void Awake()
    {
        #if UNITY_2023_1_OR_NEWER
            var first = FindFirstObjectByType<RiskInstaller>(FindObjectsInactive.Include);
        #else
            var first = FindObjectOfType<RiskInstaller>();
        #endif
            if (first && first != this) { Destroy(gameObject); return; }   // 중복 차단


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
        //if (_applied) return;
        if (!string.IsNullOrEmpty(applyOnSceneName) && s.name != applyOnSceneName) return;

        ApplyAll();
        //_applied = true;
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
        dbgTotalPoints = dbgSelected.Sum(d => Mathf.Max(0, d.points));
        dbgSelectedSummary = string.Join("\n", dbgSelected.Select(FormatRiskLine));
        var byType = dbgSelected.GroupBy(d => d.type)
                        .OrderBy(g => g.Key.ToString())
                        .Select(g => $"{g.Key}: {g.Count()}");
        dbgTypeCountSummary = string.Join("\n", byType);

        // 2) 대상 탐색 (씬에만 있으면 됨)
        dbgLauncher = FindAnyObjectByType<DiskLauncher>();
        dbgMissile = FindAnyObjectByType<HomingRocket>();
        dbgSurvivalDirector = FindAnyObjectByType<SurvivalDirector>();
        dbgCardManager = FindAnyObjectByType<CardManager>();
        if (!dbgLauncher) Debug.LogWarning("[RiskInstaller] DiskLauncher를 못 찾음");
        if (!dbgMissile) Debug.LogWarning("[RiskInstaller] HomingMissile를 못 찾음");
        if (!dbgSurvivalDirector) Debug.LogWarning("[RiskInstaller] SurvivalDirector를 못 찾음");
        if (!dbgCardManager) Debug.LogWarning("[RiskInstaller] CardManager를 못 찾음");
        // 3) 누적값 초기화
        bool onDragCooldown = false;
        bool onMissileSpeed = false;
        bool onMissileExplosion = false;
        bool onSpawnCycle = false;
        bool onMissileCount = false;
        bool onZoneGaugeMul = false;
        bool onZoneReqHits = false;
        bool onZoneComp = false;
        bool onCardCharge = false;
        bool onCardDisable = false;
        bool onPollutionFriction = false;



        float dragCooldownExtra = 0f;

        float missileSpeedMul = 1f;

        float explosionRadiusMul = 1f;

        int spawnCycle = 0;

        int missileCountAdd = 0;

        float zoneEnterBonusMul = 1f;

        int ZoneReqHitsUp_S = 0;
        int ZoneReqHitsUp_M = 0;
        int ZoneReqHitsUp_L = 0;

        int ZoneCountS = 0;
        int ZoneCountM = 0;
        int ZoneCountL = 0;

        int CardChargeAdd = 0;

        bool Carddisable = false;

        bool pollutionfriction = false;
        float damping = 0;

        // 4) 선택 항목 집계
        foreach (var def in dbgSelected)
        {
            switch (def.type)
            {
                case RiskType.DragCooldownAdd:
                    // ★ 여기가 쿨다운 초를 더하는 부분
                    dragCooldownExtra = Mathf.Max(0f, def.float_parameter1);
                    onDragCooldown = dragCooldownExtra > 0f;
                    break;

                case RiskType.MissileSpeedUp:
                    missileSpeedMul *= Mathf.Max(0.01f, def.float_parameter1);
                    onMissileSpeed = !Mathf.Approximately(missileSpeedMul, 1f);
                    break;

                case RiskType.MissileExplosionUp:
                    explosionRadiusMul *= Mathf.Max(0.01f, def.float_parameter1);
                    onMissileExplosion = !Mathf.Approximately(explosionRadiusMul, 1f);
                    break;

                case RiskType.MissileSpawnEveryCycle:
                    spawnCycle = Mathf.RoundToInt(def.float_parameter1);
                    onSpawnCycle = spawnCycle >= 1;
                    break;

                case RiskType.MissileCountUp:
                    missileCountAdd = Mathf.RoundToInt(def.float_parameter1);
                    onMissileCount = (missileCountAdd != 0);
                    break;
                case RiskType.ZoneGaugeGainDown:
                    zoneEnterBonusMul *= Mathf.Max(0.01f, def.float_parameter1);
                    onZoneGaugeMul = !Mathf.Approximately(zoneEnterBonusMul, 1f);    // ★
                    break;
                case RiskType.ZoneReqHitsUp:
                    ZoneReqHitsUp_S = Mathf.RoundToInt(def.float_parameter1);
                    ZoneReqHitsUp_M = Mathf.RoundToInt(def.float_parameter2);
                    ZoneReqHitsUp_L = Mathf.RoundToInt(def.float_parameter3);
                    onZoneReqHits = (ZoneReqHitsUp_S | ZoneReqHitsUp_M | ZoneReqHitsUp_L) != 0;
                    break;
                case RiskType.ZoneCompositionChange:
                    ZoneCountS = Mathf.RoundToInt(def.float_parameter1);
                    ZoneCountM = Mathf.RoundToInt(def.float_parameter2);
                    ZoneCountL = Mathf.RoundToInt(def.float_parameter3);
                    onZoneComp = (ZoneCountS + ZoneCountM + ZoneCountL) > 0;
                    break;
                case RiskType.CardChargeRequiredUp:
                    CardChargeAdd = Mathf.RoundToInt(def.float_parameter1);
                    onCardCharge = (CardChargeAdd != 0);
                    break;

                case RiskType.CardDisabled:
                    Carddisable = def.bool_parameter;
                    onCardDisable = Carddisable;
                    break;
                case RiskType.PollutionFrictionEnable:
                    damping = def.float_parameter1;
                    pollutionfriction = def.bool_parameter;
                    onPollutionFriction = pollutionfriction;
                    break;




            }
        }

        // 디버그 반영
        dbgDragCooldownExtra = dragCooldownExtra;

        dbgMissileSpeedMul = missileSpeedMul;

        dbgExplosionRadiusMul = explosionRadiusMul;

        dbgSpawnCycle = spawnCycle;

        dbgMissileCountAdd = missileCountAdd;

        dbgZoneReqHitsUp_S = ZoneReqHitsUp_S;
        dbgZoneReqHitsUp_M = ZoneReqHitsUp_M;
        dbgZoneReqHitsUp_L = ZoneReqHitsUp_L;


        // 5) 실제 적용
        // Drag Cooldown
        if (dbgLauncher && onDragCooldown)
        {
            var patch = dbgLauncher.GetComponent<Risk_DragCooldown>();
            if (!patch) patch = dbgLauncher.gameObject.AddComponent<Risk_DragCooldown>();
            patch.disklauncher = dbgLauncher;
            patch.addSeconds = dragCooldownExtra;      // ★ 여기로 값 전달
            patch.applyOnStart = false;
            patch.Apply();
            Debug.Log($"[RiskInstaller] DragCooldownAdd +{dragCooldownExtra:0.##}s 적용");
        }

        // Missile speed
        if (dbgMissile && onMissileSpeed)
        {
            var p = dbgMissile.GetComponent<Risk_MissileSpeedUp>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileSpeedUp>();
            p.homingMissile = dbgMissile;
            p.speedMul = missileSpeedMul;
            p.applyOnStart = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileSpeedUp x{missileSpeedMul:0.##} 적용");
        }

        // Explosion radius
        if (dbgMissile && onMissileExplosion)
        {
            var p = dbgMissile.GetComponent<Risk_MissileExplosionUp>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileExplosionUp>();
            p.homingMissile = dbgMissile;
            p.radiusMul = explosionRadiusMul;
            p.applyOnStart = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileExplosionUp x{explosionRadiusMul:0.##} 적용");
        }
        //missileCountUp
        if (dbgMissile && onMissileCount)
        {
            var p = dbgMissile.GetComponent<Risk_MissileCountUp>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileCountUp>();
            p.homingMissile = dbgMissile;
            p.missilecnt = missileCountAdd;
            p.applyOnStart = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileExplosionUp x{missileCountAdd:0.##} 적용");
        }
        //missilespawneverycycle
        if (dbgMissile && onSpawnCycle)
        {
            var p = dbgMissile.GetComponent<Risk_MissileSpawnEveryCycle>() ?? dbgMissile.gameObject.AddComponent<Risk_MissileSpawnEveryCycle>();
            p.homingMissile = dbgMissile;
            p.MissileSpawnCycle = spawnCycle;
            p.applyOnStart = false;
            p.Apply();
            Debug.Log($"[RiskInstaller] MissileExplosionUp x{spawnCycle:0.##} 적용");
        }
        //ZoneGaugeGainDown
        if (dbgSurvivalDirector && onZoneGaugeMul)
        {
            var p = dbgSurvivalDirector.GetComponent<Risk_ZoneGaugeGainDown>() ?? dbgSurvivalDirector.gameObject.AddComponent<Risk_ZoneGaugeGainDown>();
            p.zoneEnterBonusMul = zoneEnterBonusMul;
            p.applyOnStart = false;
            p.Apply();
        }

        //ZoneReqHitsUp
        if (dbgSurvivalDirector && onZoneReqHits)
        {
            var p = dbgSurvivalDirector.GetComponent<Risk_ZoneReqHitsUp>() ?? dbgSurvivalDirector.gameObject.AddComponent<Risk_ZoneReqHitsUp>();
            p.addRequiredHits_L = ZoneReqHitsUp_L;
            p.addRequiredHits_M = ZoneReqHitsUp_M;
            p.addRequiredHits_S = ZoneReqHitsUp_S;
            p.applyOnStart = false;
            p.Apply();
        }
        //ZoneCompositionChange
        if (dbgSurvivalDirector && onZoneComp)
        {
            var p = dbgSurvivalDirector.GetComponent<Risk_ZoneCompositionChange>() ?? dbgSurvivalDirector.gameObject.AddComponent<Risk_ZoneCompositionChange>();
            p.countL = ZoneCountL;
            p.countM = ZoneCountM;
            p.countS = ZoneCountS;
            p.applyOnStart = false;
            p.Apply();

        }
        //CardChargeRequireAdd
        if (dbgCardManager && onCardCharge)
        {
            var p = dbgCardManager.GetComponent<Risk_CardChargeRequiredUp>() ?? dbgCardManager.gameObject.AddComponent<Risk_CardChargeRequiredUp>();
            p.addChargeRequired = CardChargeAdd;
            p.applyOnStart = false;
            p.Apply();
        }


        //CardDisable
        if (dbgCardManager && onCardDisable)
        {
            var p = dbgCardManager.GetComponent<Risk_CardDisabled>() ?? dbgCardManager.gameObject.AddComponent<Risk_CardDisabled>();
            p.disable = Carddisable;
            p.applyOnStart = false;
            p.Apply();
        }
        //pollution friction
        if (dbgLauncher && onPollutionFriction)
        {
            var p = dbgLauncher.GetComponent<Risk_PollutionFrictionEnable>()
            ?? dbgLauncher.gameObject.AddComponent<Risk_PollutionFrictionEnable>();

            p.enableFriction = pollutionfriction;
            p.dampingPerSec = -Mathf.Log(Mathf.Clamp(damping, 0.01f, 0.999f)); // 지수형태의감속 0.1 이면 90퍼 감속
            p.applyOnStart = false;
            p.Apply();
        }


        // TODO: spawnEachCycle / missileCountAdd도 필요해지면 같은 패턴으로

        // 6) 1판 페이로드 비우기
        if (clearOnApply)
            RiskSession.Clear();
    }
    // 인스톨러 중복 방지
    public static RiskInstaller EnsureSingleton(string applyOnSceneName = "")
    {
#if UNITY_2023_1_OR_NEWER
        var exist = UnityEngine.Object.FindFirstObjectByType<RiskInstaller>(FindObjectsInactive.Include);
#else
            var exist = Object.FindObjectOfType<RiskInstaller>();
#endif
        return exist ? exist : Spawn(applyOnSceneName);
    }

    string FormatRiskLine(RiskDef d)
    {
        if (!d) return "(null)";
        // 필요하면 파라미터도 간단히 표시
        // ex) f1=..., f2=..., b=...
        string extra = "";
        // 대표 파라미터만 가볍게 노출 (있을 때만)
        if (Mathf.Abs(d.float_parameter1) > 1e-6f) extra += $" f1:{d.float_parameter1:0.###}";
        if (Mathf.Abs(d.float_parameter2) > 1e-6f) extra += $" f2:{d.float_parameter2:0.###}";
        if (Mathf.Abs(d.float_parameter3) > 1e-6f) extra += $" f3:{d.float_parameter3:0.###}";
        if (d.bool_parameter) extra += " b:true";

        return $"[{Mathf.Max(0, d.points)}pt] {d.type} - {d.title}{(string.IsNullOrEmpty(extra) ? "" : " |" + extra)}";
    }

    // 인스펙터 메뉴로 수동 갱신
    [ContextMenu("Refresh Debug From Session")]
    void RefreshDebugFromSession()
    {
        // 세션 기준으로 디버그 필드만 갱신 (적용은 하지 않음)
        dbgSet = RiskSession.Set;
        dbgSelected = RiskSession.Selected?.Where(x => x).ToList() ?? new List<RiskDef>();
        dbgSelectedTitles = dbgSelected.Select(d => d.title).ToArray();

        dbgTotalPoints = dbgSelected.Sum(d => Mathf.Max(0, d.points));
        dbgSelectedSummary = string.Join("\n", dbgSelected.Select(FormatRiskLine));

        // 타입별 개수 집계
        var byType = dbgSelected.GroupBy(d => d.type)
                                .OrderBy(g => g.Key.ToString())
                                .Select(g => $"{g.Key}: {g.Count()}");
        dbgTypeCountSummary = string.Join("\n", byType);
    }





}
