using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// 디스크: "실제 페인트"가 찍힐 때마다 XP를 얻고, 레벨업 시 잉크 반지름(굵기)을 늘린다.
/// - BoardPaintSystem.OnPlayerPaintStamp 이벤트를 구독해 실제 성공 시만 XP가 증가.
/// - CleanTrailAbility_Disk의 radiusMul / radiusAddWorld를 레벨업 때마다 가변.
/// - 인스펙터에서 레벨마다 필요 XP, 증가량을 개별 튜닝 가능.
[RequireComponent(typeof(CleanTrailAbility_Disk))]
public class DiskInkLeveler : MonoBehaviour
{
    [Header("Gain Settings")]
    [Tooltip("스탬프 길이(m) 1당 기본 XP")]
    public float xpPerMeter = 0.2f;
    [Tooltip("적 잉크 덧칠 시 XP 배수")]
    public float contamXPMul = 0.2f;
    [Tooltip("면적 기반 추가 XP (πr² × 이 값) — 0이면 미사용")]
    public float areaXPPerSquare = 0f;

    [Header("Level Requirements (per level)")]
    [Tooltip("각 레벨에 도달하기 위한 필요 XP (L0→L1, L1→L2, ...). 비어있으면 모두 100으로 간주")]
    public List<float> xpNeedPerLevel = new List<float> { 100, 120, 150, 180, 220 };

    [Header("Radius Growth (per level)")]
    [Tooltip("레벨업 시 radiusMul에 더할 값(레벨마다). 비어있으면 모두 0으로 간주")]
    public List<float> addToRadiusMul = new List<float> { 0.25f, 0.25f, 0.25f, 0.25f, 0.25f };
    [Tooltip("레벨업 시 radiusAddWorld(미터)에 더할 값(레벨마다). 비어있으면 0으로 간주")]
    public List<float> addToRadiusAddWorld = new List<float> { 0f, 0f, 0f, 0f, 0f };

    [Header("Events (UI)")]   
    public UnityEvent<int> OnLevelChanged;               
    public UnityEvent<float, float> OnXPChanged;
    public UnityEvent<float, string> OnXPGained;       //amount, 어디서얻었는가  
    public UnityEvent<int> OnLevelUp;
    
    [Header("Debug/State")]
    [SerializeField] int   level = 0;
    [SerializeField] float curXP = 0;

    CleanTrailAbility_Disk trail;
    int maxDefined => Mathf.Max(xpNeedPerLevel.Count, addToRadiusMul.Count, addToRadiusAddWorld.Count);
    int MaxLevel
    {
        get
        {
            // 요구치와 성장량 리스트 중 정의된 만큼만 올릴 수 있게 cap
            int capNeed = (xpNeedPerLevel != null && xpNeedPerLevel.Count > 0) ? xpNeedPerLevel.Count : int.MaxValue;
            return capNeed;
        }
    }

    void Awake()
    {
        trail = GetComponent<CleanTrailAbility_Disk>();
    }

    void OnEnable()
    {
        BoardPaintSystem.OnPlayerPaintStamp += OnPainted;
        EmitProgress();          
        OnLevelChanged?.Invoke(level); 
    }
    void OnDisable()
    {
        BoardPaintSystem.OnPlayerPaintStamp -= OnPainted;
    }

    void OnPainted(float meters, bool isContam, Vector3 pos, float radius)
    {
        float xp = meters * Mathf.Max(0f, xpPerMeter);
        if (isContam) xp *= contamXPMul;

        if (areaXPPerSquare > 0f)
        {
            float area = Mathf.PI * radius * radius;
            xp += area * areaXPPerSquare;
        }

        if (xp > 0f) GrantXP(xp, "paint");   // ★ CHANGED
    
    }
    void EmitProgress()
    {
        float need = GetNeedForLevel(level);
        if (need < 1f) need = 1f;
        OnXPChanged?.Invoke(curXP, need);
    }
    float GetNeedForLevel(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return float.PositiveInfinity; // 더 이상 안 오름
        if (xpNeedPerLevel == null || xpNeedPerLevel.Count == 0) return 100f;
        int idx = Mathf.Clamp(currentLevel, 0, xpNeedPerLevel.Count - 1);
        return Mathf.Max(1f, xpNeedPerLevel[idx]);
    }


    float GetAddToMul(int levelJustReached)
    {
        if (addToRadiusMul == null || addToRadiusMul.Count == 0) return 0f;
        int idx = Mathf.Clamp(levelJustReached, 0, addToRadiusMul.Count - 1);
        return addToRadiusMul[idx];
    }

    float GetAddToAddWorld(int levelJustReached)
    {
        if (addToRadiusAddWorld == null || addToRadiusAddWorld.Count == 0) return 0f;
        int idx = Mathf.Clamp(levelJustReached, 0, addToRadiusAddWorld.Count - 1);
        return addToRadiusAddWorld[idx];
    }


public void GrantXP(float amount, string reason = null)   // ★ ADDED
{
    if (amount <= 0f) return;

    // 1) 누적
    curXP += amount;
    OnXPGained?.Invoke(amount, reason); // UI/사운드 등 구독 가능

    // 2) 진행도 갱신 이벤트
    EmitProgress();

    // 3) 레벨업 루프
    int safety = 64;
    while (level < MaxLevel && safety-- > 0)
    {
        float need = GetNeedForLevel(level);
        if (need <= 0f || curXP + 1e-6f < need) break;

        curXP -= need;
        level++;

        // 반지름 성장 적용
        trail.radiusMul      += GetAddToMul(level - 1);
        trail.radiusAddWorld += GetAddToAddWorld(level - 1);

            // (선택) 상한 클램프가 있다면 여기에서
            // trail.radiusMul      = Mathf.Min(trail.radiusMul, radiusMulMax);
            // trail.radiusAddWorld = Mathf.Min(trail.radiusAddWorld, radiusAddWorldMax);

        OnLevelChanged?.Invoke(level); // ui 표시용
        OnLevelUp?.Invoke(level);
        
        EmitProgress(); // 필요치 바뀌었으니 다시
    }
}
    // === 옵션: 현재 상태 접근용 프로퍼티/디버그 메서드 ===
    public int Level => level;
    public float CurrentXP => curXP;

    [ContextMenu("DEBUG_Add_XP_50")]
    void DBG_AddXP() { OnPainted(50f, false, Vector3.zero, 0.5f); } // 길이 50m 페인트한 것과 같은 효과
}
