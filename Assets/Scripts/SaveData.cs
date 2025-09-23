using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;

    // 기록
    public int bestScore = 0;
    public int challengeScore = 0;
    public int challengeTimeMs = 0;

    // ── Unlock(수령 대기, 종류별) ─────────────────────────
    public List<string> unlockedSkins     = new(); // 아직 Claim 안 한 스킨 보상 id
    public List<string> unlockedAbilities = new(); // 아직 Claim 안 한 능력/카드 보상 id

    // ── Owned(수령완료, 종류별) ──────────────────────────────
    public List<string> ownedSkins       = new(); // Claim 완료로 내 소유가 된 스킨
    public List<string> ownedAbilities   = new(); // Claim 완료로 내 소유가 된 능력

    // ── Equip(장착) ─────────────────────────────────────
    public string equippedSkinId = "skin_default";

    // ── 업적 수령 기록(업적 ID) ─────────────────────────
    public List<string> claimedAchievements = new();
}
