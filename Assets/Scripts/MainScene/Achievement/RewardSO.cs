using UnityEngine;

public enum RewardType { Skin, Card }

/// <summary>
/// 공통 보상 메타 + 지급 훅. 
/// 저장/중복체크는 ProgressManager가 담당(이미 구현되어 있음).
/// </summary>
public abstract class RewardSO : ScriptableObject
{
    [Header("Identity (세이브와 매칭)")]
    public string id;                 // 예) "skin_bottlecap"

    [Header("UI Meta")]
    public string title;              // 표시용 이름
    [TextArea] public string description; // 설명
    public Sprite icon;               // 리스트/팝업 아이콘

    public RewardType type;

    /// <summary>
    /// 실제 지급 훅(선택). 
    /// - ProgressManager.TryClaim(...)에서 목록 추가/저장은 이미 처리하니,
    ///   여기서는 '시각 적용/프리셋 교체/카드풀 반영' 같은 후처리 정도를 자유롭게.
    /// - 당장 필요 없으면 빈 구현으로 둬도 됨.
    /// </summary>
    public abstract void Grant(ProgressManager pm);
}

/// <summary>
/// 디스크 스킨 보상 (머티리얼/프리셋/스프라이트 등 연결)
/// </summary>


/// <summary>
/// 카드 보상 (카드 데이터/프리팹/파라미터)
/// </summary>
