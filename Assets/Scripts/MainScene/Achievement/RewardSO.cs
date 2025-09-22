using UnityEngine;

public enum RewardType { Skin, Card }

/// <summary>
/// 공통 보상 메타 + 지급 훅. 
/// 저장/중복체크는 ProgressManager가 담당.
/// </summary>
public abstract class RewardSO : ScriptableObject
{
    [Header("Identity (세이브 키)")]
    public string id;                 // 예) "skin_bottlecap" / "Cards/Cleaner"

    [Header("Unlock Condition")]
    [Tooltip("이 포인트 이상 달성 시 해금")]
    public int requiredBestScore = 0;

    [Header("UI Meta")]
    public string title;              
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public RewardType type;
     [Header("DB/Listing")]
    [Tooltip("체크하면 RewardDB.All(업적/목록)에는 제외됩니다. Get/Grant는 가능.")]
    public bool hideFromListing = false;


    /// <summary>
    /// 실제 지급 훅(선택). ProgressManager.TryClaim에서 저장/중복처리는 끝났다고 가정.
    /// </summary>
    public abstract void Grant(ProgressManager pm);
}
