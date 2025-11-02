using UnityEngine;

public enum PassiveEffectType
{
    InkRadiusMulAdd,       // CleanTrailAbility_Disk.radiusMul += amount
    InkRadiusAddWorldAdd,  // CleanTrailAbility_Disk.radiusAddWorld += amount
    LaunchCooldownMul      // DiskLauncher.cooldownSeconds *= mul   (예: 0.9f)
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
    public float mul = 0.9f;                   // 곱 계열(쿨타임 감소 등)
    public int maxStacks = 0;                  // 0이면 무제한
}
