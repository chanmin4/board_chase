using System.Collections.Generic;
using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }
    public SaveData Data { get; private set; }

    // 메인 메뉴/게임에서 구독 가능
    public System.Action<int> OnBestScoreChanged;
    public System.Action OnUnlocksChanged;
    public System.Action<int> OnBestTimeChangedMs; // (bestTimeMs)

    [Header("Unlock Table (Resources/Achievements)")]
    public string achievementsFolder = "Achievements"; // ScriptableObject 모음 폴더

    List<AchievementDef> _defs = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = SaveSystem.Load();

        // 업그레이드: 최초 저장에 기본 스킨 보장
        if (!Data.unlockedSkins.Contains("skin_default"))
            Data.unlockedSkins.Add("skin_default");

        // 도전과제 정의 로드
        _defs.AddRange(Resources.LoadAll<AchievementDef>(achievementsFolder));

        // 로드 직후 한 번 평가(버전 업그레이드 대비)
        ReevaluateUnlocks();
    }

    public void Save() => SaveSystem.Save(Data);

    // 게임이 끝났을 때 점수 보고
    public void ReportRunScore(int score)
    {
        if (score > Data.bestScore)
        {
            Data.bestScore = score;
            OnBestScoreChanged?.Invoke(Data.bestScore);
            ReevaluateUnlocks(); // 새 기록으로 해금 재평가
            Save();
        }
    }

    public bool HasSkin(string id) => Data.unlockedSkins.Contains(id);
    public bool HasAbility(string key) => Data.unlockedAbilities.Contains(key);

    public void EquipSkin(string id)
    {
        if (HasSkin(id)) { Data.equippedSkinId = id; Save(); }
    }

    void ReevaluateUnlocks()
    {
        bool changed = false;
        foreach (var def in _defs)
        {
            if (!def) continue;
            if (Data.bestScore < def.requiredBestScore) continue;

            switch (def.unlockType)
            {
                case AchievementDef.UnlockType.Skin:
                    if (!Data.unlockedSkins.Contains(def.payloadId))
                    { Data.unlockedSkins.Add(def.payloadId); changed = true; }
                    break;
                case AchievementDef.UnlockType.Ability:
                    // payloadId에는 "Cards/Cleaner" 같은 CardData 리소스 키를 권장
                    if (!Data.unlockedAbilities.Contains(def.payloadId))
                    { Data.unlockedAbilities.Add(def.payloadId); changed = true; }
                    break;
            }
        }
        if (changed)
        {
            OnUnlocksChanged?.Invoke();
            Save();
        }
    }
    public void ReportRunTimeMs(int timeMs)
    {
        if (timeMs > Data.bestTimeMs)
        {
            Data.bestTimeMs = timeMs;
            OnBestTimeChangedMs?.Invoke(Data.bestTimeMs);
            ReevaluateUnlocks();
            Save();
        }
    }

}
