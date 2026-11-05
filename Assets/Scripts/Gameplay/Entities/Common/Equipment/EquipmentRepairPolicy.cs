/// <summary>
/// 현 밸런스 정책: 장비 수리는 제공하지 않는다. UI뿐 아니라 직접 API 호출도 차단한다.
/// 기존 저장의 현재/최대 내구도와 수리 계수는 보존한다. 재도입 시 이 정책과 회귀 검사를 함께 변경한다.
/// </summary>
public static class EquipmentRepairPolicy
{
    public static bool Enabled => false;
    public const string DisabledMessage = "Equipment repair is currently unavailable.";
    public const string DisabledLabel = "수리 불가 — 소모된 내구도는 회복할 수 없습니다.";
}
