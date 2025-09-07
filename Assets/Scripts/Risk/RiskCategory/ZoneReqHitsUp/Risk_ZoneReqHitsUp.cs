using UnityEngine;

[DisallowMultipleComponent]
public class Risk_ZoneReqHitsUp : MonoBehaviour
{
    [Header("Installer 호환용(사용되진 않음)")]
    public bool applyOnStart = true;

    [Header("Targets")]
     public SurvivalDirector director;

    [Header("Param")]
    public int addRequiredHits_S = 0;
    public int addRequiredHits_M = 0;
    public int addRequiredHits_L = 0;

    int orig_requiredHits_S;
    int orig_requiredHits_M;
    int orig_requiredHits_L;
    bool captured;

    void Awake()
    {
       if (!director)
            director = FindAnyObjectByType<SurvivalDirector>();

        if (director)
        {
            // SurvivalDirector에 아래 세 필드가 있어야 함:
            // public int zoneReqHitsAdd_S, zoneReqHitsAdd_M, zoneReqHitsAdd_L;
            orig_requiredHits_S = director.zoneReqHitsAdd_S;
            orig_requiredHits_M = director.zoneReqHitsAdd_M;
            orig_requiredHits_L = director.zoneReqHitsAdd_L;
            captured = true;
        }
    }

    void Start()    { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!captured || !director) return;

        director.zoneReqHitsAdd_S = orig_requiredHits_S + addRequiredHits_S;
        director.zoneReqHitsAdd_M = orig_requiredHits_M + addRequiredHits_M;
        director.zoneReqHitsAdd_L = orig_requiredHits_L + addRequiredHits_L;
    }

    public void Revert()
    {
        if (!captured || !director) return;

        // 원복
        director.zoneReqHitsAdd_S = orig_requiredHits_S;
        director.zoneReqHitsAdd_M = orig_requiredHits_M;
        director.zoneReqHitsAdd_L = orig_requiredHits_L;
    }
}
