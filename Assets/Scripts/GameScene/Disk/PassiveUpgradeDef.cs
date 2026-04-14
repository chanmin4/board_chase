using UnityEngine;

public enum PassiveEffectType
{
    InkRadiusAddWorld_Plus,  // CleanTrailAbility_Disk.radiusAddWorld += amount
    LaunchCooldown_Minus,     // DiskLauncher.cooldownSeconds
    BounceInkGet_Plus,//퍼펙트 바운드 성공시 잉크획득량 증가
    BounceInkLoss_Minus,//퍼펙트바운드실패시 잉크감소량 완화
    BounceSpeed_Plus,//퍼펙트 바운드 성공시 획득 가속력 추가증가
    InkRadiusConsume_Minus,//잉크 칠할때 사용량 감소
    InkRadiusConsumeEnemyInk_Minus,//적잉크덧칠할떄 사용 잉크배율량감소
    StunRecoverSpeedUp_Minus,//스턴 자연 회복속도 증가
    InkZonebonusHitInkRecover_Plus, //존 보너스 각 맞췄을시 잉크 회복량 증가
    PerFectBounceRange_Plus,  // 퍼펙트바운스 범위증가
    ZoneBonusArc_Plus//존 보너스 각도증가


}

[CreateAssetMenu(menuName = "Game/Passive Upgrade", fileName = "Passive_")]
public class PassiveUpgradeDef : ScriptableObject
{
    [Header("Meta")]
    public string id = "radius_mul_1";         // 고유 ID(중복 스택 카운트 키)
    public string title = "Radius +";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public PassiveEffectType effect;
    public float amount = 0.25f;               // 가산 계열
    public float mul = 1f;                   // 곱 계열(쿨타임 감소 등)
    public int maxStacks = 0;                  // 0이면 무제한
}
