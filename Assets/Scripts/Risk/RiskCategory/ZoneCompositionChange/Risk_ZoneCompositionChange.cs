using UnityEngine;

public enum ZoneLayoutSize { S, M, L } // L/M/S 지정용 파라미터 (이번 구현에선 미사용)

[DisallowMultipleComponent]
public class Risk_ZoneCompositionChange : MonoBehaviour
{
    [Header("Installer 호환용(사용되진 않음)")]
    public bool applyOnStart = true;

    [Header("Targets")]
    public SurvivalDirector[] directors;                       // ★ 타깃을 명확히

    [Header("Param (이번 세트에서 강제할 개수)")]
    [Min(0)] public int countS = 0;                            // ★ Small 개수
    [Min(0)] public int countM = 0;                            // ★ Medium 개수
    [Min(0)] public int countL = 0;                            // ★ Large 개수

    // 원복용 스냅샷
    int[] origS, origM, origL;                               // ★
    bool captured;

    void Awake()
    {
        // 타깃 자동 수집(비워두면 씬에서 찾기)
        if (directors == null || directors.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            directors = UnityEngine.Object.FindObjectsByType<SurvivalDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            directors = FindObjectsOfType<SurvivalDirector>(true);
#endif
        }

        if (directors != null && directors.Length > 0)
        {

            origS = new int[directors.Length];
            origM = new int[directors.Length];
            origL = new int[directors.Length];

            for (int i = 0; i < directors.Length; i++)
            {
                if (!directors[i]) continue;
                origS[i] = directors[i].layoutCountSmall;
                origM[i] = directors[i].layoutCountMedium;
                origL[i] = directors[i].layoutCountLarge;
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

        for (int i = 0; i < directors.Length; i++)
        {
            var d = directors[i];
            if (!d) continue;

            d.layoutCountSmall  = Mathf.Max(0, countS);         //  S 개수
            d.layoutCountMedium = Mathf.Max(0, countM);         //  M 개수
            d.layoutCountLarge  = Mathf.Max(0, countL);         //  L 개수
            // d.RegenerateAllZones();  // RegenerateAllZones가 public일 때만
        }
    }

    public void Revert()
    {
        if (!captured) return;

        for (int i = 0; i < directors.Length; i++)
        {
            var d = directors[i];
            if (!d) continue;
            d.layoutCountSmall  = origS[i];
            d.layoutCountMedium = origM[i];
            d.layoutCountLarge  = origL[i];
        }
    }
}
