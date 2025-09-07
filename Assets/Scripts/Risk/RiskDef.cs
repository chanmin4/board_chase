using UnityEngine;

public enum RiskType
{
    // 드래그/런처
    DragCooldownAdd,             // f0 = 추가 쿨다운(초)

    // 미사일
    MissileSpeedUp,              // f0 = 추적(턴) 속도 배수
    MissileExplosionUp,          // f0 = 폭발 반경 배수
    MissileSpawnEveryCycle,      // b0 = 매 사이클 1발
    MissileCountUp,              // i0 = 동시 최대 수 (예: 2)

    // 존/게이지
    ZoneGaugeGainDown,          // f0 = 게이지 획득 배수(예: 0.85)
    ZoneReqHitsUp,              // i0 = 요구 튕김 +N
    ZoneCompositionChange,       // layout 사용 (L/M/S 지정)

    // 카드
    CardChargeRequiredUp,       // i0 = 요구 충전 +N
    CardDisabled,                // b0 = 청소 기능 비활성

    // 환경
    PollutionFrictionEnable      // b0 = 감속 활성, f0 = 초당 감쇠율 등
}

[System.Serializable]
public struct ZoneLayout
{
    [Min(0)] public int large;
    [Min(0)] public int medium;
    [Min(0)] public int small;
}

[CreateAssetMenu(menuName = "Risk/Def", fileName = "RISK_NewDef")]
public class RiskDef : ScriptableObject
{
    [Header("Info")]
    [Tooltip("고유 ID (저장/중복체크용)")]
    public string riskId = "drag_cool_1";
    public string title  = "드래그 쿨타임 ↑ (1초)";
    [TextArea] public string desc = "드래그 후 재시도 대기시간이 1.0초 증가합니다.";
    [Min(0)] public int points = 1;
    public Sprite icon;

    [Header("Effect (타입별로 아래 수치 사용)")]
    public RiskType type = RiskType.DragCooldownAdd;

    [Tooltip("주요 실수 파라미터(배수/초/계수 등)")]
    public float float_parameter1 = 0.0f;   // 예: +1.0초 / ×1.6배 / 0.85 등
    public float float_parameter2= 0.0f;   // 필요 시 보조
    public float float_parameter3= 0.0f;

    [Tooltip("주요 bool 파라미터(토글성)")]
    public bool bool_parameter = false;   // 예: 매턴 등장 활성화 등

    [Header("Rules")]
    [Tooltip("함께 켤 수 없는 riskId들(서로 배타)")]
    public string[] excludes; // 예: ["drag_cool_2"]
}
