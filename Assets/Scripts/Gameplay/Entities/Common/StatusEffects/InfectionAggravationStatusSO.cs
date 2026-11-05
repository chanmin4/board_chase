using System;
using UnityEngine;

/// <summary>A real persistent status definition. Stage spread and equipment mitigation have separate owners.</summary>
[CreateAssetMenu(fileName = "InfectionAggravation", menuName = "Game/Character/Status Effect/Infection Aggravation")]
public sealed class InfectionAggravationStatusSO : StatusEffectDefinitionSO
{
    private static InfectionAggravationStatusSO _active;
    public static InfectionAggravationStatusSO Active => _active != null ? _active :
        _active = Resources.Load<InfectionAggravationStatusSO>("InfectionAggravation");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    [SerializeField] private InfectionAggravationSettings _aggravation = new();
    public InfectionAggravationSettings Settings => _aggravation;
    public override bool PersistentStacks => true;
    public override bool DealsInfectionDamage => true;
}

[Serializable]
public sealed class InfectionAggravationSettings
{
    public bool enabled = true;
    [Min(1)] public float baseGaugeCapacity = 100;
    [Min(0)] public float naturalGaugeRecoveryPerSecond;
    [Range(0f, 1f)] public float maxHealthLossFraction = .1f;
    [Range(0f, 1f)] public float currentHealthLossExposureFraction = .5f;
    [Min(0f)] public float overdueSuppressionExposurePerSecond = 1f;
    [Min(0)] public float naturalRecoveryDelaySeconds = 5f;
    [Tooltip("원형 한 바퀴당 최대 체력 잠식량. 표시 스택 상한에서도 새 완주는 계속 잠식합니다.")]
    [Min(0)] public float maximumHealthLossPerStack = 5f;
    [Tooltip("감염방에서 실제 받은 공격 피해량 중 게이지로 누적하는 비율. 0.1 = 10%.")]
    [Min(0)] public float infectedRoomHitDamageRatio = .1f;
    [Min(0)] public float lootOpenExposure = 4;
    [Min(0)] public float itemPickupExposure = 1;
    [Min(0)] public float hitExposure = 5;
    [Min(0)] public float bleedingExposurePerSecond = 1;
    [Min(0)] public float gasExposurePerPressureSecond = 5;
    [Min(0)] public float infectedBulletExposure = 15;
}
