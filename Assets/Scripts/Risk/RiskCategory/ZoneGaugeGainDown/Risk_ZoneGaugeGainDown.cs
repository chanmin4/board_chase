using UnityEngine;

[DisallowMultipleComponent]
public class Risk_ZoneGaugeGainDown : MonoBehaviour
{
    [Header("Installer 호환용(사용되진 않음)")]
    public bool applyOnStart = true;

    [Header("Targets")]
    // TODO: 인스펙터에 실제 게이지 대상(예: Gauge/Director 등) 컴포넌트를 지정
    public SurvivalDirector[] director;
    [Header("Param")]
    [Min(0.01f)] public float zoneEnterBonusMul = 0.75f; // f0 = 게이지 획득 배수

    // 원본 값 저장
    float[] orig_gainMul;
    bool captured;

    void Awake()
    {
        if (director == null || director.Length == 0)
            director = UnityEngine.Object.FindObjectsByType<SurvivalDirector>(
            FindObjectsInactive.Include,   // ← 예전의 true (비활성 포함)
            FindObjectsSortMode.None       // 정렬 불필요하면 None이 가장 빠름
            );

        if (director != null && director.Length > 0)
        {
            orig_gainMul = new float[director.Length];
            for (int i = 0; i < director.Length; i++)
            {
                if (!director[i]) continue;
                orig_gainMul[i] = director[i].zoneEnterBonusMul;
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
        float m = Mathf.Max(0.01f, zoneEnterBonusMul);
        for (int i = 0; i < director.Length; i++)
        {
            if (!director[i]) continue;
            director[i].zoneEnterBonusMul = orig_gainMul[i] * m;
        }
    }

    public void Revert()
    {
        if (!captured) return;
        for (int i = 0; i < director.Length; i++)
        {
            if (!director[i]) continue;
            director[i].zoneEnterBonusMul = orig_gainMul[i];
        }
    }
}
