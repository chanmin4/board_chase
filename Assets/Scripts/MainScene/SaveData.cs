using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;

    // 기록
    public int bestScore = 0;
    public int bestTimeMs = 0;   // ★ 생존 시간(밀리초) 저장
    // 해금
    public List<string> unlockedSkins = new();     // "skin_default", "skin_gold" ...
    public List<string> unlockedAbilities = new(); // "Cards/Cleaner" 같은 리소스 키

    // 선택(옵션)
    public string equippedSkinId = "skin_default";
    public List<string> claimedAchievements = new();
}
