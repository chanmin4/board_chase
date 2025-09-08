// Achievements.cs
using System;

public enum UnlockType { Skin, Ability }

[Serializable]
public struct Achievement
{
    public string id;               // 고유키 (예: "pt_1")
    public int requiredBestScore;   // 요구 포인트
    public UnlockType unlockType;   // 보상 타입
    public string payloadId;        // 보상 ID (예: "skin_gold" / "Cards/Cleaner")
    public string title;            // UI 텍스트
    public string description;      // UI 텍스트

    public Achievement(string id, int req, UnlockType type, string payload, string title, string desc)
    {
        this.id = id;
        this.requiredBestScore = req;
        this.unlockType = type;
        this.payloadId = payload;
        this.title = title;
        this.description = desc;
    }
}

public static class Achievements
{
    // 필요 개수대로 마음껏 편집하세요
    public static readonly Achievement[] Table = new Achievement[]
    {
        new Achievement("pt_1", 1, UnlockType.Ability, "Cards/Cleaner", "1pt 달성", "최고 포인트 1 이상"),
        new Achievement("pt_2", 2, UnlockType.Skin,    "skin_silver",   "2pt 달성", "최고 포인트 2 이상"),
        new Achievement("pt_3", 3, UnlockType.Skin,    "skin_gold",     "3pt 달성", "최고 포인트 3 이상"),
    };
}
