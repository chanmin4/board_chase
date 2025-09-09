using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Game/Reward/Card")]
public class CardRewardSO : RewardSO
{
    [Header("Card Payload (실제 적용 데이터)")]
    public ScriptableObject cardData; // 예: CardDataSO, 또는 프리팹/파라미터 등을 참조

    private void OnEnable() { type = RewardType.Card; }

    public override void Grant(ProgressManager pm)
    {
               // 예시(주석):
        // 1) 카드 풀/상점/드롭 테이블 활성화:
        // CardRepository.Add(cardData) 또는 ShopManager.EnableCard(id 또는 cardKey);
        //
        // 2) 알림/저장:
        // pm.Save();
        // pm.OnUnlocksChanged?.Invoke();
    }
}
