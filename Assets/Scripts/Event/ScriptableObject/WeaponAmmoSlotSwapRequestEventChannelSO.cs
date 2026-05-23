using System;
using UnityEngine;

/// <summary>
/// HUD에서 드래그 앤 드롭으로 탄 슬롯 위치를 바꿔달라고 요청할 때 쓰는 데이터.
/// 실제 교체 가능 여부는 PlayerBulletLoadoutRuntime이 판단한다.
/// </summary>
[Serializable]
public struct WeaponAmmoSlotSwapRequest
{
    public int fromSlotIndex;
    public int toSlotIndex;

    public WeaponAmmoSlotSwapRequest(int fromSlotIndex, int toSlotIndex)
    {
        this.fromSlotIndex = fromSlotIndex;
        this.toSlotIndex = toSlotIndex;
    }
}

/// <summary>
/// WeaponAmmoHUD -> PlayerBulletLoadoutRuntime 으로 슬롯 교체 요청을 보내는 이벤트 채널.
/// </summary>
[CreateAssetMenu(
    fileName = "WeaponAmmoSlotSwapRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Slot Swap Request Event Channel")]
public class WeaponAmmoSlotSwapRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSlotSwapRequest> OnEventRaised;

    public void RaiseEvent(WeaponAmmoSlotSwapRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}