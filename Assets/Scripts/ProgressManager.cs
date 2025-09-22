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
        if (!Application.isPlaying) return;
        if (!debugOverrideBest || Data == null) return;

        if (Data.bestScore != debugBestScore)
        {
            Data.bestScore = debugBestScore;
            OnChallengeScoreChanged?.Invoke(Data.bestScore);
            if (debugAutoSave) Save();
            Debug.Log($"[DEBUG] bestScore = {Data.bestScore}");
        }
    }

    [ContextMenu("DEBUG: Apply Inspector BestScore")]
    public void DebugApplyInspectorBestScore()
    {
        if (!Application.isPlaying || Data == null) return;
        Data.bestScore = debugBestScore;
        OnChallengeScoreChanged?.Invoke(Data.bestScore);
        if (debugAutoSave) Save();
        Debug.Log($"[DEBUG] Applied bestScore = {Data.bestScore}");
    }
#endif

    public static ProgressManager Instance { get; private set; }
    public SaveData Data { get; private set; }

    // 외부 UI가 구독하는 이벤트
    public System.Action<int> OnChallengeScoreChanged;
    public System.Action<int> OnBestScoreChanged;
    public System.Action OnUnlocksChanged;
    public System.Action<int> OnChallengeTimeChangedMs;
    

    [Header("Unlock Table (Resources/Achievements)")]
    public string achievementsFolder = "Achievements";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
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

        Data = SaveSystem.Load() ?? new SaveData();

        // null-guard (구버전 세이브 호환)
        Data.unlockedSkins ??= new List<string>();
        Data.unlockedAbilities ??= new List<string>();
        Data.claimedAchievements ??= new List<string>();
        if (string.IsNullOrEmpty(Data.equippedSkinId))
            Data.equippedSkinId = "skin_default";
        if (!Data.unlockedSkins.Contains("skin_default"))
            Data.unlockedSkins.Add("skin_default");
    }

    public void Save() => SaveSystem.Save(Data);

    // 점수 보고
    public void GameOverReportRunScore(int score)
    {
        if (score > Data.challengeScore)
        {
            Data.challengeScore= score;
            OnChallengeScoreChanged?.Invoke(Data.challengeScore);
            Save();
        }
    }

    public void GameOverReportRunTimeMs(int timeMs)
    {
        if (timeMs > Data.challengeTimeMs)
        {
            Data.challengeTimeMs = timeMs;
            OnChallengeTimeChangedMs?.Invoke(Data.challengeTimeMs);
            Save();
        }
    }
    public void GameSuccessReportRunScore(int score)
    {
        if (score > Data.bestScore)
        {
            Data.bestScore= score;
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

    public void ResetProgress(bool saveFileAfter = false)
    {
        Data = new SaveData();
        if (!Data.unlockedSkins.Contains("skin_default"))
            Data.unlockedSkins.Add("skin_default");

        OnChallengeScoreChanged?.Invoke(Data.challengeScore);
        OnChallengeTimeChangedMs?.Invoke(Data.challengeTimeMs);
        OnUnlocksChanged?.Invoke();

        if (saveFileAfter) Save();
        Debug.Log("[Progress] Reset done");
    }

    // ───────────────── 업적 판단/상태 ─────────────────

    public bool IsAchievementEligible(string achievementId)
    {
        foreach (var a in Achievements.Table)
            if (a.id == achievementId)
                return Data.bestScore >= a.requiredBestScore;
        return false;
    }
    public bool IsAchievementClaimed(string achievementId)
        => Data.claimedAchievements.Contains(achievementId);

    // 성취 패널 ‘수령’ 버튼에서 호출
    public bool IsAchievementClaimable(string achievementId)
    {
        // 1) 이미 수령했으면 불가
        if (IsAchievementClaimed(achievementId)) return false;  // claimedAchievements 기준

        // 2) 보상 메타 조회(타입/요구치 등)
        var so = RewardDB.Get(achievementId);                   // RewardSO(id, type, requiredBestScore...)
        if (so == null) return false;

        // 3) 조건 달성 여부(지금은 점수 기준; 필요 시 시간 등 추가 가능)
        bool eligible = Data.bestScore >= so.requiredBestScore;

        // 4) 이미 소유(Unlocked)했는지 — 타입별로 분기
        bool owned = false;
        switch (so.type) // RewardType.Skin | RewardType.Card
        {
            case RewardType.Skin:
                owned = Data.unlockedSkins != null && Data.unlockedSkins.Contains(so.id);
                break;
            case RewardType.Card:
                owned = Data.unlockedAbilities != null && Data.unlockedAbilities.Contains(so.id);
                break;
        }

        // 5) ‘달성했고(eligible) 아직 소유 안 했고(!owned)’ 이면 수령 가능
        return eligible && !owned;
    }

    public bool ClaimAchievement(string achievementId)
    {
        if (!IsAchievementClaimable(achievementId)) return false;

        Achievement? found = null;
        foreach (var a in Achievements.Table)
            if (a.id == achievementId) { found = a; break; }
        if (found == null) return false;

        var ach = found.Value;
        switch (ach.unlockType)
        {
            case UnlockType.Skin:
                if (!Data.unlockedSkins.Contains(ach.payloadId))
                    Data.unlockedSkins.Add(ach.payloadId);
                break;

            case UnlockType.Ability:
                if (!Data.unlockedAbilities.Contains(ach.payloadId))
                    Data.unlockedAbilities.Add(ach.payloadId);
                break;
        }

        Data.claimedAchievements.Add(achievementId);

        // 시각/런타임 후처리
        RewardDB.GrantVisualOrRuntime(ach.payloadId, this);

        Save();
        OnUnlocksChanged?.Invoke();
        return true;
    }

    // ───────────────── 레거시 호환 (가능하면 사용 금지) ─────────────────
    [System.Obsolete("TryClaim은 즉시 수령합니다. 새 플로우에선 MarkAchievementClaimable/ClaimAchievement를 사용하세요.")]
    public bool TryClaim(string achievementId)
    {
        if (!IsAchievementEligible(achievementId)) return false;
        if (IsAchievementClaimed(achievementId)) return false;

        Achievement? found = null;
        foreach (var a in Achievements.Table)
            if (a.id == achievementId) { found = a; break; }
        if (found == null) return false;

        var ach = found.Value;

        switch (ach.unlockType)
        {
            case UnlockType.Skin:
                if (!Data.unlockedSkins.Contains(ach.payloadId))
                    Data.unlockedSkins.Add(ach.payloadId);
                break;

            case UnlockType.Ability:
                if (!Data.unlockedAbilities.Contains(ach.payloadId))
                    Data.unlockedAbilities.Add(ach.payloadId);
                break;
        }

        Data.claimedAchievements.Add(achievementId);
        RewardDB.GrantVisualOrRuntime(ach.payloadId, this);

        Save();
        OnUnlocksChanged?.Invoke();
        return true;
    }
}
