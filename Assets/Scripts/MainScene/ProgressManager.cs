using System.Collections.Generic;
using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    #if UNITY_EDITOR
        [Header("Debug (Editor Only)")]
        public bool debugOverrideBest = false;
        [Range(0, 999)] public int debugBestScore = 0;
        public bool debugAutoSave = false;

        void OnValidate()
        {
            // 에디터에서 인스펙터 값 바뀔 때 호출됨
            if (!Application.isPlaying) return;          // 플레이 중에만
            if (!debugOverrideBest || Data == null) return;

            if (Data.bestScore != debugBestScore)
            {
                Data.bestScore = debugBestScore;
                OnBestScoreChanged?.Invoke(Data.bestScore); // UI/게이지 갱신
                if (debugAutoSave) Save();                  // 원하면 자동 저장
                Debug.Log($"[DEBUG] bestScore = {Data.bestScore}");
            }
        }

        // 버튼으로 강제 적용하고 싶으면
        [ContextMenu("DEBUG: Apply Inspector BestScore")]
        public void DebugApplyInspectorBestScore()
        {
            if (!Application.isPlaying || Data == null) return;
            Data.bestScore = debugBestScore;
            OnBestScoreChanged?.Invoke(Data.bestScore);
            if (debugAutoSave) Save();
            Debug.Log($"[DEBUG] Applied bestScore = {Data.bestScore}");
        }
    #endif

    public static ProgressManager Instance { get; private set; }
    public SaveData Data { get; private set; }

    // 메인 메뉴/게임에서 구독 가능
    public System.Action<int> OnBestScoreChanged;
    public System.Action OnUnlocksChanged;
    public System.Action<int> OnBestTimeChangedMs; // (bestTimeMs)

    [Header("Unlock Table (Resources/Achievements)")]
    public string achievementsFolder = "Achievements"; // ScriptableObject 모음 폴더

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        // 씬 로드 전에 무조건 한 번 존재 보장
        if (Instance == null)
        {
            var go = new GameObject("ProgressManager");
            go.AddComponent<ProgressManager>();
        }
    }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = SaveSystem.Load();
        //1. 기본 스킨이 없다면 추가
        if (!Data.unlockedSkins.Contains("skin_default"))
            Data.unlockedSkins.Add("skin_default");

        // 2. 현재 장착 스킨이 비어 있으면 기본 스킨 장착
        if (string.IsNullOrEmpty(Data.equippedSkinId))
            Data.equippedSkinId = "skin_default";
    }

    public void Save() => SaveSystem.Save(Data);

    // 게임이 끝났을 때 점수 보고
    public void ReportRunScore(int score)
    {
        if (score > Data.bestScore)
        {
            Data.bestScore = score;
            OnBestScoreChanged?.Invoke(Data.bestScore);
            Save();
        }
    }

    public bool HasSkin(string id) => Data.unlockedSkins.Contains(id);
    public bool HasAbility(string key) => Data.unlockedAbilities.Contains(key);

    public void EquipSkin(string id)
    {
        if (HasSkin(id)) { Data.equippedSkinId = id; Save(); }
    }

    public void ReportRunTimeMs(int timeMs)
    {
        if (timeMs > Data.bestTimeMs)
        {
            Data.bestTimeMs = timeMs;
            OnBestTimeChangedMs?.Invoke(Data.bestTimeMs);
            Save();
        }
    }
    public void ResetProgress(bool saveFileAfter = false)
    {
        Data = new SaveData();
        if (!Data.unlockedSkins.Contains("skin_default"))
            Data.unlockedSkins.Add("skin_default");

        // UI 갱신 이벤트 쏘기
        OnBestScoreChanged?.Invoke(Data.bestScore);
        OnBestTimeChangedMs?.Invoke(Data.bestTimeMs);
        OnUnlocksChanged?.Invoke();

        if (saveFileAfter) Save(); // 기본값 false면 파일은 안 만듦(완전 초기화 느낌)
        Debug.Log("[Progress] Reset done");
    }
    public bool IsAchievementEligible(string achievementId)
{
    foreach (var a in Achievements.Table)
        if (a.id == achievementId)
            return Data.bestScore >= a.requiredBestScore;
    return false;
}

public bool IsAchievementClaimed(string achievementId)
{
    return Data.claimedAchievements.Contains(achievementId);
}

    public bool TryClaim(string achievementId)
    {
        // 존재 확인 + 자격 + 중복 수령 방지
        Achievement? found = null;
        foreach (var a in Achievements.Table)
            if (a.id == achievementId) { found = a; break; }
        if (found == null) return false;

        var ach = found.Value;
        if (!IsAchievementEligible(achievementId)) return false;
        if (IsAchievementClaimed(achievementId)) return false;

        // 보상 지급
        switch (ach.unlockType)
        {
            case UnlockType.Skin:
                if (!Data.unlockedSkins.Contains(ach.payloadId))
                {
                    Data.unlockedSkins.Add(ach.payloadId);
                    Data.equippedSkinId = ach.payloadId;//바로장착 테스트용
                    RewardDB.GrantVisualOrRuntime(ach.payloadId, this);
                    Save();
                    OnUnlocksChanged?.Invoke();
                    Debug.Log($"[Skin] equipped = {ProgressManager.Instance.Data.equippedSkinId}");
                }
                break;

            case UnlockType.Ability:
                if (!Data.unlockedAbilities.Contains(ach.payloadId))
                    Data.unlockedAbilities.Add(ach.payloadId);
                break;
        }

        // 수령 기록 + 저장 + 이벤트
        Data.claimedAchievements.Add(achievementId);
        Save();
        OnUnlocksChanged?.Invoke();
        return true;
    }

}
