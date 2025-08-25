using UnityEngine;

[CreateAssetMenu(fileName = "Ach_", menuName = "Game/Achievement")]
public class AchievementDef : ScriptableObject
{
    public enum UnlockType { Skin, Ability }

    [Header("Condition")]
    public int requiredBestScore = 10;  // 이 점수 이상이면 해금

    [Header("Unlock")]
    public UnlockType unlockType = UnlockType.Skin;
    public string payloadId; // Skin: "skin_gold" / Ability: "Cards/Cleaner" 같은 리소스 키

    [Header("UI (선택)")]
    public string title = "New Unlock!";
    [TextArea] public string description;
    public Sprite icon;
}
