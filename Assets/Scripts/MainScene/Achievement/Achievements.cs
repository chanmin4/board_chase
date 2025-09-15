using System;
using System.Linq;
//기존코드 호환용 이제 reward DB all로 전환가능 여기사용 X
public enum UnlockType { Skin, Ability }

[Serializable]
public struct Achievement
{
    public string id;
    public int requiredBestScore;
    public UnlockType unlockType;
    public string payloadId;
    public string title;
    public string description;
}

public static class Achievements
{
    public static Achievement[] Table
    {
        get
        {
            RewardDB.EnsureLoaded();
            return RewardDB.All
                .Select(r => new Achievement
                {
                    id = r.id,
                    requiredBestScore = r.requiredBestScore,
                    // 카드 = Ability로 매핑(레거시 호환용)
                    unlockType = (r.type == RewardType.Skin) ? UnlockType.Skin : UnlockType.Ability,
                    // 예전 코드가 payloadId로 Reward를 찾았다면 id와 동일하게 둬도 동작
                    payloadId = r.id,
                    title = r.title,
                    description = r.description
                })
                .OrderBy(a => a.requiredBestScore)
                .ThenBy(a => a.id)
                .ToArray();
        }
    }
}
