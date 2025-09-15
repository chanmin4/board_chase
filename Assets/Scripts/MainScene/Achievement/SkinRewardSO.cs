using UnityEngine;

[CreateAssetMenu(fileName = "Skin", menuName = "Game/Reward/Skin")]
public class SkinRewardSO : RewardSO
{
    [Header("Skin Payload (실제 적용 리소스)")]
    public Material diskMaterialPreset; 
    [Tooltip("디스크 스프라이트(선택)")]
    public Sprite diskSprite;   
        [Tooltip("장착 시 재생할 SFX(선택)")]
    public AudioClip equipSfx;
        [Tooltip("보상 수령 즉시 자동 장착할지 여부(기본: 꺼짐)")]
    public bool autoEquipOnGrant = false;
    // 필요에 따라 Shader, Trail, SFX, VFX 프리셋 등 추가 가능


    private void OnEnable() { type = RewardType.Skin; }

    public override void Grant(ProgressManager pm)
    {
        // 예시(주석):
        // 1) 수령 즉시 이 스킨을 장착하고 싶다면:
        // pm.Data.equippedSkinId = id;
        //
        // 2) 인게임 외형 반영:
        // DiskAppearance.Apply(diskMaterialPreset, diskSprite);
        //
        // 3) 효과음:
        // if (equipSfx) AudioMaster.I?.PlaySFX(equipSfx);
        //
        // 4) 저장/이벤트:
        // pm.Save();
        // pm.OnUnlocksChanged?.Invoke();
    }
}